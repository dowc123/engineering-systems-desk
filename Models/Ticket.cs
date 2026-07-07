namespace EngineeringSystemsDesk.Models;

// The core entity: a single incident/problem/improvement ticket against
// one of the engineering systems. Mutable by design (status and comments
// change over the ticket's lifetime) - unlike the immutable `record` types
// used in the earlier drill-hole project, this is a plain class because its
// state is meant to change after creation.
public class Ticket
{
    public int Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public SystemModule Module { get; set; }
    public Priority Priority { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime? ResolvedAt { get; set; }
    public List<Comment> Comments { get; } = new();

    public void AddComment(string author, string text)
    {
        Comments.Add(new Comment { Author = author, Text = text });
    }

    // Moves the ticket to Resolved and stamps the resolution time.
    // Guard clause keeps the state machine honest - you can't "resolve"
    // something that's already closed.
    public void Resolve()
    {
        if (Status == TicketStatus.Closed)
            throw new InvalidOperationException($"Ticket {Id} is already closed and cannot be resolved.");

        Status = TicketStatus.Resolved;
        ResolvedAt = DateTime.Now;
    }

    // How long the ticket has been open (or, if resolved, how long it took).
    public TimeSpan Age =>
        (ResolvedAt ?? DateTime.Now) - CreatedAt;

    public override string ToString() =>
        $"#{Id,-3} [{Priority,-8}] [{Status,-11}] {Module,-20} \"{Title}\"";
}
