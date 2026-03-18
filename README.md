<h1 align="center">Daily Planner</h1>

<p align="center">
  <b>Beautiful weekly planner for Windows</b><br/>
  WPF + .NET 10 + SQLite | Fully local, no cloud
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/WPF-Desktop-7C5CFC?style=flat-square" alt="WPF"/>
  <img src="https://img.shields.io/badge/SQLite-EF_Core_10-003B57?style=flat-square&logo=sqlite" alt="SQLite"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="MIT"/>
  <img src="https://img.shields.io/github/v/release/valinerosgordov/DailyPlanner?style=flat-square&color=7C5CFC" alt="Release"/>
  <img src="https://img.shields.io/github/downloads/valinerosgordov/DailyPlanner/total?style=flat-square&color=green" alt="Downloads"/>
</p>

---

## Features

| Feature | Description |
|---------|-------------|
| **Weekly planning** | 7-day view with tasks, weekly goals, notes |
| **Subtasks** | Nested tasks with progress counter (2/5) |
| **Deadlines** | Set due dates, overdue tasks highlighted in red |
| **Undo delete** | Restore accidentally deleted tasks with one click |
| **Context menu** | Right-click tasks for quick actions |
| **My Day review** | Morning dialog to review yesterday and plan today |
| **Meeting scheduler** | Title, attendees, description, date/time/duration |
| **Meeting reminders** | Notifications 1 day and 2 hours before |
| **Habit tracker** | Daily habits with weekly streak progress |
| **State monitoring** | Sleep, energy, mood (1-5) per day with analytics |
| **Priorities & categories** | Work, study, personal, health + 3 priority levels |
| **Drag & drop** | Move tasks between days, reorder within a day |
| **Carry-over** | Incomplete tasks auto-move to next week |
| **Recurring templates** | Auto-create tasks every week |
| **Pomodoro timer** | Customizable work/break timer with sound alerts |
| **Reminders** | Time-based notifications for tasks |
| **Statistics** | Productivity charts, weekly comparison, trends |
| **9 color palettes** | Catppuccin (Mocha/Frappe/Macchiato), Nord, Everforest, Coffee, Graphite, Obsidian, Light |
| **4 languages** | Russian, English, Spanish, French |
| **Excel export** | Export any week to .xlsx |
| **Search** | Ctrl+F — search across all tasks and goals |
| **Daily quotes** | Motivational quote in sidebar |
| **Auto-updates** | Delta updates via Velopack (~100 KB) |
| **Backup & restore** | Auto-backup on startup |
| **Keyboard shortcuts** | Ctrl+T (today), Ctrl+F (search), Ctrl+P (pomodoro) |

## Installation

### Installer (recommended)
1. Download `DailyPlanner-win-Setup.exe` from [Releases](../../releases/latest)
2. Run the installer — desktop shortcut will be created
3. Updates install automatically on launch

### Portable
1. Download `DailyPlanner-win-Portable.zip` from [Releases](../../releases/latest)
2. Extract to any folder
3. Run `DailyPlanner.exe`

## Build from source

```bash
# Requirements: .NET 10 SDK
dotnet build --verbosity minimal
dotnet run --project DailyPlanner
```

### Publishing

```bash
# Self-contained single-file
dotnet publish DailyPlanner -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish

# Velopack package
vpk pack -u DailyPlanner -v 2.13.3 -p publish -o releases
```

## Tech stack

- **WPF** — native Windows UI
- **CommunityToolkit.Mvvm** — MVVM with source generators
- **Entity Framework Core 10** + SQLite — code-first with migrations
- **ClosedXML** — Excel export
- **Velopack** — auto-updates with delta packages
- **.NET 10**, C# 14

## Architecture

```
DailyPlanner/
├── Models/          # EF Core entities
├── Data/            # DbContext, migrations
├── ViewModels/      # MVVM ViewModels
├── Views/           # XAML pages
├── Services/        # Business logic, themes, export
├── Converters/      # WPF value converters
└── Themes/          # Color palettes (resource dictionaries)
```

Data stored locally: `%LOCALAPPDATA%\DailyPlanner\planner.db`

## License

MIT
