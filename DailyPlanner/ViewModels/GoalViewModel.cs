using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

public sealed partial class GoalViewModel : ObservableObject
{
    private readonly WeeklyGoal _model;
    private readonly PlannerService _service;

    public GoalViewModel(WeeklyGoal model, PlannerService service)
    {
        _model = model;
        _service = service;
        _text = model.Text;
        _isCompleted = model.IsCompleted;
    }

    public WeeklyGoal Model => _model;
    public int Order => _model.Order;

    [ObservableProperty]
    private string _text;

    [ObservableProperty]
    private bool _isCompleted;

    partial void OnTextChanged(string value)
    {
        _model.Text = value;
        DebounceService.Debounce($"goal-{_model.Id}",
            () => _service.SaveGoalAsync(_model));
    }

    partial void OnIsCompletedChanged(bool value)
    {
        _model.IsCompleted = value;
        _service.SaveGoalAsync(_model).FireAndForget("goal-save");
    }
}
