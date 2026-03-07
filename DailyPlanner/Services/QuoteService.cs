namespace DailyPlanner.Services;

public static class QuoteService
{
    private const int QuoteCount = 20;

    public static string GetDailyQuote()
    {
        var dayIndex = DateTime.Today.DayOfYear + DateTime.Today.Year * 366;
        var idx = dayIndex % QuoteCount;
        return Loc.Get($"Quote{idx + 1}");
    }

    public static string GetRandom()
    {
        var idx = Random.Shared.Next(QuoteCount);
        return Loc.Get($"Quote{idx + 1}");
    }
}
