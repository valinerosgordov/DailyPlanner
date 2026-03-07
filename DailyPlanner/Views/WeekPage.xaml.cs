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
