<h1 align="center">
  <img src="DailyPlanner/planner.ico" width="48" alt="icon"/>
  <br/>
  Daily Planner
</h1>

<p align="center">
  <b>Beautiful weekly planner for Windows</b><br/>
  Tasks, habits, Pomodoro, meetings, statistics — fully offline, auto-updates
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/WPF-Desktop-7C5CFC?style=flat-square" alt="WPF"/>
  <img src="https://img.shields.io/badge/SQLite-EF_Core_10-003B57?style=flat-square&logo=sqlite" alt="SQLite"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="MIT"/>
  <img src="https://img.shields.io/github/v/release/valinerosgordov/DailyPlanner?style=flat-square&color=7C5CFC" alt="Release"/>
  <img src="https://img.shields.io/github/downloads/valinerosgordov/DailyPlanner/total?style=flat-square&color=green" alt="Downloads"/>
</p>

<p align="center">
  <a href="../../releases/latest"><b>Download</b></a> · <a href="#build-from-source">Build from source</a> · <a href="#features">Features</a>
</p>

---

<!-- Replace with actual screenshots. Recommended: 1280×800, PNG, dark theme -->
<p align="center">
  <img src="assets/screenshot-week.png" width="720" alt="Weekly view"/>
</p>

<details>
<summary><b>More screenshots</b></summary>
<br/>
<p align="center">
  <img src="assets/screenshot-habits.png" width="720" alt="Habit tracker"/>
  <br/><br/>
  <img src="assets/screenshot-statistics.png" width="720" alt="Statistics"/>
  <br/><br/>
  <img src="assets/screenshot-pomodoro.png" width="720" alt="Pomodoro timer"/>
  <br/><br/>
  <img src="assets/screenshot-themes.png" width="720" alt="Theme showcase"/>
  <br/><br/>
  <img src="assets/screenshot-myday.png" width="720" alt="My Day review"/>
</p>
</details>

---

## Features

**Planning**
- 7-day weekly view with drag & drop — reorder tasks, move between days
- Subtasks with progress counter (2/5)
- Deadlines — overdue tasks highlighted in red
- Priorities (low / medium / high) and categories (work, study, personal, health)
- Recurring templates — auto-create tasks every week
- Carry-over — incomplete tasks move to next week automatically
- Weekly goals (up to 4) and free-form notes

**Productivity**
- Pomodoro timer — 45/5 min work/break cycles, focus mode, sound alerts
- My Day — morning dialog to review yesterday and plan today
- Daily quotes — motivational quote in sidebar

**Tracking**
- Habit tracker with weekly streaks
- State monitoring — sleep, energy, mood (1–5 scale) per day
- Statistics — productivity charts, weekly comparison, trends
- Excel export — any week to `.xlsx`

**Meetings & Reminders**
- Meeting scheduler — title, attendees, description, date/time/duration
- Reminders — 1 day, 2 hours, and 30 min before meeting
- Task reminders — time-based notifications

**Customization**
- 9 color themes — Catppuccin (Mocha, Frappe, Macchiato), Nord, Everforest, Coffee, Graphite, Obsidian, Light
- 4 languages — English, Russian, Spanish, French

**Quality of life**
- Undo delete — restore tasks with one click
- Search — `Ctrl+F` across all tasks and goals
- Auto-backup on startup, backup & restore
- Auto-updates via Velopack (~100 KB delta)
- System tray — minimize to tray

## Keyboard shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+T` | Jump to today |
| `Ctrl+F` | Search tasks and goals |
| `Ctrl+P` | Open Pomodoro timer |

## Installation

### Installer (recommended)
1. Download `DailyPlanner-win-Setup.exe` from [Releases](../../releases/latest)
2. Run the installer — desktop shortcut will be created
3. Updates install automatically on launch

### Portable
1. Download `DailyPlanner-win-Portable.zip` from [Releases](../../releases/latest)
2. Extract to any folder
3. Run `DailyPlanner.exe`

> **Requirements:** Windows 10 or later

## Build from source

```bash
# Requirements: .NET 10 SDK, Windows 10+
git clone https://github.com/valinerosgordov/DailyPlanner.git
cd DailyPlanner
dotnet build --verbosity minimal
dotnet run --project DailyPlanner
```

### Publishing

```bash
# Self-contained single-file executable
dotnet publish DailyPlanner -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish

# Velopack package for auto-updates
vpk pack -u DailyPlanner -v 2.13.3 -p publish -o releases
```

## Tech stack

| Layer | Technology |
|-------|-----------|
| UI | WPF + [WPF-UI](https://github.com/lepoco/wpfui) (Fluent design) |
| Architecture | MVVM — [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) with source generators |
| Database | Entity Framework Core 10 + SQLite (code-first, 10 migrations) |
| Export | [ClosedXML](https://github.com/ClosedXML/ClosedXML) |
| Updates | [Velopack](https://github.com/velopack/velopack) — delta packages |
| Runtime | .NET 10, C# 14 |

## Architecture

```
DailyPlanner/
├── Models/          # EF Core entities (11 models)
├── Data/            # DbContext + migrations
├── ViewModels/      # 12 ViewModels (MVVM)
├── Views/           # XAML pages + custom controls
├── Services/        # Business logic, themes, localization, export
├── Converters/      # WPF value converters
└── Themes/          # Color palette resources
```

All data stored locally: `%LOCALAPPDATA%\DailyPlanner\planner.db`

## License

[MIT](LICENSE)
