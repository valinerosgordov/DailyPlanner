# Contributing to Daily Planner

Thanks for your interest in contributing! Here's how you can help.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/<your-username>/DailyPlanner.git`
3. Install [.NET 10 SDK Preview](https://dotnet.microsoft.com/download/dotnet/10.0)
4. Build: `dotnet build --verbosity minimal`
5. Run: `dotnet run --project DailyPlanner`

## How to Contribute

### Reporting Bugs

- Open an [Issue](https://github.com/valinerosgordov/DailyPlanner/issues/new?template=bug_report.md)
- Include steps to reproduce, expected vs actual behavior
- Attach screenshots if applicable
- Mention your Windows version and app version

### Suggesting Features

- Open an [Issue](https://github.com/valinerosgordov/DailyPlanner/issues/new?template=feature_request.md)
- Describe the feature and why it would be useful

### Pull Requests

1. Create a branch from `master`: `git checkout -b feature/my-feature`
2. Make your changes
3. Test thoroughly — the app should build with zero warnings
4. Commit with a clear message: `git commit -m "Add: brief description"`
5. Push and open a Pull Request

## Code Style

- C# 14, .NET 10, file-scoped namespaces
- Follow existing patterns (MVVM with CommunityToolkit.Mvvm)
- Keep code self-documenting — minimal comments
- Run `dotnet format` before submitting

## Project Structure

```
DailyPlanner/
├── Models/          # EF Core entities
├── Data/            # DbContext, migrations
├── ViewModels/      # MVVM ViewModels
├── Views/           # XAML pages
├── Services/        # Business logic
├── Converters/      # WPF value converters
└── Themes/          # Resource dictionaries
```

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
