using ClosedXML.Excel;
using DailyPlanner.Models;

namespace DailyPlanner.Services;

public static class ExportService
{
    public static bool ExportWeekToExcel(PlannerWeek week, string filePath)
    {
        try
        {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet(string.Format(Loc.Get("ExpWeek"), week.StartDate.ToString("dd.MM")));

        // Header
        ws.Cell("A1").Value = string.Format(Loc.Get("ExpPlannerWeek"),
            week.StartDate.ToString("dd.MM.yyyy"), week.StartDate.AddDays(6).ToString("dd.MM.yyyy"));
        ws.Range("A1:H1").Merge().Style
            .Font.SetBold(true).Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // Goals
        ws.Cell("A3").Value = Loc.Get("ExpWeekGoals");
        ws.Cell("A3").Style.Font.SetBold(true);
        var row = 4;
        foreach (var goal in week.Goals.OrderBy(g => g.Order))
        {
            ws.Cell(row, 1).Value = goal.Order;
            ws.Cell(row, 2).Value = goal.Text;
            ws.Cell(row, 3).Value = goal.IsCompleted ? "\u2713" : "";
            row++;
        }

        // Days header
        row += 1;
        var daysStartRow = row;
        ws.Cell(row, 1).Value = Loc.Get("ExpDay");
        string[] dayKeys = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
        for (var i = 0; i < 7; i++)
            ws.Cell(row, i + 2).Value = Loc.Get(dayKeys[i]);
        ws.Range(row, 1, row, 8).Style.Font.SetBold(true);

        // Tasks (including subtasks)
        row++;
        var days = week.Days.OrderBy(d => d.Date).ToList();
        var expandedTasks = days.Select(d =>
            d.Tasks.Where(t => t.ParentTaskId is null).OrderBy(t => t.Order)
                .SelectMany(t => t.SubTasks.OrderBy(s => s.Order).Prepend(t)).ToList()).ToList();
        var maxTasks = expandedTasks.Count > 0 ? expandedTasks.Max(t => t.Count) : 0;
        for (var t = 0; t < maxTasks; t++)
        {
            ws.Cell(row, 1).Value = string.Format(Loc.Get("ExpTask"), t + 1);
            for (var d = 0; d < days.Count; d++)
            {
                if (t < expandedTasks[d].Count && !string.IsNullOrWhiteSpace(expandedTasks[d][t].Text))
                {
                    var task = expandedTasks[d][t];
                    var prefix = task.ParentTaskId is not null ? "  └ " : "";
                    var text = $"{prefix}{task.Text}";
                    if (task.IsCompleted) text = $"\u2713 {text}";
                    ws.Cell(row, d + 2).Value = text;
                }
            }
            row++;
        }

        // State
        row += 1;
        ws.Cell(row, 1).Value = Loc.Get("ExpState");
        ws.Cell(row, 1).Style.Font.SetBold(true);
        row++;
        ws.Cell(row, 1).Value = Loc.Get("Sleep");
        ws.Cell(row + 1, 1).Value = Loc.Get("Energy");
        ws.Cell(row + 2, 1).Value = Loc.Get("Mood");

        for (var d = 0; d < days.Count; d++)
        {
            var state = days[d].State;
            if (state is null) continue;
            ws.Cell(row, d + 2).Value = state.Sleep;
            ws.Cell(row + 1, d + 2).Value = state.Energy;
            ws.Cell(row + 2, d + 2).Value = state.Mood;
        }

        row += 4;

        // Habits
        ws.Cell(row, 1).Value = Loc.Get("ExpHabits");
        ws.Cell(row, 1).Style.Font.SetBold(true);
        row++;
        ws.Cell(row, 1).Value = Loc.Get("HabitLabel");
        for (var i = 0; i < 7; i++)
            ws.Cell(row, i + 2).Value = Loc.Get(dayKeys[i]);
        ws.Cell(row, 9).Value = Loc.Get("ProgressCol");
        ws.Range(row, 1, row, 9).Style.Font.SetBold(true);
        row++;

        foreach (var habit in week.Habits.OrderBy(h => h.Order))
        {
            ws.Cell(row, 1).Value = habit.Name;
            var ordered = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            for (var i = 0; i < ordered.Length; i++)
            {
                var entry = habit.Entries.FirstOrDefault(e => e.DayOfWeek == ordered[i]);
                ws.Cell(row, i + 2).Value = entry?.IsCompleted == true ? "\u2713" : "";
            }
            ws.Cell(row, 9).Value = $"{habit.Entries.Count(e => e.IsCompleted)}/7";
            row++;
        }

        // Notes
        row += 1;
        ws.Cell(row, 1).Value = Loc.Get("ExpNotes");
        ws.Cell(row, 1).Style.Font.SetBold(true);
        row++;
        if (week.WeeklyNotes.Count > 0)
        {
            foreach (var note in week.WeeklyNotes.OrderBy(n => n.Order))
            {
                ws.Cell(row, 1).Value = note.Text;
                ws.Range(row, 1, row, 8).Merge();
                row++;
            }
        }
        if (!string.IsNullOrWhiteSpace(week.Notes))
        {
            ws.Cell(row, 1).Value = week.Notes;
            ws.Range(row, 1, row, 8).Merge();
        }

        // Auto-fit columns
        ws.Columns().AdjustToContents();

        workbook.SaveAs(filePath);
        return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ExportService] Export failed: {ex.Message}");
            return false;
        }
    }
}
