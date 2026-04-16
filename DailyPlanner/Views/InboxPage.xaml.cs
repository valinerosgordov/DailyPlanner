using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using DailyPlanner.ViewModels;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseButtonState = System.Windows.Input.MouseButtonState;
using DragEventArgs = System.Windows.DragEventArgs;
using DragDropEffects = System.Windows.DragDropEffects;
using DataObject = System.Windows.DataObject;
using DragDrop = System.Windows.DragDrop;

namespace DailyPlanner.Views;

public partial class InboxPage : Page
{
    private readonly InboxViewModel _viewModel;
    private Point _dragStart;
    private InboxTaskViewModel? _draggedTask;
    private const string DragFormat = "DailyPlanner.InboxTask";

    public InboxPage(InboxViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) =>
        {
            try { await viewModel.LoadAsync(); }
            catch (Exception ex) { Log.Error("InboxPage", $"Load failed: {ex.Message}"); }
        };
    }

    private async void InboxText_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is InboxTaskViewModel vm)
        {
            try { await _viewModel.SaveTaskCommand.ExecuteAsync(vm); }
            catch (Exception ex) { Log.Error("InboxPage", $"Save failed: {ex.Message}"); }
        }
    }

    // ─── Drag source ───────────────────────────────────────────────

    private void InboxItem_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        if (sender is FrameworkElement fe && fe.Tag is InboxTaskViewModel vm)
            _draggedTask = vm;
    }

    private void InboxItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            _draggedTask = null;
            return;
        }
        if (_draggedTask is null) return;

        var current = e.GetPosition(null);
        var diff = _dragStart - current;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        if (sender is not FrameworkElement fe) return;

        var data = new DataObject(DragFormat, _draggedTask);
        try
        {
            DragDrop.DoDragDrop(fe, data, DragDropEffects.Move);
        }
        finally
        {
            _draggedTask = null;
        }
    }

    // ─── Drop target ───────────────────────────────────────────────

    private void DayColumn_DragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DragFormat))
        {
            e.Effects = DragDropEffects.None;
            return;
        }
        if (sender is FrameworkElement fe && fe.Tag is InboxDayViewModel day)
            day.IsDropTarget = true;
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void DayColumn_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is InboxDayViewModel day)
            day.IsDropTarget = false;
    }

    private void DayColumn_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DragFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private async void DayColumn_Drop(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not InboxDayViewModel day)
            return;
        day.IsDropTarget = false;

        if (e.Data.GetData(DragFormat) is not InboxTaskViewModel task) return;

        try
        {
            await _viewModel.MoveToDayAsync(task, day.Date);
        }
        catch (Exception ex)
        {
            Log.Error("InboxPage", $"Drop failed: {ex.Message}");
        }
    }

    private async void MoveToDay_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not InboxTaskViewModel vm) return;
        var date = vm.DueDate ?? DateOnly.FromDateTime(DateTime.Today);
        try
        {
            await _viewModel.MoveToDayAsync(vm, date);
        }
        catch (Exception ex)
        {
            Log.Error("InboxPage", $"MoveToDay failed: {ex.Message}");
        }
    }
}
