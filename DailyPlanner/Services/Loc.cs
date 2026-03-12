using System.ComponentModel;
using System.IO;

namespace DailyPlanner.Services;

/// <summary>
/// Localization service — singleton with indexer for XAML bindings.
/// Usage in XAML:  {Binding [Key], Source={x:Static services:Loc.Instance}}
/// Usage in code:  Loc.Get("Key")
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _lang = "ru";
    public string Language
    {
        get => _lang;
        set
        {
            if (_lang == value) return;
            _lang = value;
            Save();
            // Notify all bindings via indexer
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            LanguageChanged?.Invoke();
        }
    }

    public static event Action? LanguageChanged;

    public string this[string key] => Get(key);

    public static string Get(string key)
    {
        var lang = Instance._lang;
        if (Translations.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        // Fallback to Russian
        if (Translations.TryGetValue("ru", out var ruDict) && ruDict.TryGetValue(key, out var ruVal))
            return ruVal;
        return key;
    }

    public static readonly string[] SupportedLanguages = ["ru", "en", "es", "fr"];
    public static readonly Dictionary<string, string> LanguageNames = new()
    {
        ["ru"] = "Русский",
        ["en"] = "English",
        ["es"] = "Español",
        ["fr"] = "Français"
    };

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DailyPlanner", "language.txt");

    public Loc()
    {
        _lang = LoadSaved();
    }

    private static string LoadSaved()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var lang = File.ReadAllText(SettingsPath).Trim();
                if (SupportedLanguages.Contains(lang)) return lang;
            }
        }
        catch { }
        return "ru";
    }

    private static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsPath, Instance._lang);
        }
        catch { }
    }

    // ─── All translations ────────────────────────────────────────────
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["ru"] = new()
        {
            // Sidebar
            ["Today"] = "СЕГОДНЯ",
            ["TodayBtn"] = "Сегодня",
            ["Statistics"] = "Статистика",
            ["Export"] = "Экспорт",
            ["Settings"] = "Настройки",
            ["Pomodoro"] = "Pomodoro",

            // Tray
            ["TrayOpen"] = "Открыть",
            ["TrayToday"] = "Сегодня",
            ["TrayExit"] = "Выход",
            ["TrayMinimized"] = "Приложение свёрнуто в трей",

            // Search
            ["SearchTooltip"] = "Поиск (Ctrl+F)",
            ["CopyWeekTooltip"] = "Копировать структуру прошлой недели",

            // Today widget
            ["NoTasksToday"] = "Нет задач на сегодня",
            ["NoTasks"] = "Нет задач",
            ["Completed"] = "выполнено",

            // Stat cards (week)
            ["TotalTasks"] = "Всего задач",
            ["CompletedLabel"] = "Выполнено",
            ["NotCompleted"] = "Не выполнено",
            ["GoalsReached"] = "Целей достигнуто",
            ["TotalTasksTip"] = "Количество задач с текстом за неделю",
            ["CompletedTip"] = "Задач, отмеченных как выполненные",
            ["NotCompletedTip"] = "Задач, которые ещё не выполнены",
            ["GoalsTip"] = "Выполненных целей из 4",

            // Sections
            ["WeekGoals"] = "ЦЕЛИ НЕДЕЛИ",
            ["Notes"] = "ЗАМЕТКИ",
            ["Progress"] = "ПРОГРЕСС",
            ["ProgressLabel"] = "прогресс",
            ["BestDay"] = "лучший день",
            ["GoalsLabel"] = "целей",
            ["WeekTasks"] = "ЗАДАЧИ НА НЕДЕЛЮ",
            ["TodayBadge"] = "СЕГОДНЯ",
            ["Remaining"] = "осталось",
            ["CarryOverTip"] = "Перенести незавершённые на завтра",
            ["AddSubTaskTip"] = "Добавить подзадачу",
            ["DeleteTaskTip"] = "Удалить задачу",
            ["MoveNextDayTip"] = "Перенести на следующий день",
            ["HabitTracker"] = "ТРЕКЕР ПРИВЫЧЕК",
            ["StateTracker"] = "ТРЕКЕР СОСТОЯНИЯ",
            ["AddNoteTip"] = "Добавить заметку",
            ["AddGoalTip"] = "Добавить цель",
            ["DeleteTip"] = "Удалить",
            ["AddHabitTip"] = "Добавить привычку",
            ["HabitEmpty"] = "Нажмите + чтобы добавить привычку",
            ["HabitLabel"] = "Привычка",
            ["ProgressCol"] = "Прогресс",

            // Day names
            ["Monday"] = "Понедельник",
            ["Tuesday"] = "Вторник",
            ["Wednesday"] = "Среда",
            ["Thursday"] = "Четверг",
            ["Friday"] = "Пятница",
            ["Saturday"] = "Суббота",
            ["Sunday"] = "Воскресенье",
            ["Mon"] = "Пн",
            ["Tue"] = "Вт",
            ["Wed"] = "Ср",
            ["Thu"] = "Чт",
            ["Fri"] = "Пт",
            ["Sat"] = "Сб",
            ["Sun"] = "Вс",

            // State columns
            ["Sleep"] = "Сон",
            ["Energy"] = "Энергия",
            ["Mood"] = "Настрой",

            // Month names
            ["Month1"] = "Январь", ["Month2"] = "Февраль", ["Month3"] = "Март",
            ["Month4"] = "Апрель", ["Month5"] = "Май", ["Month6"] = "Июнь",
            ["Month7"] = "Июль", ["Month8"] = "Август", ["Month9"] = "Сентябрь",
            ["Month10"] = "Октябрь", ["Month11"] = "Ноябрь", ["Month12"] = "Декабрь",

            // Pomodoro
            ["PomWork"] = "Работа",
            ["PomBreak"] = "Перерыв",
            ["PomFocus"] = "Фокус",
            ["PomPomodoro"] = "Помодоро",
            ["PomFocusMode"] = "Фокус-режим",
            ["PomReset"] = "Сброс",
            ["PomSkip"] = "Пропустить",
            ["PomToggleFocus"] = "Переключить Помодоро/Фокус",
            ["PomSessions"] = "Сессий: ",
            ["PomPause"] = "\u23F8 Пауза",
            ["PomStart"] = "\u25B6 Старт",
            ["PomBreakTime"] = "Время перерыва! Отдохни \U0001f3d6\ufe0f",
            ["PomWorkTime"] = "Время работать! \U0001f4aa",
            ["PomFocusTimer"] = "Фокус-таймер",
            ["PomFocusAlert"] = "Уже {0} мин! Сделай перерыв \u2615",

            // Priority tooltips
            ["PriorityHigh"] = "Высокий приоритет (клик — сменить)",
            ["PriorityMedium"] = "Средний приоритет (клик — сменить)",
            ["PriorityLow"] = "Низкий приоритет (клик — сменить)",
            ["PriorityNone"] = "Без приоритета",

            // Category tooltips
            ["CatWork"] = "Работа (клик — сменить)",
            ["CatStudy"] = "Учёба (клик — сменить)",
            ["CatPersonal"] = "Личное (клик — сменить)",
            ["CatHealth"] = "Здоровье (клик — сменить)",
            ["CatOther"] = "Другое (клик — сменить)",

            // Settings
            ["SettingsTitle"] = "Настройки",
            ["ExportExcel"] = "Экспорт в Excel",
            ["ExportExcelDesc"] = "Выгрузить текущую неделю в .xlsx файл",
            ["Palette"] = "Палитра",
            ["PaletteCurrent"] = "Текущая: ",
            ["AutoStart"] = "Автозапуск",
            ["AutoStartDesc"] = "Запускать приложение при входе в Windows",
            ["Updates"] = "Обновления",
            ["CheckUpdates"] = "Проверить",
            ["UpdateBtn"] = "Обновить",
            ["Backup"] = "Резервное копирование",
            ["BackupDesc"] = "Создайте копию или восстановите базу данных",
            ["CreateBackup"] = "Создать копию",
            ["Restore"] = "Восстановить",
            ["Reminders"] = "Напоминания",
            ["RemindersDesc"] = "Уведомления в заданное время",
            ["Add"] = "Добавить",
            ["TimeTip"] = "Время (ЧЧ:ММ)",
            ["TitleTip"] = "Заголовок",
            ["MessageTip"] = "Сообщение",
            ["RecurringTasks"] = "Повторяющиеся задачи",
            ["RecurringDesc"] = "Шаблоны автоматически добавляются каждую неделю",
            ["AboutApp"] = "О приложении",
            ["Version"] = "Версия",
            ["Platform"] = "Платформа",
            ["Database"] = "База данных",
            ["DbLocal"] = "SQLite (локальная)",
            ["Storage"] = "Хранение",
            ["Shortcuts"] = "Горячие клавиши",
            ["SwitchMonth"] = "Переключить месяц",
            ["GoToToday"] = "Перейти к сегодня",
            ["SearchTasks"] = "Поиск по задачам",
            ["PomodoroTimer"] = "Помодоро таймер",
            ["ExportExcelShort"] = "Экспорт в Excel",
            ["OpenSettings"] = "Открыть настройки",
            ["Description"] = "Описание",
            ["AppDescription"] = "Daily Planner — недельный планер с трекером привычек, аналитикой продуктивности и мониторингом состояния (сон, энергия, настроение). Все данные хранятся локально в SQLite базе данных. Приложение поддерживает навигацию по месяцам и неделям, автоматический подсчёт статистики, экспорт в Excel, поиск по задачам и визуализацию прогресса через круговую диаграмму.",
            ["Language"] = "Язык",
            ["LanguageDesc"] = "Язык интерфейса приложения",

            // Statistics
            ["Productivity"] = "Продуктивность",
            ["BestDayLabel"] = "Лучший день",
            ["Goals"] = "Цели",
            ["AvgSleep"] = "Ср. сон",
            ["AvgEnergy"] = "Ср. энергия",
            ["AvgMood"] = "Ср. настроение",
            ["WeekComparison"] = "Сравнение недель",
            ["PrevWeek"] = "Прошлая неделя",
            ["Trend"] = "Тренд",
            ["CurrWeek"] = "Текущая неделя",
            ["WeeklyProductivity"] = "Продуктивность по неделям",
            ["HabitHeatmap"] = "Привычки — Heatmap (30 дней)",
            ["HabitStreaks"] = "Серии привычек",
            ["TasksUnit"] = "задач",
            ["NoChange"] = "Без изменений",
            ["DaysUnit"] = "дн.",

            // Weekly summary
            ["SummaryTasks"] = "Задачи: {0}/{1}",
            ["SummaryGoals"] = "Цели: {0}/{1}",
            ["SummaryBestDay"] = "Лучший день: {0}",
            ["SummaryExcellent"] = "\u2b50 Отличная неделя!",
            ["SummaryGood"] = "\u2705 Хорошая неделя!",
            ["SummaryCanBetter"] = "\U0001f4aa Можно лучше!",

            // Notifications/toast
            ["ExportTitle"] = "Экспорт",
            ["ExportSuccess"] = "Файл успешно сохранён",
            ["ExportError"] = "Ошибка при экспорте",
            ["RestoreTitle"] = "Восстановление",
            ["RestoreSuccess"] = "База данных успешно восстановлена",
            ["RestoreError"] = "Ошибка при восстановлении",
            ["RestoreInvalidDb"] = "Файл не является базой данных SQLite",
            ["CopyTitle"] = "Копирование",
            ["CopySuccess"] = "Структура прошлой недели скопирована",
            ["Reminder"] = "Напоминание",
            ["MoveTitle"] = "Перемещение",
            ["MoveNoSlots"] = "Нет свободных слотов в этом дне",

            // Updates
            ["UpdateUnavailable"] = "Обновления недоступны (dev-режим)",
            ["UpdateChecking"] = "Проверка обновлений...",
            ["UpdateLatest"] = "Вы используете последнюю версию",
            ["UpdateAvailable"] = "Доступно обновление: v{0}",
            ["UpdateDownloading"] = "Скачивание обновления...",
            ["UpdateProgress"] = "Скачивание: {0}%",

            // Export service
            ["ExpWeek"] = "Неделя {0}",
            ["ExpPlannerWeek"] = "Планер — Неделя {0} – {1}",
            ["ExpWeekGoals"] = "Цели недели",
            ["ExpDay"] = "День",
            ["ExpTask"] = "Задача {0}",
            ["ExpState"] = "Состояние",
            ["ExpHabits"] = "Привычки",
            ["ExpNotes"] = "Заметки",

            // Splash / errors
            ["Loading"] = "Загрузка данных...",
            ["DbError"] = "Ошибка инициализации базы данных:\n{0}",

            // Save dialog
            ["PlannerFileName"] = "Планер_{0}",
            ["ExcelFilter"] = "Excel файлы (*.xlsx)|*.xlsx",

            // Search context
            ["Goal"] = "Цель",

            // Light theme
            ["LightTheme"] = "Светлая",

            // Quotes
            ["Quote1"] = "Маленькие шаги каждый день приводят к большим результатам.",
            ["Quote2"] = "Дисциплина — это мост между целями и достижениями.",
            ["Quote3"] = "Не жди идеального момента. Бери момент и делай его идеальным.",
            ["Quote4"] = "Успех — это сумма маленьких усилий, повторяемых изо дня в день.",
            ["Quote5"] = "Планирование — это приведение будущего в настоящее.",
            ["Quote6"] = "Каждый день — это новая возможность стать лучше.",
            ["Quote7"] = "Делай сегодня то, что другие не хотят. Завтра будешь жить так, как другие не могут.",
            ["Quote8"] = "Фокус — это не то, чему ты говоришь 'да', а то, чему говоришь 'нет'.",
            ["Quote9"] = "Привычки определяют будущее. Выбирай их мудро.",
            ["Quote10"] = "Прогресс, а не совершенство.",
            ["Quote11"] = "Лучшее время начать — прямо сейчас.",
            ["Quote12"] = "Твой единственный конкурент — ты вчерашний.",
            ["Quote13"] = "Энергия следует за вниманием. Направь внимание на важное.",
            ["Quote14"] = "Без плана ты планируешь потерпеть неудачу.",
            ["Quote15"] = "Великие дела начинаются с малого.",
            ["Quote16"] = "Сосредоточься на процессе, а результат придёт.",
            ["Quote17"] = "Каждая выполненная задача — маленькая победа.",
            ["Quote18"] = "Не откладывай на завтра то, что сделает тебя лучше сегодня.",
            ["Quote19"] = "Продуктивность — это не занятость, а результат.",
            ["Quote20"] = "Один час планирования экономит десять часов работы.",

            // Meetings
            ["Meetings"] = "Встречи",
            ["MeetingsDesc"] = "Планировщик встреч с напоминаниями",
            ["NewMeeting"] = "Новая встреча",
            ["MeetingTitle"] = "Название",
            ["MeetingDesc"] = "Описание",
            ["MeetingAttendees"] = "Участники",
            ["MeetingDate"] = "Дата",
            ["MeetingTime"] = "Время",
            ["MeetingDuration"] = "Длительность",
            ["MeetingMin"] = "мин",
            ["MeetingNotifyDay"] = "За день",
            ["MeetingNotify2h"] = "За 2 часа",
            ["MeetingNotify30m"] = "За 30 мин",
            ["MeetingTomorrow"] = "Встреча завтра",
            ["MeetingSoon"] = "Встреча скоро",
            ["MeetingIn2Hours"] = "через 2 часа",
            ["MeetingIn30Min"] = "через 30 минут",
            ["MeetingNoMeetings"] = "Нет запланированных встреч",
            ["MeetingUpcoming"] = "Предстоящие",
            ["MeetingPast"] = "Прошедшие",
        },

        ["en"] = new()
        {
            ["Today"] = "TODAY",
            ["TodayBtn"] = "Today",
            ["Statistics"] = "Statistics",
            ["Export"] = "Export",
            ["Settings"] = "Settings",
            ["Pomodoro"] = "Pomodoro",

            ["TrayOpen"] = "Open",
            ["TrayToday"] = "Today",
            ["TrayExit"] = "Exit",
            ["TrayMinimized"] = "App minimized to tray",

            ["SearchTooltip"] = "Search (Ctrl+F)",
            ["CopyWeekTooltip"] = "Copy last week's structure",

            ["NoTasksToday"] = "No tasks for today",
            ["NoTasks"] = "No tasks",
            ["Completed"] = "completed",

            ["TotalTasks"] = "Total tasks",
            ["CompletedLabel"] = "Completed",
            ["NotCompleted"] = "Not completed",
            ["GoalsReached"] = "Goals reached",
            ["TotalTasksTip"] = "Number of tasks with text for the week",
            ["CompletedTip"] = "Tasks marked as completed",
            ["NotCompletedTip"] = "Tasks not yet completed",
            ["GoalsTip"] = "Completed goals out of 4",

            ["WeekGoals"] = "WEEK GOALS",
            ["Notes"] = "NOTES",
            ["Progress"] = "PROGRESS",
            ["ProgressLabel"] = "progress",
            ["BestDay"] = "best day",
            ["GoalsLabel"] = "goals",
            ["WeekTasks"] = "WEEKLY TASKS",
            ["TodayBadge"] = "TODAY",
            ["Remaining"] = "remaining",
            ["CarryOverTip"] = "Carry over unfinished to tomorrow",
            ["AddSubTaskTip"] = "Add subtask",
            ["DeleteTaskTip"] = "Delete task",
            ["MoveNextDayTip"] = "Move to next day",
            ["HabitTracker"] = "HABIT TRACKER",
            ["StateTracker"] = "STATE TRACKER",
            ["AddNoteTip"] = "Add note",
            ["AddGoalTip"] = "Add goal",
            ["DeleteTip"] = "Delete",
            ["AddHabitTip"] = "Add habit",
            ["HabitEmpty"] = "Press + to add a habit",
            ["HabitLabel"] = "Habit",
            ["ProgressCol"] = "Progress",

            ["Monday"] = "Monday", ["Tuesday"] = "Tuesday", ["Wednesday"] = "Wednesday",
            ["Thursday"] = "Thursday", ["Friday"] = "Friday", ["Saturday"] = "Saturday", ["Sunday"] = "Sunday",
            ["Mon"] = "Mo", ["Tue"] = "Tu", ["Wed"] = "We", ["Thu"] = "Th",
            ["Fri"] = "Fr", ["Sat"] = "Sa", ["Sun"] = "Su",

            ["Sleep"] = "Sleep", ["Energy"] = "Energy", ["Mood"] = "Mood",

            ["Month1"] = "January", ["Month2"] = "February", ["Month3"] = "March",
            ["Month4"] = "April", ["Month5"] = "May", ["Month6"] = "June",
            ["Month7"] = "July", ["Month8"] = "August", ["Month9"] = "September",
            ["Month10"] = "October", ["Month11"] = "November", ["Month12"] = "December",

            ["PomWork"] = "Work", ["PomBreak"] = "Break", ["PomFocus"] = "Focus",
            ["PomPomodoro"] = "Pomodoro", ["PomFocusMode"] = "Focus mode",
            ["PomReset"] = "Reset", ["PomSkip"] = "Skip",
            ["PomToggleFocus"] = "Toggle Pomodoro/Focus",
            ["PomSessions"] = "Sessions: ",
            ["PomPause"] = "\u23F8 Pause", ["PomStart"] = "\u25B6 Start",
            ["PomBreakTime"] = "Break time! Rest up \U0001f3d6\ufe0f",
            ["PomWorkTime"] = "Time to work! \U0001f4aa",
            ["PomFocusTimer"] = "Focus Timer",
            ["PomFocusAlert"] = "Already {0} min! Take a break \u2615",

            ["PriorityHigh"] = "High priority (click to change)",
            ["PriorityMedium"] = "Medium priority (click to change)",
            ["PriorityLow"] = "Low priority (click to change)",
            ["PriorityNone"] = "No priority",

            ["CatWork"] = "Work (click to change)", ["CatStudy"] = "Study (click to change)",
            ["CatPersonal"] = "Personal (click to change)", ["CatHealth"] = "Health (click to change)",
            ["CatOther"] = "Other (click to change)",

            ["SettingsTitle"] = "Settings",
            ["ExportExcel"] = "Export to Excel",
            ["ExportExcelDesc"] = "Export the current week to .xlsx file",
            ["Palette"] = "Palette",
            ["PaletteCurrent"] = "Current: ",
            ["AutoStart"] = "Auto start",
            ["AutoStartDesc"] = "Launch the app on Windows login",
            ["Updates"] = "Updates",
            ["CheckUpdates"] = "Check",
            ["UpdateBtn"] = "Update",
            ["Backup"] = "Backup",
            ["BackupDesc"] = "Create a copy or restore the database",
            ["CreateBackup"] = "Create backup",
            ["Restore"] = "Restore",
            ["Reminders"] = "Reminders",
            ["RemindersDesc"] = "Notifications at specified time",
            ["Add"] = "Add",
            ["TimeTip"] = "Time (HH:MM)",
            ["TitleTip"] = "Title",
            ["MessageTip"] = "Message",
            ["RecurringTasks"] = "Recurring tasks",
            ["RecurringDesc"] = "Templates are automatically added every week",
            ["AboutApp"] = "About",
            ["Version"] = "Version",
            ["Platform"] = "Platform",
            ["Database"] = "Database",
            ["DbLocal"] = "SQLite (local)",
            ["Storage"] = "Storage",
            ["Shortcuts"] = "Keyboard shortcuts",
            ["SwitchMonth"] = "Switch month",
            ["GoToToday"] = "Go to today",
            ["SearchTasks"] = "Search tasks",
            ["PomodoroTimer"] = "Pomodoro timer",
            ["ExportExcelShort"] = "Export to Excel",
            ["OpenSettings"] = "Open settings",
            ["Description"] = "Description",
            ["AppDescription"] = "Daily Planner — a weekly planner with habit tracker, productivity analytics, and state monitoring (sleep, energy, mood). All data is stored locally in a SQLite database. The app supports month and week navigation, automatic statistics, Excel export, task search, and progress visualization via donut chart.",
            ["Language"] = "Language",
            ["LanguageDesc"] = "Application interface language",

            ["Productivity"] = "Productivity",
            ["BestDayLabel"] = "Best day",
            ["Goals"] = "Goals",
            ["AvgSleep"] = "Avg. sleep",
            ["AvgEnergy"] = "Avg. energy",
            ["AvgMood"] = "Avg. mood",
            ["WeekComparison"] = "Week comparison",
            ["PrevWeek"] = "Previous week",
            ["Trend"] = "Trend",
            ["CurrWeek"] = "Current week",
            ["WeeklyProductivity"] = "Weekly productivity",
            ["HabitHeatmap"] = "Habits — Heatmap (30 days)",
            ["HabitStreaks"] = "Habit streaks",
            ["TasksUnit"] = "tasks",
            ["NoChange"] = "No change",
            ["DaysUnit"] = "d.",

            ["SummaryTasks"] = "Tasks: {0}/{1}",
            ["SummaryGoals"] = "Goals: {0}/{1}",
            ["SummaryBestDay"] = "Best day: {0}",
            ["SummaryExcellent"] = "\u2b50 Excellent week!",
            ["SummaryGood"] = "\u2705 Good week!",
            ["SummaryCanBetter"] = "\U0001f4aa Can do better!",

            ["ExportTitle"] = "Export",
            ["ExportSuccess"] = "File saved successfully",
            ["ExportError"] = "Export error",
            ["RestoreTitle"] = "Restore",
            ["RestoreSuccess"] = "Database restored successfully",
            ["RestoreError"] = "Error restoring database",
            ["RestoreInvalidDb"] = "File is not a valid SQLite database",
            ["CopyTitle"] = "Copy",
            ["CopySuccess"] = "Last week's structure copied",
            ["Reminder"] = "Reminder",
            ["MoveTitle"] = "Move",
            ["MoveNoSlots"] = "No empty slots in this day",

            ["UpdateUnavailable"] = "Updates unavailable (dev mode)",
            ["UpdateChecking"] = "Checking for updates...",
            ["UpdateLatest"] = "You are using the latest version",
            ["UpdateAvailable"] = "Update available: v{0}",
            ["UpdateDownloading"] = "Downloading update...",
            ["UpdateProgress"] = "Downloading: {0}%",

            ["ExpWeek"] = "Week {0}",
            ["ExpPlannerWeek"] = "Planner — Week {0} – {1}",
            ["ExpWeekGoals"] = "Week goals",
            ["ExpDay"] = "Day",
            ["ExpTask"] = "Task {0}",
            ["ExpState"] = "State",
            ["ExpHabits"] = "Habits",
            ["ExpNotes"] = "Notes",

            ["Loading"] = "Loading data...",
            ["DbError"] = "Database initialization error:\n{0}",
            ["PlannerFileName"] = "Planner_{0}",
            ["ExcelFilter"] = "Excel files (*.xlsx)|*.xlsx",
            ["Goal"] = "Goal",
            ["LightTheme"] = "Light",

            ["Quote1"] = "Small steps every day lead to big results.",
            ["Quote2"] = "Discipline is the bridge between goals and accomplishment.",
            ["Quote3"] = "Don't wait for the perfect moment. Take the moment and make it perfect.",
            ["Quote4"] = "Success is the sum of small efforts repeated day in and day out.",
            ["Quote5"] = "Planning is bringing the future into the present.",
            ["Quote6"] = "Every day is a new opportunity to become better.",
            ["Quote7"] = "Do today what others won't. Tomorrow you'll live as others can't.",
            ["Quote8"] = "Focus is not about saying 'yes', it's about saying 'no'.",
            ["Quote9"] = "Habits define the future. Choose them wisely.",
            ["Quote10"] = "Progress, not perfection.",
            ["Quote11"] = "The best time to start is right now.",
            ["Quote12"] = "Your only competitor is who you were yesterday.",
            ["Quote13"] = "Energy follows attention. Focus on what matters.",
            ["Quote14"] = "Without a plan you are planning to fail.",
            ["Quote15"] = "Great things start small.",
            ["Quote16"] = "Focus on the process and results will follow.",
            ["Quote17"] = "Every completed task is a small victory.",
            ["Quote18"] = "Don't put off until tomorrow what will make you better today.",
            ["Quote19"] = "Productivity is not busyness, it's results.",
            ["Quote20"] = "One hour of planning saves ten hours of work.",

            // Meetings
            ["Meetings"] = "Meetings",
            ["MeetingsDesc"] = "Meeting scheduler with reminders",
            ["NewMeeting"] = "New meeting",
            ["MeetingTitle"] = "Title",
            ["MeetingDesc"] = "Description",
            ["MeetingAttendees"] = "Attendees",
            ["MeetingDate"] = "Date",
            ["MeetingTime"] = "Time",
            ["MeetingDuration"] = "Duration",
            ["MeetingMin"] = "min",
            ["MeetingNotifyDay"] = "1 day before",
            ["MeetingNotify2h"] = "2 hours before",
            ["MeetingNotify30m"] = "30 min before",
            ["MeetingTomorrow"] = "Meeting tomorrow",
            ["MeetingSoon"] = "Meeting soon",
            ["MeetingIn2Hours"] = "in 2 hours",
            ["MeetingIn30Min"] = "in 30 minutes",
            ["MeetingNoMeetings"] = "No scheduled meetings",
            ["MeetingUpcoming"] = "Upcoming",
            ["MeetingPast"] = "Past",
        },

        ["es"] = new()
        {
            ["Today"] = "HOY",
            ["TodayBtn"] = "Hoy",
            ["Statistics"] = "Estadísticas",
            ["Export"] = "Exportar",
            ["Settings"] = "Configuración",
            ["Pomodoro"] = "Pomodoro",

            ["TrayOpen"] = "Abrir",
            ["TrayToday"] = "Hoy",
            ["TrayExit"] = "Salir",
            ["TrayMinimized"] = "Aplicación minimizada a la bandeja",

            ["SearchTooltip"] = "Buscar (Ctrl+F)",
            ["CopyWeekTooltip"] = "Copiar estructura de la semana pasada",

            ["NoTasksToday"] = "Sin tareas para hoy",
            ["NoTasks"] = "Sin tareas",
            ["Completed"] = "completadas",

            ["TotalTasks"] = "Total de tareas",
            ["CompletedLabel"] = "Completadas",
            ["NotCompleted"] = "No completadas",
            ["GoalsReached"] = "Metas alcanzadas",
            ["TotalTasksTip"] = "Número de tareas con texto en la semana",
            ["CompletedTip"] = "Tareas marcadas como completadas",
            ["NotCompletedTip"] = "Tareas aún no completadas",
            ["GoalsTip"] = "Metas completadas de 4",

            ["WeekGoals"] = "METAS DE LA SEMANA",
            ["Notes"] = "NOTAS",
            ["Progress"] = "PROGRESO",
            ["ProgressLabel"] = "progreso",
            ["BestDay"] = "mejor día",
            ["GoalsLabel"] = "metas",
            ["WeekTasks"] = "TAREAS DE LA SEMANA",
            ["TodayBadge"] = "HOY",
            ["Remaining"] = "restantes",
            ["CarryOverTip"] = "Pasar pendientes a mañana",
            ["AddSubTaskTip"] = "Agregar subtarea",
            ["DeleteTaskTip"] = "Eliminar tarea",
            ["MoveNextDayTip"] = "Mover al día siguiente",
            ["HabitTracker"] = "SEGUIMIENTO DE HÁBITOS",
            ["StateTracker"] = "SEGUIMIENTO DE ESTADO",
            ["AddNoteTip"] = "Añadir nota",
            ["AddGoalTip"] = "Añadir objetivo",
            ["DeleteTip"] = "Eliminar",
            ["AddHabitTip"] = "Añadir hábito",
            ["HabitEmpty"] = "Pulse + para añadir un hábito",
            ["HabitLabel"] = "Hábito",
            ["ProgressCol"] = "Progreso",

            ["Monday"] = "Lunes", ["Tuesday"] = "Martes", ["Wednesday"] = "Miércoles",
            ["Thursday"] = "Jueves", ["Friday"] = "Viernes", ["Saturday"] = "Sábado", ["Sunday"] = "Domingo",
            ["Mon"] = "Lu", ["Tue"] = "Ma", ["Wed"] = "Mi", ["Thu"] = "Ju",
            ["Fri"] = "Vi", ["Sat"] = "Sá", ["Sun"] = "Do",

            ["Sleep"] = "Sueño", ["Energy"] = "Energía", ["Mood"] = "Ánimo",

            ["Month1"] = "Enero", ["Month2"] = "Febrero", ["Month3"] = "Marzo",
            ["Month4"] = "Abril", ["Month5"] = "Mayo", ["Month6"] = "Junio",
            ["Month7"] = "Julio", ["Month8"] = "Agosto", ["Month9"] = "Septiembre",
            ["Month10"] = "Octubre", ["Month11"] = "Noviembre", ["Month12"] = "Diciembre",

            ["PomWork"] = "Trabajo", ["PomBreak"] = "Descanso", ["PomFocus"] = "Enfoque",
            ["PomPomodoro"] = "Pomodoro", ["PomFocusMode"] = "Modo enfoque",
            ["PomReset"] = "Reiniciar", ["PomSkip"] = "Saltar",
            ["PomToggleFocus"] = "Alternar Pomodoro/Enfoque",
            ["PomSessions"] = "Sesiones: ",
            ["PomPause"] = "\u23F8 Pausa", ["PomStart"] = "\u25B6 Iniciar",
            ["PomBreakTime"] = "¡Hora del descanso! Relájate \U0001f3d6\ufe0f",
            ["PomWorkTime"] = "¡Hora de trabajar! \U0001f4aa",
            ["PomFocusTimer"] = "Temporizador de enfoque",
            ["PomFocusAlert"] = "¡Ya {0} min! Toma un descanso \u2615",

            ["PriorityHigh"] = "Prioridad alta (clic para cambiar)",
            ["PriorityMedium"] = "Prioridad media (clic para cambiar)",
            ["PriorityLow"] = "Prioridad baja (clic para cambiar)",
            ["PriorityNone"] = "Sin prioridad",

            ["CatWork"] = "Trabajo (clic para cambiar)", ["CatStudy"] = "Estudio (clic para cambiar)",
            ["CatPersonal"] = "Personal (clic para cambiar)", ["CatHealth"] = "Salud (clic para cambiar)",
            ["CatOther"] = "Otro (clic para cambiar)",

            ["SettingsTitle"] = "Configuración",
            ["ExportExcel"] = "Exportar a Excel",
            ["ExportExcelDesc"] = "Exportar la semana actual a archivo .xlsx",
            ["Palette"] = "Paleta",
            ["PaletteCurrent"] = "Actual: ",
            ["AutoStart"] = "Inicio automático",
            ["AutoStartDesc"] = "Iniciar la aplicación al entrar en Windows",
            ["Updates"] = "Actualizaciones",
            ["CheckUpdates"] = "Comprobar",
            ["UpdateBtn"] = "Actualizar",
            ["Backup"] = "Copia de seguridad",
            ["BackupDesc"] = "Crear una copia o restaurar la base de datos",
            ["CreateBackup"] = "Crear copia",
            ["Restore"] = "Restaurar",
            ["Reminders"] = "Recordatorios",
            ["RemindersDesc"] = "Notificaciones a la hora indicada",
            ["Add"] = "Añadir",
            ["TimeTip"] = "Hora (HH:MM)",
            ["TitleTip"] = "Título",
            ["MessageTip"] = "Mensaje",
            ["RecurringTasks"] = "Tareas recurrentes",
            ["RecurringDesc"] = "Las plantillas se añaden automáticamente cada semana",
            ["AboutApp"] = "Acerca de",
            ["Version"] = "Versión",
            ["Platform"] = "Plataforma",
            ["Database"] = "Base de datos",
            ["DbLocal"] = "SQLite (local)",
            ["Storage"] = "Almacenamiento",
            ["Shortcuts"] = "Atajos de teclado",
            ["SwitchMonth"] = "Cambiar mes",
            ["GoToToday"] = "Ir a hoy",
            ["SearchTasks"] = "Buscar tareas",
            ["PomodoroTimer"] = "Temporizador Pomodoro",
            ["ExportExcelShort"] = "Exportar a Excel",
            ["OpenSettings"] = "Abrir configuración",
            ["Description"] = "Descripción",
            ["AppDescription"] = "Daily Planner — un planificador semanal con seguimiento de hábitos, análisis de productividad y monitoreo de estado (sueño, energía, ánimo). Todos los datos se almacenan localmente en una base de datos SQLite. La aplicación soporta navegación por meses y semanas, estadísticas automáticas, exportación a Excel, búsqueda de tareas y visualización del progreso.",
            ["Language"] = "Idioma",
            ["LanguageDesc"] = "Idioma de la interfaz de la aplicación",

            ["Productivity"] = "Productividad",
            ["BestDayLabel"] = "Mejor día",
            ["Goals"] = "Metas",
            ["AvgSleep"] = "Prom. sueño",
            ["AvgEnergy"] = "Prom. energía",
            ["AvgMood"] = "Prom. ánimo",
            ["WeekComparison"] = "Comparación semanal",
            ["PrevWeek"] = "Semana anterior",
            ["Trend"] = "Tendencia",
            ["CurrWeek"] = "Semana actual",
            ["WeeklyProductivity"] = "Productividad semanal",
            ["HabitHeatmap"] = "Hábitos — Mapa de calor (30 días)",
            ["HabitStreaks"] = "Rachas de hábitos",
            ["TasksUnit"] = "tareas",
            ["NoChange"] = "Sin cambios",
            ["DaysUnit"] = "d.",

            ["SummaryTasks"] = "Tareas: {0}/{1}",
            ["SummaryGoals"] = "Metas: {0}/{1}",
            ["SummaryBestDay"] = "Mejor día: {0}",
            ["SummaryExcellent"] = "\u2b50 ¡Semana excelente!",
            ["SummaryGood"] = "\u2705 ¡Buena semana!",
            ["SummaryCanBetter"] = "\U0001f4aa ¡Se puede mejorar!",

            ["ExportTitle"] = "Exportar",
            ["ExportSuccess"] = "Archivo guardado correctamente",
            ["ExportError"] = "Error al exportar",
            ["RestoreTitle"] = "Restaurar",
            ["RestoreSuccess"] = "Base de datos restaurada correctamente",
            ["RestoreError"] = "Error al restaurar la base de datos",
            ["RestoreInvalidDb"] = "El archivo no es una base de datos SQLite válida",
            ["CopyTitle"] = "Copiar",
            ["CopySuccess"] = "Estructura de la semana pasada copiada",
            ["Reminder"] = "Recordatorio",
            ["MoveTitle"] = "Mover",
            ["MoveNoSlots"] = "No hay espacios libres en este día",

            ["UpdateUnavailable"] = "Actualizaciones no disponibles (modo dev)",
            ["UpdateChecking"] = "Comprobando actualizaciones...",
            ["UpdateLatest"] = "Estás usando la última versión",
            ["UpdateAvailable"] = "Actualización disponible: v{0}",
            ["UpdateDownloading"] = "Descargando actualización...",
            ["UpdateProgress"] = "Descargando: {0}%",

            ["ExpWeek"] = "Semana {0}",
            ["ExpPlannerWeek"] = "Planificador — Semana {0} – {1}",
            ["ExpWeekGoals"] = "Metas de la semana",
            ["ExpDay"] = "Día",
            ["ExpTask"] = "Tarea {0}",
            ["ExpState"] = "Estado",
            ["ExpHabits"] = "Hábitos",
            ["ExpNotes"] = "Notas",

            ["Loading"] = "Cargando datos...",
            ["DbError"] = "Error al inicializar la base de datos:\n{0}",
            ["PlannerFileName"] = "Planificador_{0}",
            ["ExcelFilter"] = "Archivos Excel (*.xlsx)|*.xlsx",
            ["Goal"] = "Meta",
            ["LightTheme"] = "Claro",

            ["Quote1"] = "Pequeños pasos cada día llevan a grandes resultados.",
            ["Quote2"] = "La disciplina es el puente entre las metas y los logros.",
            ["Quote3"] = "No esperes el momento perfecto. Toma el momento y hazlo perfecto.",
            ["Quote4"] = "El éxito es la suma de pequeños esfuerzos repetidos día tras día.",
            ["Quote5"] = "Planificar es traer el futuro al presente.",
            ["Quote6"] = "Cada día es una nueva oportunidad para ser mejor.",
            ["Quote7"] = "Haz hoy lo que otros no quieren. Mañana vivirás como otros no pueden.",
            ["Quote8"] = "El enfoque no es decir 'sí', es decir 'no'.",
            ["Quote9"] = "Los hábitos definen el futuro. Elígelos sabiamente.",
            ["Quote10"] = "Progreso, no perfección.",
            ["Quote11"] = "El mejor momento para empezar es ahora mismo.",
            ["Quote12"] = "Tu único competidor es quien fuiste ayer.",
            ["Quote13"] = "La energía sigue a la atención. Enfócate en lo importante.",
            ["Quote14"] = "Sin un plan estás planeando fracasar.",
            ["Quote15"] = "Las grandes cosas empiezan en pequeño.",
            ["Quote16"] = "Concéntrate en el proceso y los resultados vendrán.",
            ["Quote17"] = "Cada tarea completada es una pequeña victoria.",
            ["Quote18"] = "No dejes para mañana lo que te hará mejor hoy.",
            ["Quote19"] = "La productividad no es estar ocupado, es obtener resultados.",
            ["Quote20"] = "Una hora de planificación ahorra diez horas de trabajo.",

            // Meetings
            ["Meetings"] = "Reuniones",
            ["MeetingsDesc"] = "Programador de reuniones con recordatorios",
            ["NewMeeting"] = "Nueva reunión",
            ["MeetingTitle"] = "Título",
            ["MeetingDesc"] = "Descripción",
            ["MeetingAttendees"] = "Participantes",
            ["MeetingDate"] = "Fecha",
            ["MeetingTime"] = "Hora",
            ["MeetingDuration"] = "Duración",
            ["MeetingMin"] = "min",
            ["MeetingNotifyDay"] = "1 día antes",
            ["MeetingNotify2h"] = "2 horas antes",
            ["MeetingNotify30m"] = "30 min antes",
            ["MeetingTomorrow"] = "Reunión mañana",
            ["MeetingSoon"] = "Reunión pronto",
            ["MeetingIn2Hours"] = "en 2 horas",
            ["MeetingIn30Min"] = "en 30 minutos",
            ["MeetingNoMeetings"] = "No hay reuniones programadas",
            ["MeetingUpcoming"] = "Próximas",
            ["MeetingPast"] = "Pasadas",
        },

        ["fr"] = new()
        {
            ["Today"] = "AUJOURD'HUI",
            ["TodayBtn"] = "Aujourd'hui",
            ["Statistics"] = "Statistiques",
            ["Export"] = "Exporter",
            ["Settings"] = "Paramètres",
            ["Pomodoro"] = "Pomodoro",

            ["TrayOpen"] = "Ouvrir",
            ["TrayToday"] = "Aujourd'hui",
            ["TrayExit"] = "Quitter",
            ["TrayMinimized"] = "Application réduite dans la barre des tâches",

            ["SearchTooltip"] = "Rechercher (Ctrl+F)",
            ["CopyWeekTooltip"] = "Copier la structure de la semaine dernière",

            ["NoTasksToday"] = "Pas de tâches aujourd'hui",
            ["NoTasks"] = "Pas de tâches",
            ["Completed"] = "terminées",

            ["TotalTasks"] = "Total des tâches",
            ["CompletedLabel"] = "Terminées",
            ["NotCompleted"] = "Non terminées",
            ["GoalsReached"] = "Objectifs atteints",
            ["TotalTasksTip"] = "Nombre de tâches avec texte pour la semaine",
            ["CompletedTip"] = "Tâches marquées comme terminées",
            ["NotCompletedTip"] = "Tâches pas encore terminées",
            ["GoalsTip"] = "Objectifs atteints sur 4",

            ["WeekGoals"] = "OBJECTIFS DE LA SEMAINE",
            ["Notes"] = "NOTES",
            ["Progress"] = "PROGRÈS",
            ["ProgressLabel"] = "progrès",
            ["BestDay"] = "meilleur jour",
            ["GoalsLabel"] = "objectifs",
            ["WeekTasks"] = "TÂCHES DE LA SEMAINE",
            ["TodayBadge"] = "AUJOURD'HUI",
            ["Remaining"] = "restantes",
            ["CarryOverTip"] = "Reporter les tâches non terminées à demain",
            ["AddSubTaskTip"] = "Ajouter une sous-tâche",
            ["DeleteTaskTip"] = "Supprimer la tâche",
            ["MoveNextDayTip"] = "Déplacer au jour suivant",
            ["HabitTracker"] = "SUIVI DES HABITUDES",
            ["StateTracker"] = "SUIVI DE L'ÉTAT",
            ["AddNoteTip"] = "Ajouter une note",
            ["AddGoalTip"] = "Ajouter un objectif",
            ["DeleteTip"] = "Supprimer",
            ["AddHabitTip"] = "Ajouter une habitude",
            ["HabitEmpty"] = "Appuyez sur + pour ajouter une habitude",
            ["HabitLabel"] = "Habitude",
            ["ProgressCol"] = "Progrès",

            ["Monday"] = "Lundi", ["Tuesday"] = "Mardi", ["Wednesday"] = "Mercredi",
            ["Thursday"] = "Jeudi", ["Friday"] = "Vendredi", ["Saturday"] = "Samedi", ["Sunday"] = "Dimanche",
            ["Mon"] = "Lu", ["Tue"] = "Ma", ["Wed"] = "Me", ["Thu"] = "Je",
            ["Fri"] = "Ve", ["Sat"] = "Sa", ["Sun"] = "Di",

            ["Sleep"] = "Sommeil", ["Energy"] = "Énergie", ["Mood"] = "Humeur",

            ["Month1"] = "Janvier", ["Month2"] = "Février", ["Month3"] = "Mars",
            ["Month4"] = "Avril", ["Month5"] = "Mai", ["Month6"] = "Juin",
            ["Month7"] = "Juillet", ["Month8"] = "Août", ["Month9"] = "Septembre",
            ["Month10"] = "Octobre", ["Month11"] = "Novembre", ["Month12"] = "Décembre",

            ["PomWork"] = "Travail", ["PomBreak"] = "Pause", ["PomFocus"] = "Focus",
            ["PomPomodoro"] = "Pomodoro", ["PomFocusMode"] = "Mode focus",
            ["PomReset"] = "Réinitialiser", ["PomSkip"] = "Passer",
            ["PomToggleFocus"] = "Basculer Pomodoro/Focus",
            ["PomSessions"] = "Sessions : ",
            ["PomPause"] = "\u23F8 Pause", ["PomStart"] = "\u25B6 Démarrer",
            ["PomBreakTime"] = "Temps de pause ! Repose-toi \U0001f3d6\ufe0f",
            ["PomWorkTime"] = "Au travail ! \U0001f4aa",
            ["PomFocusTimer"] = "Minuteur focus",
            ["PomFocusAlert"] = "Déjà {0} min ! Fais une pause \u2615",

            ["PriorityHigh"] = "Priorité haute (clic pour changer)",
            ["PriorityMedium"] = "Priorité moyenne (clic pour changer)",
            ["PriorityLow"] = "Priorité basse (clic pour changer)",
            ["PriorityNone"] = "Sans priorité",

            ["CatWork"] = "Travail (clic pour changer)", ["CatStudy"] = "Études (clic pour changer)",
            ["CatPersonal"] = "Personnel (clic pour changer)", ["CatHealth"] = "Santé (clic pour changer)",
            ["CatOther"] = "Autre (clic pour changer)",

            ["SettingsTitle"] = "Paramètres",
            ["ExportExcel"] = "Exporter vers Excel",
            ["ExportExcelDesc"] = "Exporter la semaine en cours en fichier .xlsx",
            ["Palette"] = "Palette",
            ["PaletteCurrent"] = "Actuelle : ",
            ["AutoStart"] = "Démarrage automatique",
            ["AutoStartDesc"] = "Lancer l'application à l'ouverture de Windows",
            ["Updates"] = "Mises à jour",
            ["CheckUpdates"] = "Vérifier",
            ["UpdateBtn"] = "Mettre à jour",
            ["Backup"] = "Sauvegarde",
            ["BackupDesc"] = "Créer une copie ou restaurer la base de données",
            ["CreateBackup"] = "Créer une copie",
            ["Restore"] = "Restaurer",
            ["Reminders"] = "Rappels",
            ["RemindersDesc"] = "Notifications à l'heure indiquée",
            ["Add"] = "Ajouter",
            ["TimeTip"] = "Heure (HH:MM)",
            ["TitleTip"] = "Titre",
            ["MessageTip"] = "Message",
            ["RecurringTasks"] = "Tâches récurrentes",
            ["RecurringDesc"] = "Les modèles sont ajoutés automatiquement chaque semaine",
            ["AboutApp"] = "À propos",
            ["Version"] = "Version",
            ["Platform"] = "Plateforme",
            ["Database"] = "Base de données",
            ["DbLocal"] = "SQLite (locale)",
            ["Storage"] = "Stockage",
            ["Shortcuts"] = "Raccourcis clavier",
            ["SwitchMonth"] = "Changer de mois",
            ["GoToToday"] = "Aller à aujourd'hui",
            ["SearchTasks"] = "Rechercher des tâches",
            ["PomodoroTimer"] = "Minuteur Pomodoro",
            ["ExportExcelShort"] = "Exporter vers Excel",
            ["OpenSettings"] = "Ouvrir les paramètres",
            ["Description"] = "Description",
            ["AppDescription"] = "Daily Planner — un planificateur hebdomadaire avec suivi des habitudes, analyse de productivité et suivi de l'état (sommeil, énergie, humeur). Toutes les données sont stockées localement dans une base SQLite. L'application prend en charge la navigation par mois et semaines, les statistiques automatiques, l'export Excel, la recherche de tâches et la visualisation des progrès.",
            ["Language"] = "Langue",
            ["LanguageDesc"] = "Langue de l'interface de l'application",

            ["Productivity"] = "Productivité",
            ["BestDayLabel"] = "Meilleur jour",
            ["Goals"] = "Objectifs",
            ["AvgSleep"] = "Moy. sommeil",
            ["AvgEnergy"] = "Moy. énergie",
            ["AvgMood"] = "Moy. humeur",
            ["WeekComparison"] = "Comparaison des semaines",
            ["PrevWeek"] = "Semaine précédente",
            ["Trend"] = "Tendance",
            ["CurrWeek"] = "Semaine en cours",
            ["WeeklyProductivity"] = "Productivité hebdomadaire",
            ["HabitHeatmap"] = "Habitudes — Carte thermique (30 jours)",
            ["HabitStreaks"] = "Séries d'habitudes",
            ["TasksUnit"] = "tâches",
            ["NoChange"] = "Pas de changement",
            ["DaysUnit"] = "j.",

            ["SummaryTasks"] = "Tâches : {0}/{1}",
            ["SummaryGoals"] = "Objectifs : {0}/{1}",
            ["SummaryBestDay"] = "Meilleur jour : {0}",
            ["SummaryExcellent"] = "\u2b50 Excellente semaine !",
            ["SummaryGood"] = "\u2705 Bonne semaine !",
            ["SummaryCanBetter"] = "\U0001f4aa On peut faire mieux !",

            ["ExportTitle"] = "Exporter",
            ["ExportSuccess"] = "Fichier enregistré avec succès",
            ["ExportError"] = "Erreur lors de l'export",
            ["RestoreTitle"] = "Restauration",
            ["RestoreSuccess"] = "Base de données restaurée avec succès",
            ["RestoreError"] = "Erreur lors de la restauration",
            ["RestoreInvalidDb"] = "Le fichier n'est pas une base de données SQLite valide",
            ["CopyTitle"] = "Copier",
            ["CopySuccess"] = "Structure de la semaine dernière copiée",
            ["Reminder"] = "Rappel",
            ["MoveTitle"] = "Déplacer",
            ["MoveNoSlots"] = "Pas de créneaux libres pour ce jour",

            ["UpdateUnavailable"] = "Mises à jour non disponibles (mode dev)",
            ["UpdateChecking"] = "Vérification des mises à jour...",
            ["UpdateLatest"] = "Vous utilisez la dernière version",
            ["UpdateAvailable"] = "Mise à jour disponible : v{0}",
            ["UpdateDownloading"] = "Téléchargement de la mise à jour...",
            ["UpdateProgress"] = "Téléchargement : {0}%",

            ["ExpWeek"] = "Semaine {0}",
            ["ExpPlannerWeek"] = "Planificateur — Semaine {0} – {1}",
            ["ExpWeekGoals"] = "Objectifs de la semaine",
            ["ExpDay"] = "Jour",
            ["ExpTask"] = "Tâche {0}",
            ["ExpState"] = "État",
            ["ExpHabits"] = "Habitudes",
            ["ExpNotes"] = "Notes",

            ["Loading"] = "Chargement des données...",
            ["DbError"] = "Erreur d'initialisation de la base de données :\n{0}",
            ["PlannerFileName"] = "Planificateur_{0}",
            ["ExcelFilter"] = "Fichiers Excel (*.xlsx)|*.xlsx",
            ["Goal"] = "Objectif",
            ["LightTheme"] = "Clair",

            ["Quote1"] = "De petits pas chaque jour mènent à de grands résultats.",
            ["Quote2"] = "La discipline est le pont entre les objectifs et les réalisations.",
            ["Quote3"] = "N'attends pas le moment parfait. Prends le moment et rends-le parfait.",
            ["Quote4"] = "Le succès est la somme de petits efforts répétés jour après jour.",
            ["Quote5"] = "Planifier, c'est amener le futur dans le présent.",
            ["Quote6"] = "Chaque jour est une nouvelle opportunité de s'améliorer.",
            ["Quote7"] = "Fais aujourd'hui ce que les autres ne veulent pas. Demain tu vivras comme ils ne peuvent pas.",
            ["Quote8"] = "Le focus, ce n'est pas dire 'oui', c'est dire 'non'.",
            ["Quote9"] = "Les habitudes définissent l'avenir. Choisis-les avec sagesse.",
            ["Quote10"] = "Le progrès, pas la perfection.",
            ["Quote11"] = "Le meilleur moment pour commencer, c'est maintenant.",
            ["Quote12"] = "Ton seul concurrent est celui que tu étais hier.",
            ["Quote13"] = "L'énergie suit l'attention. Concentre-toi sur l'essentiel.",
            ["Quote14"] = "Sans plan, tu planifies d'échouer.",
            ["Quote15"] = "Les grandes choses commencent petit.",
            ["Quote16"] = "Concentre-toi sur le processus et les résultats viendront.",
            ["Quote17"] = "Chaque tâche accomplie est une petite victoire.",
            ["Quote18"] = "Ne remets pas à demain ce qui te rendra meilleur aujourd'hui.",
            ["Quote19"] = "La productivité n'est pas l'occupation, c'est le résultat.",
            ["Quote20"] = "Une heure de planification économise dix heures de travail.",

            // Meetings
            ["Meetings"] = "Réunions",
            ["MeetingsDesc"] = "Planificateur de réunions avec rappels",
            ["NewMeeting"] = "Nouvelle réunion",
            ["MeetingTitle"] = "Titre",
            ["MeetingDesc"] = "Description",
            ["MeetingAttendees"] = "Participants",
            ["MeetingDate"] = "Date",
            ["MeetingTime"] = "Heure",
            ["MeetingDuration"] = "Durée",
            ["MeetingMin"] = "min",
            ["MeetingNotifyDay"] = "1 jour avant",
            ["MeetingNotify2h"] = "2 heures avant",
            ["MeetingNotify30m"] = "30 min avant",
            ["MeetingTomorrow"] = "Réunion demain",
            ["MeetingSoon"] = "Réunion bientôt",
            ["MeetingIn2Hours"] = "dans 2 heures",
            ["MeetingIn30Min"] = "dans 30 minutes",
            ["MeetingNoMeetings"] = "Aucune réunion prévue",
            ["MeetingUpcoming"] = "À venir",
            ["MeetingPast"] = "Passées",
        },
    };

    // Helper: get month name for current language
    public static string GetMonthName(int month) => Get($"Month{month}");

    // Helper: get day name for current language
    public static string GetDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Get("Monday"),
        DayOfWeek.Tuesday => Get("Tuesday"),
        DayOfWeek.Wednesday => Get("Wednesday"),
        DayOfWeek.Thursday => Get("Thursday"),
        DayOfWeek.Friday => Get("Friday"),
        DayOfWeek.Saturday => Get("Saturday"),
        DayOfWeek.Sunday => Get("Sunday"),
        _ => ""
    };

    public static string GetShortDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Get("Mon"),
        DayOfWeek.Tuesday => Get("Tue"),
        DayOfWeek.Wednesday => Get("Wed"),
        DayOfWeek.Thursday => Get("Thu"),
        DayOfWeek.Friday => Get("Fri"),
        DayOfWeek.Saturday => Get("Sat"),
        DayOfWeek.Sunday => Get("Sun"),
        _ => ""
    };
}
