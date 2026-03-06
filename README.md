<p align="center">
  <img src="assets/banner.png" alt="Daily Planner" width="600"/>
</p>

<h1 align="center">Daily Planner</h1>

<p align="center">
  <b>Красивый недельный планировщик для Windows</b><br/>
  WPF + .NET 10 + SQLite | Полностью локальный, без облака
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10_Preview-512BD4?style=flat-square&logo=dotnet" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/WPF-UI-7C5CFC?style=flat-square" alt="WPF-UI"/>
  <img src="https://img.shields.io/badge/SQLite-EF_Core-003B57?style=flat-square&logo=sqlite" alt="SQLite"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="MIT"/>
</p>

---

## Возможности

| Функция | Описание |
|---------|----------|
| **Недельное планирование** | 7 дней, 10 задач на день, 4 цели на неделю |
| **Трекер привычек** | Ежедневные привычки с heatmap-визуализацией и сериями |
| **Мониторинг состояния** | Сон, энергия, настроение (1-5) с графиками |
| **Приоритеты и категории** | Работа, учёба, личное, здоровье + три уровня приоритета |
| **Drag & Drop** | Перетаскивание задач между днями |
| **Перенос задач** | Незавершённые задачи переносятся на следующий день |
| **Повторяющиеся шаблоны** | Автоматическое добавление задач каждую неделю |
| **Помодоро-таймер** | Классический 25/5 + режим фокуса со звуковыми уведомлениями |
| **Напоминания** | Уведомления в заданное время |
| **Статистика** | Графики продуктивности, сравнение недель, heatmap привычек |
| **8 цветовых тем** | Фиолетовая, синяя, зелёная, розовая, оранжевая, бирюзовая, красная, золотая |
| **Тёмная/светлая тема** | Полная поддержка обоих режимов |
| **Экспорт в Excel** | Выгрузка недели в .xlsx файл |
| **Поиск** | Быстрый поиск по задачам и целям (Ctrl+F) |
| **Мотивационные цитаты** | Ежедневная цитата в сайдбаре |
| **Автообновления** | Velopack + GitHub Releases |
| **Backup/Restore** | Резервное копирование и восстановление базы данных |
| **Горячие клавиши** | Ctrl+T (сегодня), Ctrl+F (поиск), Ctrl+P (помодоро) и др. |

## Скриншоты

> Добавьте скриншоты в папку `assets/` и раскомментируйте:

<!--
<p align="center">
  <img src="assets/screenshot-dark.png" alt="Dark Theme" width="800"/>
</p>
<p align="center">
  <img src="assets/screenshot-light.png" alt="Light Theme" width="800"/>
</p>
-->

## Установка

### Установщик (рекомендуется)
1. Скачайте `DailyPlanner_Setup.exe` из [Releases](../../releases/latest)
2. Запустите установщик
3. Ярлык появится на рабочем столе

### Portable
1. Скачайте `DailyPlanner-win-Portable.zip` из [Releases](../../releases/latest)
2. Распакуйте в любую папку
3. Запустите `DailyPlanner.exe`

## Сборка из исходников

```bash
# Требования: .NET 10 SDK Preview
dotnet build --verbosity minimal
dotnet run --project DailyPlanner
```

### Публикация

```bash
# Self-contained single-file
dotnet publish DailyPlanner -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish

# Установщик (Inno Setup)
iscc installer.iss

# Velopack пакет
vpk pack --packId DailyPlanner --packVersion 2.1.1 --packDir publish --mainExe DailyPlanner.exe --outputDir ./releases
```

## Технологии

- **WPF** с [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- **CommunityToolkit.Mvvm** (MVVM, source generators)
- **Entity Framework Core** + SQLite (code-first, migrations)
- **ClosedXML** (Excel export)
- **Velopack** (auto-updates)
- **.NET 10 Preview**, C# 13

## Архитектура

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

Данные хранятся в `%LOCALAPPDATA%\DailyPlanner\planner.db`.

## Лицензия

MIT
