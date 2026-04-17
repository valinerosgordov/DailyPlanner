<h1 align="center">
  <img src="DailyPlanner/planner.ico" width="56" alt="DailyPlanner icon"/>
  <br/>
  Daily &amp; Financial Planner
</h1>

<p align="center">
  Monochrome weekly planner for Windows with finance tracker, Trello inbox, habits and Pomodoro — fully offline.
</p>

<p align="center">
  <a href="https://github.com/valinerosgordov/DailyPlanner/releases/latest"><img src="https://img.shields.io/github/v/release/valinerosgordov/DailyPlanner?style=flat-square&label=latest"/></a>
  <a href="https://github.com/valinerosgordov/DailyPlanner/releases/latest/download/DailyPlanner-win-Setup.exe"><img src="https://img.shields.io/github/downloads/valinerosgordov/DailyPlanner/total?style=flat-square&label=downloads"/></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-blue?style=flat-square"/>
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=.net"/>
</p>

---

## Install

**Installer (auto-updating):**
[DailyPlanner-win-Setup.exe](https://github.com/valinerosgordov/DailyPlanner/releases/latest/download/DailyPlanner-win-Setup.exe)

**Portable:**
[DailyPlanner-win-Portable.zip](https://github.com/valinerosgordov/DailyPlanner/releases/latest/download/DailyPlanner-win-Portable.zip)

Windows 10/11 x64. .NET 10 runtime is bundled in the self-contained build.

---

## Features

### Planning
- Weekly board with 7 day columns, 10 task slots per day
- Subtasks, priorities (None/Low/Med/High), categories, deadlines
- Carry-over incomplete tasks to the next day
- Drag &amp; drop tasks between days
- Weekly goals, notes, reminders, meetings
- Daily state tracking (sleep / energy / mood, 1–5 stars)
- Habit heatmap for the last 30 days

### Trello integration (Inbox)
- Inbox sidebar built into the week view
- Pull cards from a named list (default: “В работе”) across all your boards
- Drag Trello cards directly onto any day — fills the first empty slot
- Already-placed cards aren’t re-added on re-sync
- Optional auto-sync on startup

### Finance module
- Income &amp; expenses with categories, budgets, and monthly analytics
- Debts (I owe / owed to me) factored into Net Worth
- Recurring payments (weekly / biweekly / monthly / quarterly / yearly) normalised to monthly obligatory
- Multiple accounts with transfers
- 30-day cashflow forecast
- Income sources with per-project payment schedule
- Excel export

### Pomodoro
- Work / break / focus with configurable durations
- Session counter per day

### Statistics
- Monthly task completion, productivity trends
- Best / hardest day, goals reached
- Habit streak tracking

### System
- Multi-language UI: 🇷🇺 🇺🇸 🇪🇸 🇫🇷
- 2 themes: Pure Monochrome Dark (default, Mica backdrop) / Pure Monochrome Light
- Auto-backup on startup (rolling 5)
- Auto-updates via Velopack

---

## Stack

| Layer | Tech |
|---|---|
| UI | WPF + WPF-UI (FluentWindow, Mica) |
| MVVM | CommunityToolkit.Mvvm source generators |
| DI | Microsoft.Extensions.DependencyInjection |
| DB | EF Core 10 + SQLite (`%LOCALAPPDATA%\DailyPlanner\planner.db`) |
| Excel | ClosedXML |
| Auto-update | Velopack + GitHub Releases |
| Logging | File logger with rotation (`app.log`) |
| Tests | xUnit + FluentAssertions |

---

## Keyboard shortcuts

| Key | Action |
|---|---|
| `Ctrl+T` | Jump to today |
| `Ctrl+P` | Toggle Pomodoro |
| `Ctrl+F` | Search |

---

## Build from source

```powershell
# Requires .NET 10 SDK (preview)
dotnet restore DailyPlanner/DailyPlanner.csproj -r win-x64
dotnet build   DailyPlanner/DailyPlanner.csproj --no-restore -c Release -r win-x64
dotnet run --project DailyPlanner
```

Run tests:

```powershell
dotnet test DailyPlanner.Tests/DailyPlanner.Tests.csproj
```

Publish a release build:

```powershell
dotnet publish DailyPlanner/DailyPlanner.csproj -c Release -r win-x64 --self-contained -o ./publish
```

---

## License

MIT
