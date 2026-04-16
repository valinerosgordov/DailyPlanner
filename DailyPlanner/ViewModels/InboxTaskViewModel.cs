using CommunityToolkit.Mvvm.ComponentModel;
using DailyPlanner.Models;

namespace DailyPlanner.ViewModels;

public sealed partial class InboxTaskViewModel : ObservableObject
{
    public InboxTask Model { get; }

    public InboxTaskViewModel(InboxTask model)
    {
        Model = model;
        _text = model.Text;
        _dueDate = model.DueDate;
    }

    public int Id => Model.Id;
    public InboxSource Source => Model.Source;
    public string? BoardName => Model.BoardName;
    public string? ListName => Model.ListName;
    public string? Url => Model.Url;

    public string SourceLabel => Source == InboxSource.Trello
        ? $"Trello · {BoardName}"
        : "Manual";

    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private DateOnly? _dueDate;

    public string? DueDateDisplay => DueDate?.ToString("dd.MM");
    public bool HasDueDate => DueDate.HasValue;

    partial void OnDueDateChanged(DateOnly? value) => OnPropertyChanged(nameof(DueDateDisplay));
}
