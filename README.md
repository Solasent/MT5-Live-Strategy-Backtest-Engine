<div align="center">

Topics: metatrader5, mql5, backtesting, expert-advisor, mql4, metatrader, forex-trading, automated-trading, quantitative-trading, trading-strategies, forward-testing, strategy-tester, monte-carlo, mt4, mt5, trading-bot, mt5-backtest-engine, mt5-bar-replay, mt5-forward-tester

# Information

**Live strategy backtesting and forward-testing platform for MT5 / MT4 traders. The project lets you replay historical bars, run real-time forward tests, and measure walk-forward performance against streaming market data, all from one desktop console with full equity curve, drawdown, and profit factor analytics.**

# 🧪 Live Strategy Backtest MT5/MT4

**Bar-by-bar replay engine plus live forward-test mode with equity curve, drawdown stats, and side-by-side strategy comparison.**

<br>

[![Stars](https://img.shields.io/github/stars/torvalds/linux?style=for-the-badge&color=00D4AA&label=Stars)](https://github.com/your-username/volume-profile-mt5/stargazers)
[![Forks](https://img.shields.io/github/forks/torvalds/linux?style=for-the-badge&color=4D9FFF&label=Forks)](https://github.com/your-username/volume-profile-mt5/network)
[![Issues](https://img.shields.io/github/issues/torvalds/linux?style=for-the-badge&color=FF4D6A&label=Issues)](https://github.com/your-username/volume-profile-mt5/issues)
[![Platform](https://img.shields.io/badge/MT5%20%2F%20MT4-Compatible-00D4AA?style=for-the-badge)](https://www.metatrader5.com)
[![License](https://img.shields.io/badge/License-MIT-4D9FFF?style=for-the-badge)](LICENSE)

</div>

<p align="center">
    <img src="https://minkxx-spotify-readme.vercel.app/api?theme=dark&rainbow=true&scan=true&spin=True" alt="Preview">
</p>

---

## 📸 Screenshot

<div align="center">

<p align="center">
  <img src="https://i.ibb.co/PsnTdw7V/4.png" alt="Backtest console" width="820">
</p>

</div>

---

## 🎬 Demo

<div align="center">

<img src="https://i.imgur.com/M4Qtq9L.gif" alt="Demo">

</div>


---

## Why Live Backtesting?

A strategy that wins on static history can still die on live data.

This project bridges the gap by running:
- Full historical replay  
- Real-time forward tests on streaming bars  
- Side-by-side comparison of both equity curves  

---

## What It Does

**Live Strategy Backtest MT5/MT4** turns your trading idea into a measurable, statistically honest workflow.

| Module | Description |
|---|---|
| Bar Replay Engine | Steps through history at variable speed |
| Forward Test | Runs strategy on live broker feed |
| Order Simulator | Slippage & spread aware execution |
| Equity Tracker | Real-time balance & drawdown |
| Stats Engine | Sharpe, profit factor, expectancy |
| Trade Log | Every entry, exit, and reason |

---

## Features

| Feature | Description |
|---|---|
| Replay Speed Control | 1x to 500x bar speed |
| Equity Curve | Live drawing & zoom |
| Drawdown Heatmap | Max DD per session highlighted |
| Strategy Slots | Load multiple strategies in parallel |
| MT4 / MT5 Support | Platform selection system |
| Symbol & Timeframe | Any pair, M1 to D1 |
| Spread Simulator | Fixed / variable / realistic |
| Trade Table | All trades with P&L and R |
| Stats Panel | Win rate, PF, Sharpe, expectancy |
| Export | CSV / JSON results |

---

## System Behavior

- Deterministic replay, identical run = identical result
- Forward and replay can run side by side
- Pause / resume any test mid-run
- Crash-safe state save per session

---

## Quick Start

**Requirements:**
- Windows 10 / 11  
- .NET 8+  
- Visual Studio 2022  

```bash
git clone https://github.com/your-username/live-strategy-backtest.git
```

Open solution → Press **F5**

---

## How to Use

1. Launch app  
2. Select MT4 / MT5  
3. Enter login  
4. Click **CONNECT**  
5. Load strategy & data range  
6. Pick replay speed  
7. Click **START TEST**  
8. Read equity & stats live  

---

## Interface Logic

```
EQUITY  ┌─/\__/\─┐
        │       └─/\
DD      ────▼────▼──
TRADES  +  -  +  +  -
```

- Green = trade closed in profit  
- Red = trade closed in loss  
- Blue line = equity curve  
- Red shading = drawdown  

---

## Roadmap

- [x] Bar replay  
- [x] Forward test  
- [x] Stats engine  
- [ ] Real MT5 history bridge  
- [ ] Monte Carlo runs  
- [ ] Walk-forward optimizer  
- [ ] Cloud test queue  

---

## Contributing

```
1. Fork
2. git checkout -b feature/new-feature
3. git commit -m "Add feature"
4. git push
5. Open PR
```

---

## License

MIT

---

<div align="center">

Live Strategy Backtest MT5/MT4 · v1.0

</div>
