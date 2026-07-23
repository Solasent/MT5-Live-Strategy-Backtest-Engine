"""Generate a realistic MT5-style backtest CSV for testing the analyzer."""
import csv, random
from datetime import datetime, timedelta

random.seed(42)

START = datetime(2025, 1, 6, 8, 0)   # Monday
N_TRADES = 220
SYMBOL = "EURUSD"

# Bias certain hour ranges to be more profitable (London/NY sessions)
GOOD_HOURS = {7, 8, 9, 13, 14, 15}
BAD_HOURS  = {0, 1, 2, 3, 22, 23}

rows = []
t = START
ticket = 1000001
for _ in range(N_TRADES):
    # Step forward 1-9 hours, skip weekends
    t += timedelta(hours=random.randint(1, 9))
    while t.weekday() >= 5:                        # Sat=5, Sun=6
        t += timedelta(days=1)
        t = t.replace(hour=random.randint(7, 18))

    side = random.choice(["buy", "sell"])
    vol  = round(random.choice([0.10, 0.20, 0.50, 1.00]), 2)
    price_open  = round(1.0700 + random.uniform(-0.02, 0.02), 5)
    price_close = round(price_open + random.uniform(-0.0040, 0.0040), 5)

    # Profit bias
    hr = t.hour
    if hr in GOOD_HOURS:
        win_p = 0.62
    elif hr in BAD_HOURS:
        win_p = 0.35
    else:
        win_p = 0.50

    is_win = random.random() < win_p
    mag    = abs(random.gauss(0, 1)) * 35 * vol + 5  # USD
    profit = round(mag if is_win else -mag * random.uniform(0.7, 1.3), 2)

    rows.append({
        "Ticket": ticket,
        "Open Time":  (t - timedelta(hours=random.randint(1, 6))).strftime("%Y.%m.%d %H:%M:%S"),
        "Type":       side,
        "Volume":     f"{vol:.2f}",
        "Symbol":     SYMBOL,
        "Price":      f"{price_open:.5f}",
        "S/L":        f"{price_open - 0.0050:.5f}" if side == "buy" else f"{price_open + 0.0050:.5f}",
        "T/P":        f"{price_open + 0.0080:.5f}" if side == "buy" else f"{price_open - 0.0080:.5f}",
        "Close Time": t.strftime("%Y.%m.%d %H:%M:%S"),
        "Profit":     f"{profit:.2f}",
    })
    ticket += 1

out = r"C:\Users\LW\Desktop\New Projs\LiveStrategyBacktest\sample_data\sample_backtest.csv"
with open(out, "w", newline="", encoding="utf-8") as f:
    # Mimic MT5 export: a couple of metadata rows then the header
    f.write("Strategy Tester Report\n")
    f.write(f"Symbol;{SYMBOL};Period;H1;Model;Every tick\n")
    f.write("\n")
    w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
    w.writeheader()
    w.writerows(rows)

print(f"Wrote {len(rows)} trades to {out}")
