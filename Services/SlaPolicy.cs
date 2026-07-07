using EngineeringSystemsDesk.Models;

namespace EngineeringSystemsDesk.Services;

// Encodes the business rule for how quickly a ticket of a given priority
// should be resolved. Kept as its own small class so the rule is defined
// in exactly one place - if the SLA policy changes, only this file changes.
public static class SlaPolicy
{
    private static readonly Dictionary<Priority, TimeSpan> Targets = new()
    {
        [Priority.Critical] = TimeSpan.FromHours(4),
        [Priority.High] = TimeSpan.FromHours(24),
        [Priority.Medium] = TimeSpan.FromHours(72),
        [Priority.Low] = TimeSpan.FromHours(168), // 1 week
    };

    public static TimeSpan TargetFor(Priority priority) => Targets[priority];

    // A ticket has "breached" SLA if it's still open/in-progress and has
    // been alive longer than its priority's target resolution time.
    public static bool HasBreached(Ticket ticket)
    {
        if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
            return false;

        return ticket.Age > TargetFor(ticket.Priority);
    }
}
