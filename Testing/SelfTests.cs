using EngineeringSystemsDesk.Models;
using EngineeringSystemsDesk.Repositories;
using EngineeringSystemsDesk.Services;

namespace EngineeringSystemsDesk.Testing;

// A minimal hand-rolled test runner. Normally you'd reach for xUnit/NUnit
// (via NuGet) for this, but this project was written in a sandbox with no
// access to NuGet, so this reimplements just enough of "assert and report"
// to demonstrate the same testing habits: one test per behavior, clear
// pass/fail output, and both unit tests (single class in isolation) and
// integration tests (repository + service + report working together).
public static class SelfTests
{
    private static int _passed;
    private static int _failed;

    public static void RunAll()
    {
        Console.WriteLine("Running self-tests...\n");

        Run("Repository_Add_AssignsIncrementingIds", Repository_Add_AssignsIncrementingIds);
        Run("Repository_GetById_ReturnsNullWhenMissing", Repository_GetById_ReturnsNullWhenMissing);
        Run("TicketService_CreateTicket_RejectsEmptyTitle", TicketService_CreateTicket_RejectsEmptyTitle);
        Run("TicketService_ResolveTicket_SetsResolvedAt", TicketService_ResolveTicket_SetsResolvedAt);
        Run("TicketService_ResolveTicket_ThrowsIfAlreadyClosed", TicketService_ResolveTicket_ThrowsIfAlreadyClosed);
        Run("SlaPolicy_FlagsBreach_WhenOverdueAndOpen", SlaPolicy_FlagsBreach_WhenOverdueAndOpen);
        Run("SlaPolicy_DoesNotFlag_WhenResolvedEvenIfOverdue", SlaPolicy_DoesNotFlag_WhenResolvedEvenIfOverdue);
        Run("SlaPolicy_DoesNotFlag_WhenWithinTarget", SlaPolicy_DoesNotFlag_WhenWithinTarget);
        Run("ReportService_CountByModule_GroupsCorrectly", ReportService_CountByModule_GroupsCorrectly);
        Run("ReportService_AverageResolutionHours_NullWhenNoneResolved", ReportService_AverageResolutionHours_NullWhenNoneResolved);
        Run("Integration_SeedThenReport_MatchesExpectedCounts", Integration_SeedThenReport_MatchesExpectedCounts);

        Console.WriteLine($"\n{_passed} passed, {_failed} failed.");
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine($"  PASS  {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"  FAIL  {name}");
            Console.WriteLine($"        {ex.Message}");
        }
    }

    // --- tiny assert helpers, standing in for an xUnit/NUnit Assert class ---

