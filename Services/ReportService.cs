using EngineeringSystemsDesk.Models;

namespace EngineeringSystemsDesk.Services;

// Read-only reporting queries over a ticket set. Each method here is the
// LINQ equivalent of a SQL aggregate query - GroupBy + Count is basically
// "SELECT Module, COUNT(*) FROM Tickets GROUP BY Module", and Average is
// "SELECT AVG(...)". Kept separate from TicketService because reporting is
// a different concern from creating/mutating tickets.
public static class ReportService
{
    public static Dictionary<SystemModule, int> CountByModule(IEnumerable<Ticket> tickets) =>
        tickets
            .GroupBy(t => t.Module)
            .ToDictionary(g => g.Key, g => g.Count());

    public static Dictionary<TicketStatus, int> CountByStatus(IEnumerable<Ticket> tickets) =>
        tickets
            .GroupBy(t => t.Status)
            .ToDictionary(g => g.Key, g => g.Count());

    public static Dictionary<Priority, int> CountByPriority(IEnumerable<Ticket> tickets) =>
        tickets
            .GroupBy(t => t.Priority)
            .ToDictionary(g => g.Key, g => g.Count());

    // Average time-to-resolution, in hours, across resolved tickets only.
    // Returns null when there's nothing resolved yet, rather than dividing
    // by zero or reporting a misleading 0.
    public static double? AverageResolutionHours(IEnumerable<Ticket> tickets)
    {
        var resolved = tickets
            .Where(t => t.ResolvedAt.HasValue)
            .Select(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours)
            .ToList();

        return resolved.Count == 0 ? null : resolved.Average();
    }

    public static void PrintSummary(IEnumerable<Ticket> allTickets)
    {
        var tickets = allTickets.ToList();

        Console.WriteLine();
        Console.WriteLine("=== Ticket Summary ===");
        Console.WriteLine($"Total tickets: {tickets.Count}");

        Console.WriteLine("\nBy status:");
        foreach (var (status, count) in CountByStatus(tickets))
            Console.WriteLine($"  {status,-12} {count}");

        Console.WriteLine("\nBy module:");
        foreach (var (module, count) in CountByModule(tickets))
            Console.WriteLine($"  {module,-22} {count}");

        Console.WriteLine("\nBy priority:");
        foreach (var (priority, count) in CountByPriority(tickets))
            Console.WriteLine($"  {priority,-10} {count}");

        var avgHours = AverageResolutionHours(tickets);
        Console.WriteLine(avgHours is null
            ? "\nAverage resolution time: n/a (nothing resolved yet)"
            : $"\nAverage resolution time: {avgHours:F1} hours");

        var breached = tickets.Where(SlaPolicy.HasBreached).ToList();
        Console.WriteLine($"\nSLA breaches: {breached.Count}");
        foreach (var t in breached)
            Console.WriteLine($"  {t} (open {t.Age.TotalHours:F1}h, target {SlaPolicy.TargetFor(t.Priority).TotalHours:F0}h)");
    }
}
