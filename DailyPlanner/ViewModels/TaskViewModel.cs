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
    }

    [ObservableProperty]
    private string _text;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private TaskPriority _priority;

    [ObservableProperty]
    private TaskCategory _category;

    public int Order => _model.Order;
    public DailyTask Model => _model;

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
}
