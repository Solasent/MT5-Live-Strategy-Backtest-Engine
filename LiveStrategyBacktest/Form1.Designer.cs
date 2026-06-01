namespace LiveStrategyBacktest
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // ---- Sidebar ----
        private Panel sidebarPanel = null!;
        private Label logoLabel = null!;
        private Label logoSubLabel = null!;
        private Panel dropZonePanel = null!;
        private Label dropZoneLabel = null!;
        private Label dropZoneHintLabel = null!;
        private Label tradeCounterTitle = null!;
        private Label tradeCounterValue = null!;
        private Label sidebarFootLabel = null!;

        // ---- Top control bar ----
        private Panel topBarPanel = null!;
        private Button browseButton = null!;
        private ComboBox filterCombo = null!;
        private Button recalcButton = null!;
        private Button chartToggleButton = null!;
        private Label statusLabel = null!;
        private ProgressBar loadingBar = null!;

        // ---- Statistics card ----
        private Panel statsCard = null!;
        private Label statsCardTitle = null!;
        private TableLayoutPanel statsGrid = null!;

        private Label lblNetProfitT = null!, lblNetProfitV = null!;
        private Label lblProfitFactorT = null!, lblProfitFactorV = null!;
        private Label lblRecoveryT = null!, lblRecoveryV = null!;
        private Label lblWinRateT = null!, lblWinRateV = null!;
        private Label lblExpPayoffT = null!, lblExpPayoffV = null!;
        private Label lblSharpeT = null!, lblSharpeV = null!;
        private Label lblMaxDDT = null!, lblMaxDDV = null!;

        // ---- Chart canvas card ----
        private Panel chartCard = null!;
        private Label chartCardTitle = null!;
        private DoubleBufferedPanel chartCanvas = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ---- helpers ----
            Color colBg          = Color.FromArgb(17, 20, 28);   // #11141C
            Color colCard        = Color.FromArgb(26, 31, 44);   // #1A1F2C
            Color colCardEdge    = Color.FromArgb(38, 45, 62);
            Color colAccent      = Color.FromArgb(0, 230, 195);  // #00E6C3
            Color colLoss        = Color.FromArgb(255, 59, 48);  // #FF3B30
            Color colTextPrimary = Color.FromArgb(232, 236, 245);
            Color colTextMuted   = Color.FromArgb(140, 150, 170);

            Font fontTitle    = new Font("Segoe UI Semibold", 11F, FontStyle.Regular);
            Font fontStatTtl  = new Font("Segoe UI", 9F);
            Font fontStatVal  = new Font("Segoe UI Semibold", 14F);
            Font fontLogo     = new Font("Segoe UI Black", 12F);
            Font fontLogoSub  = new Font("Segoe UI", 8F);
            Font fontButton   = new Font("Segoe UI Semibold", 9F);

            SuspendLayout();

            // ============ Sidebar ============
            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 230,
                BackColor = colCard,
                Padding = new Padding(18)
            };

            logoLabel = new Label
            {
                Text = "AUTOSCRIPTS",
                Font = fontLogo,
                ForeColor = colAccent,
                AutoSize = true,
                Location = new Point(18, 22)
            };

            logoSubLabel = new Label
            {
                Text = "ANALYZER  •  v1.0",
                Font = fontLogoSub,
                ForeColor = colTextMuted,
                AutoSize = true,
                Location = new Point(19, 46)
            };

            dropZonePanel = new Panel
            {
                Location = new Point(18, 90),
                Size = new Size(194, 140),
                BackColor = colBg,
                BorderStyle = BorderStyle.FixedSingle,
                AllowDrop = true
            };

            dropZoneLabel = new Label
            {
                Text = "⇩  DROP MT5 REPORT",
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = colAccent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            dropZoneHintLabel = new Label
            {
                Text = "Drag & drop a .csv or .html\nbacktest export here",
                Font = new Font("Segoe UI", 8F),
                ForeColor = colTextMuted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            dropZonePanel.Controls.Add(dropZoneHintLabel);
            dropZonePanel.Controls.Add(dropZoneLabel);

            tradeCounterTitle = new Label
            {
                Text = "TRADES PROCESSED",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = colTextMuted,
                AutoSize = true,
                Location = new Point(20, 252)
            };

            tradeCounterValue = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI Semibold", 28F),
                ForeColor = colTextPrimary,
                AutoSize = true,
                Location = new Point(18, 270)
            };

            sidebarFootLabel = new Label
            {
                Text = "Live Strategy Backtest\n& Optimization Analyzer",
                Font = new Font("Segoe UI", 8F),
                ForeColor = colTextMuted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(4, 0, 0, 8)
            };

            sidebarPanel.Controls.Add(tradeCounterValue);
            sidebarPanel.Controls.Add(tradeCounterTitle);
            sidebarPanel.Controls.Add(dropZonePanel);
            sidebarPanel.Controls.Add(logoSubLabel);
            sidebarPanel.Controls.Add(logoLabel);
            sidebarPanel.Controls.Add(sidebarFootLabel);

            // ============ Top control bar ============
            topBarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = colBg,
                Padding = new Padding(20, 14, 20, 14)
            };

            browseButton = new Button
            {
                Text = "📂  Browse Backtest File",
                Font = fontButton,
                ForeColor = Color.White,
                BackColor = colAccent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(190, 36),
                Location = new Point(20, 14),
                Cursor = Cursors.Hand
            };
            browseButton.FlatAppearance.BorderSize = 0;

            filterCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = fontButton,
                ForeColor = colTextPrimary,
                BackColor = colCard,
                Size = new Size(160, 36),
                Location = new Point(220, 16)
            };
            filterCombo.Items.AddRange(new object[] { "All Trades", "Buys Only", "Sells Only" });
            filterCombo.SelectedIndex = 0;

            recalcButton = new Button
            {
                Text = "↻  Recalculate",
                Font = fontButton,
                ForeColor = colTextPrimary,
                BackColor = colCard,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 36),
                Location = new Point(390, 14),
                Cursor = Cursors.Hand
            };
            recalcButton.FlatAppearance.BorderColor = colCardEdge;

            chartToggleButton = new Button
            {
                Text = "📊  Show Heatmap",
                Font = fontButton,
                ForeColor = colTextPrimary,
                BackColor = colCard,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(170, 36),
                Location = new Point(530, 14),
                Cursor = Cursors.Hand
            };
            chartToggleButton.FlatAppearance.BorderColor = colCardEdge;

            statusLabel = new Label
            {
                Text = "Ready — load an MT5 report to begin.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = colTextMuted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Right,
                Width = 320
            };

            loadingBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 0,
                Visible = false,
                Location = new Point(710, 22),
                Size = new Size(140, 18)
            };

            topBarPanel.Controls.Add(statusLabel);
            topBarPanel.Controls.Add(loadingBar);
            topBarPanel.Controls.Add(chartToggleButton);
            topBarPanel.Controls.Add(recalcButton);
            topBarPanel.Controls.Add(filterCombo);
            topBarPanel.Controls.Add(browseButton);

            // ============ Stats card ============
            statsCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 170,
                BackColor = colCard,
                Padding = new Padding(20, 14, 20, 14),
                Margin = new Padding(20)
            };

            statsCardTitle = new Label
            {
                Text = "ADVANCED STATISTICS",
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = colTextMuted,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            statsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                ColumnCount = 7,
                RowCount = 2,
                BackColor = colCard
            };
            for (int i = 0; i < 7; i++)
                statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 7F));
            statsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            statsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));

            lblNetProfitT     = MakeStatTitle("NET PROFIT",        fontStatTtl, colTextMuted);
            lblNetProfitV     = MakeStatValue("$0.00",             fontStatVal, colTextPrimary);
            lblProfitFactorT  = MakeStatTitle("PROFIT FACTOR",     fontStatTtl, colTextMuted);
            lblProfitFactorV  = MakeStatValue("0.00",              fontStatVal, colTextPrimary);
            lblRecoveryT      = MakeStatTitle("RECOVERY FACTOR",   fontStatTtl, colTextMuted);
            lblRecoveryV      = MakeStatValue("0.00",              fontStatVal, colTextPrimary);
            lblWinRateT       = MakeStatTitle("WIN RATE",          fontStatTtl, colTextMuted);
            lblWinRateV       = MakeStatValue("0.0%",              fontStatVal, colTextPrimary);
            lblExpPayoffT     = MakeStatTitle("EXPECTED PAYOFF",   fontStatTtl, colTextMuted);
            lblExpPayoffV     = MakeStatValue("$0.00",             fontStatVal, colTextPrimary);
            lblSharpeT        = MakeStatTitle("SHARPE RATIO",      fontStatTtl, colTextMuted);
            lblSharpeV        = MakeStatValue("0.00",              fontStatVal, colTextPrimary);
            lblMaxDDT         = MakeStatTitle("MAX DRAWDOWN",      fontStatTtl, colTextMuted);
            lblMaxDDV         = MakeStatValue("0.0%",              fontStatVal, colTextPrimary);

            statsGrid.Controls.Add(lblNetProfitT,    0, 0);
            statsGrid.Controls.Add(lblNetProfitV,    0, 1);
            statsGrid.Controls.Add(lblProfitFactorT, 1, 0);
            statsGrid.Controls.Add(lblProfitFactorV, 1, 1);
            statsGrid.Controls.Add(lblRecoveryT,     2, 0);
            statsGrid.Controls.Add(lblRecoveryV,     2, 1);
            statsGrid.Controls.Add(lblWinRateT,      3, 0);
            statsGrid.Controls.Add(lblWinRateV,      3, 1);
            statsGrid.Controls.Add(lblExpPayoffT,    4, 0);
            statsGrid.Controls.Add(lblExpPayoffV,    4, 1);
            statsGrid.Controls.Add(lblSharpeT,       5, 0);
            statsGrid.Controls.Add(lblSharpeV,       5, 1);
            statsGrid.Controls.Add(lblMaxDDT,        6, 0);
            statsGrid.Controls.Add(lblMaxDDV,        6, 1);

            statsCard.Controls.Add(statsGrid);
            statsCard.Controls.Add(statsCardTitle);

            // ============ Chart card ============
            chartCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = colCard,
                Padding = new Padding(20, 14, 20, 18)
            };

            chartCardTitle = new Label
            {
                Text = "EQUITY & DRAWDOWN CURVE",
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = colTextMuted,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            chartCanvas = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = colCard,
                Margin = new Padding(0, 32, 0, 0),
                Padding = new Padding(0, 30, 0, 0)
            };

            chartCard.Controls.Add(chartCanvas);
            chartCard.Controls.Add(chartCardTitle);

            // ============ Outer assembly ============
            // Order matters for Dock fill behavior — Fill must be added FIRST.
            var rightContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = colBg,
                Padding = new Padding(20, 0, 20, 20)
            };
            rightContainer.Controls.Add(chartCard);
            rightContainer.Controls.Add(statsCard);

            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1320, 820);
            MinimumSize = new Size(1100, 700);
            BackColor = colBg;
            ForeColor = colTextPrimary;
            Font = new Font("Segoe UI", 9F);
            Text = "Live Strategy Backtest & Optimization Analyzer";
            StartPosition = FormStartPosition.CenterScreen;

            Controls.Add(rightContainer);
            Controls.Add(topBarPanel);
            Controls.Add(sidebarPanel);

            AllowDrop = true;

            ResumeLayout(false);
        }

        private static Label MakeStatTitle(string text, Font f, Color c) => new Label
        {
            Text = text,
            Font = f,
            ForeColor = c,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(2, 0, 0, 2)
        };

        private static Label MakeStatValue(string text, Font f, Color c) => new Label
        {
            Text = text,
            Font = f,
            ForeColor = c,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(2, 2, 0, 0)
        };

        #endregion
    }

    /// <summary>Panel with composited double-buffering enabled — eliminates flicker during repaint.</summary>
    internal sealed class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
        }
    }
}
