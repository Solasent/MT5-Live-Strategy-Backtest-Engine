using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LiveStrategyBacktest
{
    public partial class Form1 : Form
    {
        // ====================================================================
        //  Domain types
        // ====================================================================

        public sealed class ReportTrade
        {
            public int Ticket;
            public DateTime CloseTime;
            public string Type = "";          // "BUY" or "SELL"
            public double Volume;
            public double Profit;
            public double RunningEquity;      // populated after sort
        }

        private enum ChartMode { Equity, Heatmap }

        private enum TradeFilter { All, BuysOnly, SellsOnly }

        // ====================================================================
        //  State
        // ====================================================================

        private readonly List<ReportTrade> _allTrades = new();
        private List<ReportTrade> _viewTrades = new();
        private TradeFilter _filter = TradeFilter.All;
        private ChartMode _chartMode = ChartMode.Equity;

        // Pre-computed analytics on the filtered view
        private double _netProfit, _profitFactor, _recovery, _winRate;
        private double _expPayoff, _sharpe, _maxDDPct;
        private readonly double[,] _heatmap = new double[5, 24];   // [DayOfWeek 0..4 Mon-Fri, Hour 0..23]
        private double _heatmapMin, _heatmapMax;

        // Theme colors (re-declared here for paint code)
        private static readonly Color ColBg          = Color.FromArgb(17, 20, 28);
        private static readonly Color ColCard        = Color.FromArgb(26, 31, 44);
        private static readonly Color ColGrid        = Color.FromArgb(38, 45, 62);
        private static readonly Color ColAccent      = Color.FromArgb(0, 230, 195);
        private static readonly Color ColLoss        = Color.FromArgb(255, 59, 48);
        private static readonly Color ColText        = Color.FromArgb(232, 236, 245);
        private static readonly Color ColTextMuted   = Color.FromArgb(140, 150, 170);

        // ====================================================================
        //  Construction & wiring
        // ====================================================================

        public Form1()
        {
            InitializeComponent();

            // Button events
            browseButton.Click       += async (_, __) => await PromptAndLoadAsync();
            recalcButton.Click       += (_, __) => RecomputeAndRender();
            chartToggleButton.Click  += OnToggleChart;
            filterCombo.SelectedIndexChanged += OnFilterChanged;

            // Drag & drop — both at form level and on the sidebar drop zone
            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop  += OnDragDrop;
            dropZonePanel.DragEnter += OnDragEnter;
            dropZonePanel.DragDrop  += OnDragDrop;

            // Paint hook for the chart canvas
            chartCanvas.Paint  += OnChartPaint;
            chartCanvas.Resize += (_, __) => chartCanvas.Invalidate();

            UpdateStatus("Ready — load an MT5 report to begin.");
        }

        // ====================================================================
        //  Drag-drop
        // ====================================================================

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                dropZoneLabel.ForeColor = Color.White;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private async void OnDragDrop(object? sender, DragEventArgs e)
        {
            dropZoneLabel.ForeColor = ColAccent;
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                await LoadFileAsync(files[0]);
            }
        }

        // ====================================================================
        //  File loading
        // ====================================================================

        private async Task PromptAndLoadAsync()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Select an MT5 backtest report",
                Filter = "MT5 reports (*.csv;*.htm;*.html)|*.csv;*.htm;*.html|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await LoadFileAsync(dlg.FileName);
            }
        }

        private async Task LoadFileAsync(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show(this, $"File not found:\n{path}", "Load error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetBusy(true, $"Parsing {Path.GetFileName(path)}…");
            try
            {
                List<ReportTrade> parsed = await Task.Run(() => ParseMt5Report(path));

                _allTrades.Clear();
                _allTrades.AddRange(parsed);

                UpdateStatus($"Loaded {parsed.Count:n0} trades from {Path.GetFileName(path)}");
                RecomputeAndRender();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to parse report:\n{ex.Message}", "Parse error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Parse failed — see error dialog.");
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ====================================================================
        //  High-speed MT5 report parser
        //  Supports the standard CSV export columns:
        //    Ticket, Time/Open Time/Close Time, Type, Volume/Size, Symbol,
        //    Price, S/L, T/P, Profit
        //  Tolerates differing column orders by reading the header row.
        // ====================================================================

        public static List<ReportTrade> ParseMt5Report(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".htm" || ext == ".html")
                return ParseHtmlReport(filePath);

            return ParseCsvReportAsync(filePath).GetAwaiter().GetResult();
        }

        private static async Task<List<ReportTrade>> ParseCsvReportAsync(string filePath)
        {
            var trades = new List<ReportTrade>(capacity: 4096);

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                FileShare.Read, bufferSize: 1 << 16, useAsync: true);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            // Locate header line, tolerate leading blank / metadata rows.
            Dictionary<string, int>? cols = null;
            char delim = ',';
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                delim = DetectDelimiter(line);
                string[] header = SplitCsv(line, delim);
                if (LooksLikeHeader(header))
                {
                    cols = BuildHeaderMap(header);
                    break;
                }
            }

            if (cols == null)
                throw new InvalidDataException("Could not find an MT5 trade header row (Ticket/Type/Profit).");

            int idxTicket  = TryCol(cols, "ticket", "order", "deal", "position");
            int idxTime    = TryCol(cols, "close time", "time", "closetime", "close_time");
            int idxType    = TryCol(cols, "type", "direction");
            int idxVolume  = TryCol(cols, "volume", "size", "lots");
            int idxProfit  = TryCol(cols, "profit", "profit usd", "p/l", "pl");

            if (idxType < 0 || idxProfit < 0)
                throw new InvalidDataException("Missing required columns 'Type' and/or 'Profit'.");

            var invariant = CultureInfo.InvariantCulture;

            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] f = SplitCsv(line, delim);
                if (f.Length <= idxProfit) continue;

                string rawType = idxType >= 0 ? f[idxType].Trim().ToLowerInvariant() : "";
                string normType =
                    rawType.Contains("sell") || rawType == "out" ? "SELL" :
                    rawType.Contains("buy")  || rawType == "in"  ? "BUY"  : "";
                if (normType.Length == 0) continue; // skip balance/credit/non-trade lines

                if (!TryParseDouble(idxProfit >= 0 ? f[idxProfit] : "", invariant, out double profit))
                    continue;

                int ticket = 0;
                if (idxTicket >= 0) int.TryParse(StripNonDigits(f[idxTicket]), NumberStyles.Integer, invariant, out ticket);

                DateTime ct = DateTime.MinValue;
                if (idxTime >= 0) TryParseDate(f[idxTime], out ct);

                double vol = 0;
                if (idxVolume >= 0) TryParseDouble(f[idxVolume], invariant, out vol);

                trades.Add(new ReportTrade
                {
                    Ticket    = ticket,
                    CloseTime = ct,
                    Type      = normType,
                    Volume    = vol,
                    Profit    = profit
                });
            }

            return trades;
        }

        /// <summary>Tolerant HTML report parser — extracts trade rows from MT5's "Report" .htm export.</summary>
        private static List<ReportTrade> ParseHtmlReport(string filePath)
        {
            var trades = new List<ReportTrade>(capacity: 4096);
            string html = File.ReadAllText(filePath);

            // Pull all <tr>…</tr> blocks
            var rowRx  = new Regex(@"<tr[^>]*>(?<body>.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var cellRx = new Regex(@"<t[dh][^>]*>(?<cell>.*?)</t[dh]>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var tagRx  = new Regex(@"<[^>]+>", RegexOptions.Singleline);

            Dictionary<string, int>? cols = null;
            var invariant = CultureInfo.InvariantCulture;

            foreach (Match rm in rowRx.Matches(html))
            {
                var cells = new List<string>();
                foreach (Match cm in cellRx.Matches(rm.Groups["body"].Value))
                {
                    string txt = WebUtilityDecode(tagRx.Replace(cm.Groups["cell"].Value, "")).Trim();
                    cells.Add(txt);
                }
                if (cells.Count == 0) continue;

                if (cols == null && LooksLikeHeader(cells.ToArray()))
                {
                    cols = BuildHeaderMap(cells.ToArray());
                    continue;
                }
                if (cols == null) continue;

                int idxType   = TryCol(cols, "type", "direction");
                int idxProfit = TryCol(cols, "profit", "profit usd", "p/l", "pl");
                int idxTicket = TryCol(cols, "ticket", "order", "deal", "position");
                int idxTime   = TryCol(cols, "close time", "time", "closetime", "close_time");
                int idxVol    = TryCol(cols, "volume", "size", "lots");

                if (idxType < 0 || idxProfit < 0 || cells.Count <= idxProfit) continue;

                string rawType = cells[idxType].ToLowerInvariant();
                string normType =
                    rawType.Contains("sell") ? "SELL" :
                    rawType.Contains("buy")  ? "BUY"  : "";
                if (normType.Length == 0) continue;

                if (!TryParseDouble(cells[idxProfit], invariant, out double profit)) continue;

                int ticket = 0;
                if (idxTicket >= 0 && cells.Count > idxTicket)
                    int.TryParse(StripNonDigits(cells[idxTicket]), out ticket);

                DateTime ct = DateTime.MinValue;
                if (idxTime >= 0 && cells.Count > idxTime) TryParseDate(cells[idxTime], out ct);

                double vol = 0;
                if (idxVol >= 0 && cells.Count > idxVol) TryParseDouble(cells[idxVol], invariant, out vol);

                trades.Add(new ReportTrade
                {
                    Ticket = ticket, CloseTime = ct, Type = normType,
                    Volume = vol, Profit = profit
                });
            }
            return trades;
        }

        // ---- Parse helpers ----

        private static char DetectDelimiter(string line)
        {
            int commas = 0, tabs = 0, semis = 0;
            foreach (char c in line) { if (c == ',') commas++; else if (c == '\t') tabs++; else if (c == ';') semis++; }
            if (tabs >= commas && tabs >= semis && tabs > 0) return '\t';
            if (semis > commas) return ';';
            return ',';
        }

        private static bool LooksLikeHeader(string[] cells)
        {
            int hits = 0;
            foreach (string c in cells)
            {
                string s = c.Trim().ToLowerInvariant();
                if (s is "ticket" or "order" or "deal" or "position" or "type" or "profit" or "volume" or "size") hits++;
            }
            return hits >= 2;
        }

        private static Dictionary<string, int> BuildHeaderMap(string[] cells)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < cells.Length; i++)
            {
                string key = cells[i].Trim().ToLowerInvariant();
                if (key.Length == 0) continue;
                if (!map.ContainsKey(key)) map.Add(key, i);
            }
            return map;
        }

        private static int TryCol(Dictionary<string, int> map, params string[] keys)
        {
            foreach (string k in keys)
                if (map.TryGetValue(k, out int idx)) return idx;
            return -1;
        }

        private static string[] SplitCsv(string line, char delim)
        {
            // Simple quote-aware splitter (good enough for MT5 exports)
            var parts = new List<string>(16);
            var sb = new StringBuilder();
            bool inQuote = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') { inQuote = !inQuote; continue; }
                if (c == delim && !inQuote) { parts.Add(sb.ToString()); sb.Clear(); continue; }
                sb.Append(c);
            }
            parts.Add(sb.ToString());
            return parts.ToArray();
        }

        private static bool TryParseDouble(string s, IFormatProvider fp, out double v)
        {
            s = (s ?? "").Trim().Replace(" ", "");
            // Some exports use comma as decimal — try invariant first, then current culture
            if (double.TryParse(s, NumberStyles.Any, fp, out v)) return true;
            return double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out v);
        }

        private static bool TryParseDate(string s, out DateTime d)
        {
            s = (s ?? "").Trim();
            string[] formats =
            {
                "yyyy.MM.dd HH:mm:ss", "yyyy.MM.dd HH:mm",
                "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm",
                "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm",
                "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm"
            };
            if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out d)) return true;
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out d);
        }

        private static string StripNonDigits(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s) if (char.IsDigit(c)) sb.Append(c);
            return sb.ToString();
        }

        private static string WebUtilityDecode(string s) =>
            System.Net.WebUtility.HtmlDecode(s).Replace("\u00A0", " ");

        // ====================================================================
        //  Filter / recompute / render orchestration
        // ====================================================================

        private void OnFilterChanged(object? sender, EventArgs e)
        {
            _filter = filterCombo.SelectedIndex switch
            {
                1 => TradeFilter.BuysOnly,
                2 => TradeFilter.SellsOnly,
                _ => TradeFilter.All
            };
            RecomputeAndRender();
        }

        private void OnToggleChart(object? sender, EventArgs e)
        {
            _chartMode = _chartMode == ChartMode.Equity ? ChartMode.Heatmap : ChartMode.Equity;
            chartToggleButton.Text = _chartMode == ChartMode.Equity ? "📊  Show Heatmap" : "📈  Show Equity";
            chartCardTitle.Text    = _chartMode == ChartMode.Equity
                ? "EQUITY & DRAWDOWN CURVE"
                : "HOURLY × WEEKDAY PERFORMANCE HEATMAP";
            chartCanvas.Invalidate();
        }

        private void RecomputeAndRender()
        {
            _viewTrades = ApplyFilter(_allTrades, _filter);
            ComputeStatistics(_viewTrades);
            ComputeHeatmap(_viewTrades);
            UpdateStatsLabels();
            tradeCounterValue.Text = _viewTrades.Count.ToString("n0");
            chartCanvas.Invalidate();
        }

        private static List<ReportTrade> ApplyFilter(List<ReportTrade> src, TradeFilter f)
        {
            IEnumerable<ReportTrade> q = src;
            if (f == TradeFilter.BuysOnly)  q = src.Where(t => t.Type == "BUY");
            if (f == TradeFilter.SellsOnly) q = src.Where(t => t.Type == "SELL");

            var list = q.OrderBy(t => t.CloseTime).ToList();
            double equity = 0;
            for (int i = 0; i < list.Count; i++)
            {
                equity += list[i].Profit;
                list[i].RunningEquity = equity;
            }
            return list;
        }

        // ====================================================================
        //  Institutional mathematics engine
        // ====================================================================

        private void ComputeStatistics(List<ReportTrade> trades)
        {
            if (trades.Count == 0)
            {
                _netProfit = _profitFactor = _recovery = _winRate =
                _expPayoff = _sharpe = _maxDDPct = 0;
                return;
            }

            double grossProfit = 0, grossLoss = 0;
            int wins = 0;
            foreach (var t in trades)
            {
                if (t.Profit >= 0) { grossProfit += t.Profit; if (t.Profit > 0) wins++; }
                else                grossLoss   += -t.Profit;
            }

            _netProfit    = grossProfit - grossLoss;
            _profitFactor = grossLoss > 0 ? grossProfit / grossLoss : (grossProfit > 0 ? double.PositiveInfinity : 0);
            _winRate      = 100.0 * wins / trades.Count;
            _expPayoff    = _netProfit / trades.Count;

            // ---- Max equity drawdown (%) ----
            double peak = 0, maxDD = 0;
            foreach (var t in trades)
            {
                if (t.RunningEquity > peak) peak = t.RunningEquity;
                double dd = peak - t.RunningEquity;
                if (peak > 0)
                {
                    double pct = (dd / peak) * 100.0;
                    if (pct > maxDD) maxDD = pct;
                }
            }
            _maxDDPct = maxDD;

            // Recovery Factor = net profit / max absolute drawdown
            double maxAbsDD = 0; peak = 0;
            foreach (var t in trades)
            {
                if (t.RunningEquity > peak) peak = t.RunningEquity;
                double dd = peak - t.RunningEquity;
                if (dd > maxAbsDD) maxAbsDD = dd;
            }
            _recovery = maxAbsDD > 0 ? _netProfit / maxAbsDD : 0;

            // ---- Sharpe ratio (annualized, daily returns) ----
            // Group profits by date, compute mean and stddev of daily returns.
            var daily = trades
                .Where(t => t.CloseTime != DateTime.MinValue)
                .GroupBy(t => t.CloseTime.Date)
                .Select(g => g.Sum(t => t.Profit))
                .ToArray();

            if (daily.Length > 1)
            {
                double mean = daily.Average();
                double sumSq = 0;
                for (int i = 0; i < daily.Length; i++) { double d = daily[i] - mean; sumSq += d * d; }
                double std = Math.Sqrt(sumSq / (daily.Length - 1));
                _sharpe = std > 1e-9 ? (mean / std) * Math.Sqrt(252.0) : 0;
            }
            else _sharpe = 0;
        }

        private void ComputeHeatmap(List<ReportTrade> trades)
        {
            Array.Clear(_heatmap, 0, _heatmap.Length);
            foreach (var t in trades)
            {
                if (t.CloseTime == DateTime.MinValue) continue;
                int dow = (int)t.CloseTime.DayOfWeek; // Sun=0..Sat=6
                if (dow == 0 || dow == 6) continue;   // skip weekends
                int row = dow - 1;                    // Mon=0..Fri=4
                int hr  = t.CloseTime.Hour;
                _heatmap[row, hr] += t.Profit;
            }
            double min = 0, max = 0;
            for (int r = 0; r < 5; r++)
                for (int h = 0; h < 24; h++)
                {
                    double v = _heatmap[r, h];
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            _heatmapMin = min;
            _heatmapMax = max;
        }

        private void UpdateStatsLabels()
        {
            lblNetProfitV.Text    = _netProfit.ToString("C2", CultureInfo.CurrentCulture);
            lblNetProfitV.ForeColor = _netProfit >= 0 ? ColAccent : ColLoss;

            lblProfitFactorV.Text = double.IsInfinity(_profitFactor) ? "∞" : _profitFactor.ToString("0.00");
            lblRecoveryV.Text     = _recovery.ToString("0.00");
            lblWinRateV.Text      = _winRate.ToString("0.0") + "%";
            lblExpPayoffV.Text    = _expPayoff.ToString("C2", CultureInfo.CurrentCulture);
            lblSharpeV.Text       = _sharpe.ToString("0.00");
            lblMaxDDV.Text        = _maxDDPct.ToString("0.0") + "%";
            lblMaxDDV.ForeColor   = _maxDDPct > 25 ? ColLoss : ColText;
        }

        // ====================================================================
        //  GDI+ paint engine — equity curve & heatmap
        // ====================================================================

        private void OnChartPaint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(ColCard);

            Rectangle r = chartCanvas.ClientRectangle;
            r.Inflate(-10, -10);
            r.Y += 10; r.Height -= 10;

            if (_viewTrades.Count == 0)
            {
                using var f = new Font("Segoe UI", 10F);
                using var br = new SolidBrush(ColTextMuted);
                TextRenderer.DrawText(g, "No data loaded — drop or browse an MT5 backtest report.",
                    f, r, ColTextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            if (_chartMode == ChartMode.Equity) DrawEquityCurve(g, r);
            else                                DrawHeatmap(g, r);
        }

        private void DrawEquityCurve(Graphics g, Rectangle bounds)
        {
            // ---- Compute scale ----
            double minEq = 0, maxEq = 0;
            foreach (var t in _viewTrades)
            {
                if (t.RunningEquity < minEq) minEq = t.RunningEquity;
                if (t.RunningEquity > maxEq) maxEq = t.RunningEquity;
            }
            if (Math.Abs(maxEq - minEq) < 1e-9) { maxEq = minEq + 1; }
            double range = maxEq - minEq;
            double pad   = range * 0.08;
            minEq -= pad; maxEq += pad;
            range = maxEq - minEq;

            int padL = 70, padR = 16, padT = 8, padB = 28;
            var plot = new Rectangle(bounds.X + padL, bounds.Y + padT,
                                     bounds.Width - padL - padR,
                                     bounds.Height - padT - padB);

            // ---- Grid lines + Y axis labels ----
            using (var gridPen = new Pen(ColGrid) { DashStyle = DashStyle.Dot })
            using (var axisFont = new Font("Segoe UI", 8F))
            {
                const int yTicks = 5;
                for (int i = 0; i <= yTicks; i++)
                {
                    int y = plot.Bottom - (int)(i * (double)plot.Height / yTicks);
                    g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                    double val = minEq + i * range / yTicks;
                    string s = val.ToString("C0", CultureInfo.CurrentCulture);
                    TextRenderer.DrawText(g, s, axisFont,
                        new Rectangle(bounds.X, y - 8, padL - 6, 16),
                        ColTextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                }
            }

            // ---- Compute peak/equity points for line + drawdown shading ----
            int n = _viewTrades.Count;
            PointF[] eqPts   = new PointF[n];
            PointF[] peakPts = new PointF[n];
            double peak = double.MinValue;

            float xScale = n > 1 ? (float)plot.Width / (n - 1) : 0f;
            float YOf(double v) => plot.Bottom - (float)((v - minEq) / range * plot.Height);

            for (int i = 0; i < n; i++)
            {
                double eq = _viewTrades[i].RunningEquity;
                if (eq > peak) peak = eq;
                float x = plot.Left + i * xScale;
                eqPts[i]   = new PointF(x, YOf(eq));
                peakPts[i] = new PointF(x, YOf(peak));
            }

            // ---- Drawdown shading: area between peak curve and equity ----
            if (n > 1)
            {
                var ddPath = new GraphicsPath();
                ddPath.AddLines(peakPts);
                for (int i = n - 1; i >= 0; i--) ddPath.AddLine(eqPts[i], eqPts[i]);
                // Append reversed equity points
                var rev = new PointF[n];
                for (int i = 0; i < n; i++) rev[i] = eqPts[n - 1 - i];
                ddPath.AddLines(rev);
                ddPath.CloseFigure();

                using var ddBrush = new SolidBrush(Color.FromArgb(55, ColLoss));
                g.FillPath(ddBrush, ddPath);
                ddPath.Dispose();
            }

            // ---- Equity area fill (above zero only) ----
            if (n > 1)
            {
                float zeroY = YOf(0);
                zeroY = Math.Clamp(zeroY, plot.Top, plot.Bottom);

                var fillPts = new List<PointF>(n + 2);
                fillPts.AddRange(eqPts);
                fillPts.Add(new PointF(eqPts[n - 1].X, zeroY));
                fillPts.Add(new PointF(eqPts[0].X,     zeroY));

                using var lg = new LinearGradientBrush(
                    new RectangleF(plot.Left, plot.Top, plot.Width, plot.Height),
                    Color.FromArgb(110, ColAccent), Color.FromArgb(0, ColAccent),
                    LinearGradientMode.Vertical);
                g.FillPolygon(lg, fillPts.ToArray());
            }

            // ---- Equity line ----
            using (var linePen = new Pen(ColAccent, 2.0f) { LineJoin = LineJoin.Round })
            {
                if (n > 1) g.DrawLines(linePen, eqPts);
            }

            // ---- Peak (high-water mark) thin guide ----
            using (var peakPen = new Pen(Color.FromArgb(80, ColText), 1f) { DashStyle = DashStyle.Dash })
            {
                if (n > 1) g.DrawLines(peakPen, peakPts);
            }

            // ---- X-axis labels: first & last close times ----
            using (var axisFont = new Font("Segoe UI", 8F))
            {
                var first = _viewTrades[0].CloseTime;
                var last  = _viewTrades[^1].CloseTime;
                string sFirst = first == DateTime.MinValue ? "Trade #1" : first.ToString("yyyy-MM-dd");
                string sLast  = last  == DateTime.MinValue ? $"Trade #{n}" : last.ToString("yyyy-MM-dd");
                TextRenderer.DrawText(g, sFirst, axisFont,
                    new Rectangle(plot.Left, plot.Bottom + 4, 120, 18),
                    ColTextMuted, TextFormatFlags.Left);
                TextRenderer.DrawText(g, sLast, axisFont,
                    new Rectangle(plot.Right - 120, plot.Bottom + 4, 120, 18),
                    ColTextMuted, TextFormatFlags.Right);
            }

            // ---- Legend ----
            DrawLegend(g, plot, ("Equity", ColAccent), ("Drawdown", Color.FromArgb(180, ColLoss)));
        }

        private void DrawHeatmap(Graphics g, Rectangle bounds)
        {
            string[] days  = { "MON", "TUE", "WED", "THU", "FRI" };
            int padL = 56, padR = 16, padT = 22, padB = 28;
            var plot = new Rectangle(bounds.X + padL, bounds.Y + padT,
                                     bounds.Width - padL - padR,
                                     bounds.Height - padT - padB);

            float cellW = plot.Width / 24f;
            float cellH = plot.Height / 5f;

            using var rowFont  = new Font("Segoe UI Semibold", 8F);
            using var hourFont = new Font("Segoe UI", 7.5F);
            using var valFont  = new Font("Segoe UI Semibold", 7.5F);

            // Cells
            for (int r = 0; r < 5; r++)
            {
                for (int h = 0; h < 24; h++)
                {
                    var cell = new RectangleF(plot.X + h * cellW + 1, plot.Y + r * cellH + 1,
                                              cellW - 2, cellH - 2);
                    double v = _heatmap[r, h];
                    Color c = HeatmapColor(v, _heatmapMin, _heatmapMax);
                    using (var br = new SolidBrush(c)) g.FillRectangle(br, cell);

                    if (Math.Abs(v) > 0.0001 && cell.Width > 26 && cell.Height > 20)
                    {
                        string s = Math.Abs(v) >= 1000 ? (v / 1000.0).ToString("0.#") + "k"
                                                       : v.ToString("0");
                        TextRenderer.DrawText(g, s, valFont,
                            Rectangle.Round(cell), ColText,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
                // Row label
                TextRenderer.DrawText(g, days[r], rowFont,
                    new Rectangle(bounds.X, (int)(plot.Y + r * cellH), padL - 8, (int)cellH),
                    ColTextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }

            // Hour labels (every 2h)
            for (int h = 0; h < 24; h += 2)
            {
                var rect = new Rectangle((int)(plot.X + h * cellW), plot.Bottom + 4,
                                         (int)(cellW * 2), 16);
                TextRenderer.DrawText(g, h.ToString("00"), hourFont,
                    rect, ColTextMuted, TextFormatFlags.HorizontalCenter);
            }

            // Title strip "HOUR OF DAY (UTC)"
            TextRenderer.DrawText(g, "HOUR OF DAY",
                new Font("Segoe UI", 8F, FontStyle.Bold),
                new Rectangle(plot.X, bounds.Y, plot.Width, 16),
                ColTextMuted, TextFormatFlags.HorizontalCenter);

            // Legend gradient
            int legendW = 160, legendH = 8;
            int legendX = plot.Right - legendW, legendY = bounds.Bottom - 14;
            using (var lg = new LinearGradientBrush(
                new Rectangle(legendX, legendY, legendW, legendH),
                ColLoss, ColAccent, LinearGradientMode.Horizontal))
            {
                g.FillRectangle(lg, legendX, legendY, legendW, legendH);
            }
            TextRenderer.DrawText(g, "loss",  hourFont, new Rectangle(legendX - 36, legendY - 3, 32, 16),
                ColTextMuted, TextFormatFlags.Right);
            TextRenderer.DrawText(g, "profit", hourFont, new Rectangle(legendX + legendW + 4, legendY - 3, 40, 16),
                ColTextMuted, TextFormatFlags.Left);
        }

        private static Color HeatmapColor(double v, double min, double max)
        {
            if (Math.Abs(v) < 1e-9) return Color.FromArgb(36, 42, 58);

            if (v > 0)
            {
                double t = max > 1e-9 ? Math.Min(1.0, v / max) : 0;
                int alpha = 70 + (int)(180 * t);
                return Color.FromArgb(alpha, ColAccent);
            }
            else
            {
                double t = min < -1e-9 ? Math.Min(1.0, v / min) : 0; // both negative -> positive ratio
                int alpha = 70 + (int)(180 * t);
                return Color.FromArgb(alpha, ColLoss);
            }
        }

        private static void DrawLegend(Graphics g, Rectangle plot, params (string label, Color color)[] items)
        {
            using var font = new Font("Segoe UI", 8F);
            int x = plot.Right - 8, y = plot.Top + 4;
            for (int i = items.Length - 1; i >= 0; i--)
            {
                string txt = items[i].label;
                Size sz = TextRenderer.MeasureText(txt, font);
                using var br = new SolidBrush(items[i].color);
                g.FillRectangle(br, x - sz.Width - 16, y + 4, 10, 10);
                TextRenderer.DrawText(g, txt, font,
                    new Point(x - sz.Width, y), ColTextMuted);
                x -= sz.Width + 26;
            }
        }

        // ====================================================================
        //  Status / busy
        // ====================================================================

        private void UpdateStatus(string s)
        {
            if (InvokeRequired) { BeginInvoke(() => statusLabel.Text = s); return; }
            statusLabel.Text = s;
        }

        private void SetBusy(bool busy, string? statusText = null)
        {
            if (InvokeRequired) { BeginInvoke(() => SetBusy(busy, statusText)); return; }

            loadingBar.Visible = busy;
            loadingBar.MarqueeAnimationSpeed = busy ? 25 : 0;
            browseButton.Enabled = !busy;
            recalcButton.Enabled = !busy;
            filterCombo.Enabled  = !busy;
            if (statusText != null) statusLabel.Text = statusText;
            Cursor = busy ? Cursors.AppStarting : Cursors.Default;
        }
    }
}
