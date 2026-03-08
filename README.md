<p align="center">
  <img src="assets/banner.png" alt="Daily Planner" width="600"/>
</p>

<h1 align="center">Daily Planner</h1>

<p align="center">
  <b>Beautiful weekly planner for Windows</b><br/>
  WPF + .NET 10 + SQLite | Fully local, no cloud
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10_Preview-512BD4?style=flat-square&logo=dotnet" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/WPF-UI-7C5CFC?style=flat-square" alt="WPF-UI"/>
  <img src="https://img.shields.io/badge/SQLite-EF_Core-003B57?style=flat-square&logo=sqlite" alt="SQLite"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="MIT"/>
  <img src="https://img.shields.io/github/v/release/valinerosgordov/DailyPlanner?style=flat-square&color=7C5CFC" alt="Release"/>
</p>

---

## Features

| Feature | Description |
|---------|-------------|
| **Weekly planning** | 7 days with tasks, 4 weekly goals, notes |
| **Subtasks** | Nested tasks with progress and collapse/expand |
| **Meeting scheduler** | Title, attendees, description, date/time/duration |
| **Meeting reminders** | Notifications 1 day and 2 hours before a meeting |
| **Habit tracker** | Daily habits with weekly progress |
| **State monitoring** | Sleep, energy, mood (1-5) per day |
| **Priorities & categories** | Work, study, personal, health + 3 priority levels |
| **Drag & drop** | Move tasks between days |
| **Task move & delete** | Move to next day (with subtasks), delete tasks |
| **Recurring templates** | Auto-add tasks every week |
| **Pomodoro timer** | Customizable timer (work/break/focus) with sound |
| **Reminders** | Notifications at scheduled times |
| **Statistics** | Productivity charts, week comparison |
| **9 color palettes** | Catppuccin (Mocha, Frappe, Macchiato), Nord, Everforest, Coffee, Graphite, Obsidian, Light |
| **4 languages** | Russian, English, Spanish, French |
| **Excel export** | Export week to .xlsx file |
| **Search** | Quick search across tasks and goals (Ctrl+F) |
| **Daily quotes** | Motivational quote in sidebar |
| **Auto-updates** | Velopack + GitHub Releases (delta ~100 KB) |
| **Backup/Restore** | Auto-backup on startup + restore |
| **Keyboard shortcuts** | Ctrl+T (today), Ctrl+F (search), Ctrl+P (pomodoro) |

## Screenshots

> Add screenshots to `assets/` folder and uncomment:

<!--
<p align="center">
  <img src="assets/screenshot-dark.png" alt="Dark Theme" width="800"/>
</p>
<p align="center">
  <img src="assets/screenshot-light.png" alt="Light Theme" width="800"/>
</p>
-->

## Installation

### Installer (recommended)
1. Download `DailyPlanner-win-Setup.exe` from [Releases](../../releases/latest)
2. Run the installer
3. Desktop shortcut will be created
4. Updates install automatically

### Portable
1. Download `DailyPlanner-win-Portable.zip` from [Releases](../../releases/latest)
2. Extract to any folder
3. Run `DailyPlanner.exe`

## Build from source

```bash
# Requirements: .NET 10 SDK Preview
dotnet build --verbosity minimal
dotnet run --project DailyPlanner
```

### Publishing

```bash
# Self-contained single-file
dotnet publish DailyPlanner -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish

# Velopack package
vpk pack -u DailyPlanner -v 2.10.0 -p publish -o releases
```

## Tech stack

- **WPF** with [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- **CommunityToolkit.Mvvm** (MVVM, source generators)
- **Entity Framework Core** + SQLite (code-first, migrations)
- **ClosedXML** (Excel export)
- **Velopack** (auto-updates with delta packages)
- **.NET 10 Preview**, C# 13

## Architecture

```
DailyPlanner/
├── Models/          # Entity models (EF Core)
├── Data/            # DbContext, Factory, Migrations
├── ViewModels/      # MVVM ViewModels (CommunityToolkit)
├── Views/           # XAML Pages + code-behind
├── Services/        # Business logic, Theme, Pomodoro, Export
├── Converters/      # WPF value converters
└── Themes/          # Resource dictionaries
```

Data is stored locally at `%LOCALAPPDATA%\DailyPlanner\planner.db`.

## License

MIT