    private static void AssertEqual<T>(T expected, T actual, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"{context}: expected {expected}, got {actual}");
    }

    private static void AssertTrue(bool condition, string context)
    {
        if (!condition) throw new Exception($"{context}: expected true, got false");
    }

    private static void AssertThrows<TException>(Action action, string context) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new Exception($"{context}: expected {typeof(TException).Name} to be thrown, but it wasn't");
    }

    // --- unit tests: repository in isolation ---

    private static void Repository_Add_AssignsIncrementingIds()
    {
        var repo = new InMemoryTicketRepository();
        var t1 = repo.Add(new Ticket { Title = "A", Module = SystemModule.Other, Priority = Priority.Low });
        var t2 = repo.Add(new Ticket { Title = "B", Module = SystemModule.Other, Priority = Priority.Low });

        AssertEqual(1, t1.Id, "first ticket id");
        AssertEqual(2, t2.Id, "second ticket id");
    }

    private static void Repository_GetById_ReturnsNullWhenMissing()
    {
        var repo = new InMemoryTicketRepository();
        AssertEqual(null, repo.GetById(999), "missing ticket lookup");
    }

    // --- unit tests: service layer in isolation ---

    private static void TicketService_CreateTicket_RejectsEmptyTitle()
    {
        var service = new TicketService(new InMemoryTicketRepository());
        AssertThrows<ArgumentException>(
            () => service.CreateTicket("", SystemModule.DrillAndBlast, Priority.Low),
            "creating a ticket with an empty title");
    }

    private static void TicketService_ResolveTicket_SetsResolvedAt()
    {
        var service = new TicketService(new InMemoryTicketRepository());
        var ticket = service.CreateTicket("Test", SystemModule.Geotechnical, Priority.Medium);

        service.ResolveTicket(ticket.Id);

        AssertEqual(TicketStatus.Resolved, ticket.Status, "status after resolve");
        AssertTrue(ticket.ResolvedAt.HasValue, "ResolvedAt should be set after resolve");
    }

    private static void TicketService_ResolveTicket_ThrowsIfAlreadyClosed()
    {
        var service = new TicketService(new InMemoryTicketRepository());
        var ticket = service.CreateTicket("Test", SystemModule.Geotechnical, Priority.Medium);
        ticket.Status = TicketStatus.Closed;

        AssertThrows<InvalidOperationException>(
            () => service.ResolveTicket(ticket.Id),
            "resolving an already-closed ticket");
    }

    // --- unit tests: SLA business rule ---

    private static void SlaPolicy_FlagsBreach_WhenOverdueAndOpen()
    {
        var ticket = new Ticket
        {
            Id = 1,
            Title = "Overdue critical",
            Module = SystemModule.DrillAndBlast,
            Priority = Priority.Critical, // 4h target
            Status = TicketStatus.Open,
            CreatedAt = DateTime.Now - TimeSpan.FromHours(10),
        };

        AssertTrue(SlaPolicy.HasBreached(ticket), "10h-old critical ticket should breach the 4h target");
    }

    private static void SlaPolicy_DoesNotFlag_WhenResolvedEvenIfOverdue()
    {
        var ticket = new Ticket
        {
            Id = 1,
            Title = "Resolved late",
            Module = SystemModule.DrillAndBlast,
            Priority = Priority.Critical,
            Status = TicketStatus.Resolved,
            CreatedAt = DateTime.Now - TimeSpan.FromHours(10),
            ResolvedAt = DateTime.Now,
        };

        AssertTrue(!SlaPolicy.HasBreached(ticket), "resolved tickets should never count as an active breach");
    }

    private static void SlaPolicy_DoesNotFlag_WhenWithinTarget()
    {
        var ticket = new Ticket
        {
            Id = 1,
            Title = "Fresh low priority",
            Module = SystemModule.ReservesAndResources,
            Priority = Priority.Low, // 168h target
            Status = TicketStatus.Open,
            CreatedAt = DateTime.Now - TimeSpan.FromHours(2),
        };

        AssertTrue(!SlaPolicy.HasBreached(ticket), "a 2h-old low priority ticket should not breach a 168h target");
    }

    // --- unit test: reporting ---

    private static void ReportService_CountByModule_GroupsCorrectly()
    {
        var tickets = new List<Ticket>
        {
            new() { Id = 1, Title = "A", Module = SystemModule.DrillAndBlast, Priority = Priority.Low },
            new() { Id = 2, Title = "B", Module = SystemModule.DrillAndBlast, Priority = Priority.Low },
            new() { Id = 3, Title = "C", Module = SystemModule.Geotechnical, Priority = Priority.Low },
        };

        var counts = ReportService.CountByModule(tickets);

        AssertEqual(2, counts[SystemModule.DrillAndBlast], "DrillAndBlast count");
        AssertEqual(1, counts[SystemModule.Geotechnical], "Geotechnical count");
    }

    private static void ReportService_AverageResolutionHours_NullWhenNoneResolved()
    {
        var tickets = new List<Ticket>
        {
            new() { Id = 1, Title = "A", Module = SystemModule.Other, Priority = Priority.Low },
        };

        AssertEqual(null, ReportService.AverageResolutionHours(tickets), "average with nothing resolved");
    }

    // --- integration test: repository + service + reporting together ---

    private static void Integration_SeedThenReport_MatchesExpectedCounts()
    {
        var repo = new InMemoryTicketRepository();
        var service = new TicketService(repo);

        service.ImportSeedTicket("Seed A", SystemModule.DrillAndBlast, Priority.High, TicketStatus.Open, DateTime.Now - TimeSpan.FromHours(30));
        service.ImportSeedTicket("Seed B", SystemModule.DrillAndBlast, Priority.Low, TicketStatus.Open, DateTime.Now - TimeSpan.FromHours(1));
        service.ImportSeedTicket("Seed C", SystemModule.Geotechnical, Priority.Critical, TicketStatus.Resolved, DateTime.Now - TimeSpan.FromHours(20));

        var all = service.GetAll().ToList();
        AssertEqual(3, all.Count, "total imported tickets");

        var breached = service.GetBreachedTickets().ToList();
        // Seed A: High priority, 30h old, 24h target -> breached.
        // Seed B: Low priority, 1h old, 168h target -> not breached.
        // Seed C: Resolved -> never counts as breached regardless of age.
        AssertEqual(1, breached.Count, "breach count after seeding");
        AssertEqual("Seed A", breached[0].Title, "which ticket breached");
    }
}
