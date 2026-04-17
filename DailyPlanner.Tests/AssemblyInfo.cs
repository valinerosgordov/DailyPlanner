// Disable parallel test execution \u2014 tests share a static PlannerDbContextFactory override
// that would race across threads. Serial execution is fast enough (<3s for the whole suite).
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
