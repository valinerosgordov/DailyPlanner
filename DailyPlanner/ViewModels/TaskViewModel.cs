using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class TaskViewModel : ObservableObject
{
    private readonly DailyTask _model;
    private readonly PlannerService _service;

    public TaskViewModel(DailyTask model, PlannerService service)
    {
        _model = model;
        _service = service;
        _text = model.Text;
        _isCompleted = model.IsCompleted;
        _priority = model.Priority;
        _category = model.Category;
        _isExpanded = model.SubTasks.Count > 0;

        SubTasks = new ObservableCollection<TaskViewModel>(
            model.SubTasks.OrderBy(s => s.Order).Select(s => new TaskViewModel(s, service)));
    }

    [ObservableProperty]
    private string _text;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private TaskPriority _priority;

    [ObservableProperty]
    private TaskCategory _category;

    [ObservableProperty]
    private bool _isExpanded;

    public int Order => _model.Order;
    public DailyTask Model => _model;
    public bool IsSubTask => _model.ParentTaskId is not null;
    public bool HasSubTasks => SubTasks.Count > 0;
    public ObservableCollection<TaskViewModel> SubTasks { get; }

    public int SubTasksCompleted => SubTasks.Count(s => s.IsCompleted);
    public string SubTasksProgress => HasSubTasks ? $"{SubTasksCompleted}/{SubTasks.Count}" : string.Empty;

    partial void OnTextChanged(string value)
    {
        _model.Text = value;
        DebounceService.Debounce($"task-{_model.Id}",
            () => _service.SaveTaskAsync(_model));
    }

    partial void OnIsCompletedChanged(bool value)
    {
        _model.IsCompleted = value;
        _service.SaveTaskAsync(_model).FireAndForget("task-save");
    }

    partial void OnPriorityChanged(TaskPriority value)
    {
        _model.Priority = value;
        _service.SaveTaskAsync(_model).FireAndForget("task-save");
    }

    partial void OnCategoryChanged(TaskCategory value)
    {
        _model.Category = value;
        _service.SaveTaskAsync(_model).FireAndForget("task-save");
    }

    [RelayCommand]
    private void CyclePriority()
    {
        Priority = Priority switch
        {
            TaskPriority.None => TaskPriority.Low,
            TaskPriority.Low => TaskPriority.Medium,
            TaskPriority.Medium => TaskPriority.High,
            TaskPriority.High => TaskPriority.None,
            _ => TaskPriority.None
        };
    }

    [RelayCommand]
    private void CycleCategory()
    {
        Category = Category switch
        {
            TaskCategory.None => TaskCategory.Work,
            TaskCategory.Work => TaskCategory.Study,
            TaskCategory.Study => TaskCategory.Personal,
            TaskCategory.Personal => TaskCategory.Health,
            TaskCategory.Health => TaskCategory.Other,
            TaskCategory.Other => TaskCategory.None,
            _ => TaskCategory.None
        };
    }

    [RelayCommand]
    private async Task AddSubTaskAsync()
    {
        var order = _model.SubTasks.Count + 1;
        var subTask = new DailyTask
        {
            DailyPlanId = _model.DailyPlanId,
            ParentTaskId = _model.Id,
            Order = order
        };

        await _service.AddSubTaskAsync(subTask);
        _model.SubTasks.Add(subTask);

        var vm = new TaskViewModel(subTask, _service);
        vm.PropertyChanged += (_, _) => RefreshSubTasksProgress();
        SubTasks.Add(vm);

        IsExpanded = true;
        OnPropertyChanged(nameof(HasSubTasks));
        RefreshSubTasksProgress();
    }

    [RelayCommand]
    private async Task RemoveSubTaskAsync(TaskViewModel? subVm)
    {
        if (subVm is null) return;

        await _service.RemoveSubTaskAsync(subVm.Model.Id);
        _model.SubTasks.Remove(subVm.Model);
        SubTasks.Remove(subVm);

        OnPropertyChanged(nameof(HasSubTasks));
        RefreshSubTasksProgress();
    }

    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;

    private void RefreshSubTasksProgress()
    {
        OnPropertyChanged(nameof(SubTasksCompleted));
        OnPropertyChanged(nameof(SubTasksProgress));
    }
}
