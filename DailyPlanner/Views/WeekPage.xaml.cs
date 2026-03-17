using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DailyPlanner.ViewModels;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;

namespace DailyPlanner.Views;

public partial class WeekPage : Page
{
    private TaskViewModel? _draggedTask;
    private System.Windows.Point _dragStart;

    public WeekPage()
    {
        InitializeComponent();
    }

    private void PriorityCycle_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TaskViewModel task })
            task.CyclePriorityCommand.Execute(null);
    }

    private void CategoryCycle_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TaskViewModel task })
            task.CycleCategoryCommand.Execute(null);
    }

    private async void CarryOver_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: DayViewModel day }) return;

        var mainVm = DataContext as MainViewModel;
        if (mainVm?.SelectedWeek is null) return;

        var nextDate = day.Date.AddDays(1);
        await mainVm.Service.CarryOverTasksAsync(day.Date, nextDate);
        await mainVm.LoadMonthCommand.ExecuteAsync(null);
    }

    private void ToggleExpand_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TaskViewModel task })
        {
            task.ToggleExpandCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async void AddSubTask_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TaskViewModel task } && !task.IsSubTask
            && !string.IsNullOrWhiteSpace(task.Text))
        {
            await task.AddSubTaskCommand.ExecuteAsync(null);
            e.Handled = true;
        }
    }

    // Overload for ContextMenu MenuItem (RoutedEventArgs)
    private async void AddSubTask_Click(object sender, RoutedEventArgs e)
    {
        var task = GetCtxTask(sender);
        if (task is not null && !task.IsSubTask && !string.IsNullOrWhiteSpace(task.Text))
            await task.AddSubTaskCommand.ExecuteAsync(null);
    }

    private async void RemoveSubTask_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: TaskViewModel subTask }) return;

        // Find parent task
        var parent = FindParentTask(subTask);
        if (parent is not null)
        {
            await parent.RemoveSubTaskCommand.ExecuteAsync(subTask);
            e.Handled = true;
        }
    }

    private TaskViewModel? FindParentTask(TaskViewModel subTask)
    {
        var mainVm = DataContext as MainViewModel;
        if (mainVm?.SelectedWeek is null) return null;

        return mainVm.SelectedWeek.Days
            .SelectMany(d => d.Tasks)
            .FirstOrDefault(t => t.SubTasks.Contains(subTask));
    }

    private async void DeleteTask_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: TaskViewModel task }) return;
        if (string.IsNullOrWhiteSpace(task.Text)) return;

        var mainVm = DataContext as MainViewModel;
        if (mainVm?.SelectedWeek is null) return;

        var day = mainVm.SelectedWeek.Days.FirstOrDefault(d => d.Tasks.Contains(task));
        if (day is null) return;

        // Save snapshot for undo
        var snapshot = new Models.DailyTask
        {
            DailyPlanId = task.Model.DailyPlanId,
            Order = task.Model.Order,
            Text = task.Model.Text,
            IsCompleted = task.Model.IsCompleted,
            Priority = task.Model.Priority,
            Category = task.Model.Category,
            Deadline = task.Model.Deadline
        };
        var dayRef = day;

        await mainVm.Service.RemoveTaskAsync(task.Model.Id);
        day.Tasks.Remove(task);

        Services.UndoService.Push(
            string.Format(Services.Loc.Get("TaskDeleted"), snapshot.Text),
            () => RestoreTask(mainVm, dayRef, snapshot));

        e.Handled = true;
    }

    // Overload for ContextMenu MenuItem
    private async void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        var task = GetCtxTask(sender);
        if (task is not null) await DeleteTaskCore(task);
    }

    private async Task DeleteTaskCore(TaskViewModel task)
    {
        if (string.IsNullOrWhiteSpace(task.Text)) return;
        var mainVm = DataContext as MainViewModel;
        if (mainVm?.SelectedWeek is null) return;
        var day = mainVm.SelectedWeek.Days.FirstOrDefault(d => d.Tasks.Contains(task));
        if (day is null) return;

        var snapshot = new Models.DailyTask
        {
            DailyPlanId = task.Model.DailyPlanId,
            Order = task.Model.Order,
            Text = task.Model.Text,
            IsCompleted = task.Model.IsCompleted,
            Priority = task.Model.Priority,
            Category = task.Model.Category,
            Deadline = task.Model.Deadline
        };
        var dayRef = day;
        await mainVm.Service.RemoveTaskAsync(task.Model.Id);
        day.Tasks.Remove(task);
        Services.UndoService.Push(
            string.Format(Services.Loc.Get("TaskDeleted"), snapshot.Text),
            () => RestoreTask(mainVm, dayRef, snapshot));
    }

    private async void RestoreTask(MainViewModel mainVm, DayViewModel day, Models.DailyTask snapshot)
    {
        await mainVm.Service.AddSubTaskAsync(snapshot);
        await mainVm.LoadMonthCommand.ExecuteAsync(null);
    }

    private void DuplicateTask_Click(object sender, RoutedEventArgs e)
    {
        var task = GetCtxTask(sender);
        if (task is null || string.IsNullOrWhiteSpace(task.Text)) return;

        var mainVm = DataContext as MainViewModel;
        if (mainVm?.SelectedWeek is null) return;

        var day = mainVm.SelectedWeek.Days.FirstOrDefault(d => d.Tasks.Contains(task));
        if (day is null) return;

        var emptySlot = day.Tasks.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Text));
        if (emptySlot is null) return;

        emptySlot.Text = task.Text;
        emptySlot.Priority = task.Priority;
        emptySlot.Category = task.Category;
        emptySlot.Deadline = task.Deadline;
    }

    private static TaskViewModel? GetTaskFromSender(object sender)
    {
        // Direct FrameworkElement with DataContext
        if (sender is FrameworkElement { DataContext: TaskViewModel t })
            return t;
        // MenuItem inside ContextMenu — walk up to placement target
        if (sender is System.Windows.Controls.MenuItem mi
            && mi.Parent is System.Windows.Controls.ContextMenu ctx
            && ctx.PlacementTarget is FrameworkElement { DataContext: TaskViewModel t2 })
            return t2;
        return null;
    }

    // Context menu: Priority
    private void CtxPriorityHigh_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetPriorityCommand.Execute("High");
    private void CtxPriorityMedium_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetPriorityCommand.Execute("Medium");
    private void CtxPriorityLow_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetPriorityCommand.Execute("Low");
    private void CtxPriorityNone_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetPriorityCommand.Execute("None");

    // Context menu: Category
    private void CtxCatWork_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetCategoryCommand.Execute("Work");
    private void CtxCatStudy_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetCategoryCommand.Execute("Study");
    private void CtxCatPersonal_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetCategoryCommand.Execute("Personal");
    private void CtxCatHealth_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetCategoryCommand.Execute("Health");
    private void CtxCatNone_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetCategoryCommand.Execute("None");

    // Context menu: Deadline
    private void CtxDeadlineToday_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetDeadlineTodayCommand.Execute(null);
    private void CtxDeadlineTomorrow_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetDeadlineTomorrowCommand.Execute(null);
    private void CtxDeadlineNextWeek_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.SetDeadlineNextWeekCommand.Execute(null);
    private void CtxDeadlineClear_Click(object sender, RoutedEventArgs e) => GetCtxTask(sender)?.ClearDeadlineCommand.Execute(null);

    private static TaskViewModel? GetCtxTask(object sender)
    {
        if (sender is not System.Windows.Controls.MenuItem mi) return null;

        // Walk up MenuItem parents to find the root ContextMenu
        DependencyObject current = mi;
        while (current is not null)
        {
            if (current is System.Windows.Controls.ContextMenu ctx
                && ctx.PlacementTarget is FrameworkElement { DataContext: TaskViewModel task })
                return task;
            current = LogicalTreeHelper.GetParent(current)
                      ?? System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private async void MoveTaskNextDay_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: TaskViewModel task }) return;
        await MoveTaskNextDayCore(task);
        e.Handled = true;
    }

    // Overload for ContextMenu MenuItem
    private async void MoveTaskNextDay_Click(object sender, RoutedEventArgs e)
    {
        var task = GetCtxTask(sender);
        if (task is not null) await MoveTaskNextDayCore(task);
    }

    private async Task MoveTaskNextDayCore(TaskViewModel task)
    {
        if (string.IsNullOrWhiteSpace(task.Text)) return;

        var mainVm = DataContext as MainViewModel;
        if (mainVm?.SelectedWeek is null) return;

        var day = mainVm.SelectedWeek.Days.FirstOrDefault(d => d.Tasks.Contains(task));
        if (day is null) return;

        var nextDate = day.Date.AddDays(1);
        await mainVm.Service.MoveTaskToNextDayAsync(task.Model.Id, nextDate);
        day.Tasks.Remove(task);
        await mainVm.LoadMonthCommand.ExecuteAsync(null);
    }

    // Drag & drop
    private void Task_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        if (sender is FrameworkElement { DataContext: TaskViewModel task })
            _draggedTask = task;
    }

    private void Task_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedTask is null) return;

        var pos = e.GetPosition(null);
        var diff = _dragStart - pos;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        if (string.IsNullOrWhiteSpace(_draggedTask.Text)) return;

        var data = new DataObject("TaskVM", _draggedTask);
        DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
        _draggedTask = null;
    }

    private void Day_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("TaskVM") ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Day_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("TaskVM")) return;
        if (e.Data.GetData("TaskVM") is not TaskViewModel sourceTask) return;

        // Find target day from the ItemsControl's Tag
        var target = sender as FrameworkElement;
        var depth = 0;
        while (target is not null && target is not ItemsControl && depth++ < 20)
            target = System.Windows.Media.VisualTreeHelper.GetParent(target) as FrameworkElement;

        if (target is not ItemsControl { Tag: DayViewModel targetDay }) return;

        // Find source day
        var mainVm = DataContext as MainViewModel;
        if (mainVm?.SelectedWeek is null) return;

        var sourceDay = mainVm.SelectedWeek.Days.FirstOrDefault(d => d.Tasks.Contains(sourceTask));
        if (sourceDay is null || sourceDay == targetDay) return;

        // Move: copy text to first empty slot in target, clear source
        var emptySlot = targetDay.Tasks.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Text));
        if (emptySlot is null)
        {
            Services.NotificationService.ShowToast(Services.Loc.Get("MoveTitle"), Services.Loc.Get("MoveNoSlots"));
            return;
        }

        emptySlot.IsCompleted = sourceTask.IsCompleted;
        emptySlot.Text = sourceTask.Text;
        emptySlot.Priority = sourceTask.Priority;
        emptySlot.Category = sourceTask.Category;
        sourceTask.Text = string.Empty;
        sourceTask.Priority = Models.TaskPriority.None;
        sourceTask.Category = Models.TaskCategory.None;
        sourceTask.IsCompleted = false;

        e.Handled = true;
    }
}
