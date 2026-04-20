<div align="center">

<img src="DailyPlanner/planner.ico" width="84" alt="Daily Planner"/>

# Daily &amp; Financial Planner

**Weekly planner for Windows. Fully offline. Auto-updating.**

Tasks · Habits · Pomodoro · Finance tracker · Trello inbox

[![Latest release](https://img.shields.io/github/v/release/valinerosgordov/DailyPlanner?style=for-the-badge&color=7C3AED)](https://github.com/valinerosgordov/DailyPlanner/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/valinerosgordov/DailyPlanner/total?style=for-the-badge&color=10B981)](https://github.com/valinerosgordov/DailyPlanner/releases/latest)
[![Platform](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?style=for-the-badge&logo=windows)](https://github.com/valinerosgordov/DailyPlanner/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=.net)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/valinerosgordov/DailyPlanner?style=for-the-badge&color=F59E0B)](LICENSE)

</div>

---

## Download

<table>
<tr>
<td align="center" width="50%">

### Installer *(recommended)*
Auto-updates via Velopack

**[⬇ DailyPlanner-win-Setup.exe](https://github.com/valinerosgordov/DailyPlanner/releases/latest/download/DailyPlanner-win-Setup.exe)**

</td>
<td align="center" width="50%">

### Portable
No installation, no auto-updates

**[⬇ DailyPlanner-win-Portable.zip](https://github.com/valinerosgordov/DailyPlanner/releases/latest/download/DailyPlanner-win-Portable.zip)**

</td>
</tr>
</table>

Windows 10 / 11 x64. .NET 10 runtime is bundled (self-contained build).

---

## Features

### Planning
- Weekly board — 7 day columns, 10 task slots per day
- Subtasks, priorities (None / Low / Med / High), categories, deadlines
- Drag &amp; drop between days · carry-over of incomplete tasks
- Weekly goals, notes, reminders, meetings
- Daily state tracking — sleep, energy, mood (1–5 stars)
- Habit heatmap for the last 30 days

### Trello inbox
- Built-in sidebar with cards from a named Trello list (default: *"В работе"*)
- Pulls cards across all your boards
- Drag a card onto any day — fills the first empty slot
- Already-placed cards never re-appear on resync *(deduplicated by card id)*
- Optional auto-sync on startup

### Finance
- Income &amp; expenses with categories, budgets, monthly analytics
- Debts (I owe / owed to me) factored into Net Worth
- Recurring payments (weekly / biweekly / monthly / quarterly / yearly), normalised to monthly obligatory
- Multiple accounts with transfers
- 30-day cashflow forecast
- Income sources with per-project payment schedule
- Excel export

### Pomodoro
- Work / break / focus timer with configurable durations
- Session counter per day

### Statistics
- Monthly task completion &amp; productivity trends
- Best / hardest day of the week, goals reached
- Habit streak tracking

### System
- Single-instance guard — second launch just activates the existing window *(even from tray)*
- Multi-language UI — 🇷🇺 🇺🇸 🇪🇸 🇫🇷
- Pure Monochrome Dark *(default, Mica backdrop)* / Pure Monochrome Light
- Auto-backup on startup (rolling 5)
- Auto-updates via Velopack + GitHub Releases

---

## Keyboard shortcuts

| Key | Action |
|---|---|
| `Ctrl+←` / `Ctrl+→` | Previous / next month |
| `Ctrl+T` | Jump to today |
| `Ctrl+F` | Search |
| `Ctrl+E` | Export to Excel |
| `Ctrl+P` | Toggle Pomodoro |
| `Ctrl+M` | Toggle Finance module |
| `Ctrl+W` | Weekly review |

---

## Stack

| Layer | Tech |
|---|---|
| UI | WPF + [WPF-UI](https://github.com/lepoco/wpfui) (FluentWindow, Mica backdrop) |
| MVVM | [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) (source generators) |
| DI | `Microsoft.Extensions.DependencyInjection` |
| DB | EF Core 10 + SQLite (`%LOCALAPPDATA%\DailyPlanner\planner.db`) |
| Excel | [ClosedXML](https://github.com/ClosedXML/ClosedXML) |
| Auto-update | [Velopack](https://velopack.io/) + GitHub Releases |
| Logging | File logger with rotation (`app.log`) |
| Tests | xUnit + FluentAssertions (74 tests, integration + unit) |

---

## Build from source

Requires [.NET 10 SDK Preview](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
dotnet restore DailyPlanner.Tests/DailyPlanner.Tests.csproj
dotnet restore DailyPlanner/DailyPlanner.csproj -r win-x64
dotnet build DailyPlanner/DailyPlanner.csproj --no-restore -c Release -r win-x64
dotnet run --project DailyPlanner
```

Run tests:

```powershell
dotnet test DailyPlanner.Tests/DailyPlanner.Tests.csproj
```

Publish a self-contained build:

```powershell
dotnet publish DailyPlanner/DailyPlanner.csproj -c Release -r win-x64 --self-contained -o ./publish
```

---

## Data &amp; privacy

All data is stored locally in `%LOCALAPPDATA%\DailyPlanner\planner.db`. The app only talks to the network when:

- You enable Trello sync (requests to `api.trello.com` with *your* key + token)
- Checking for updates (`github.com/valinerosgordov/DailyPlanner` releases)

Nothing is uploaded. Nothing is telemetered. No accounts required.

---

## Contributing

Pull requests welcome — see [CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Security reports: [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE) © Valiner Osgordov
