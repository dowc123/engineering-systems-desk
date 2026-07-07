using EngineeringSystemsDesk.Models;

namespace EngineeringSystemsDesk.Repositories;

// A concrete, in-memory implementation of IRepository<Ticket>. Standing in
// for what would be a SQL Server table in production - the interface above
// is what makes that swap possible later without changing TicketService.
public class InMemoryTicketRepository : ITicketRepository
{
    private readonly List<Ticket> _tickets = new();
    private int _nextId = 1;

    public Ticket Add(Ticket entity)
    {
        // Ignore any Id the caller set - the repository owns id assignment,
        // the same way a SQL Server IDENTITY column would.
        var ticket = new Ticket
        {
            Id = _nextId++,
            Title = entity.Title,
            Module = entity.Module,
            Priority = entity.Priority,
            Status = entity.Status,
        };
        _tickets.Add(ticket);
        return ticket;
    }

    // Used only when importing historical/seed data, where we want to
    // preserve an explicit CreatedAt (e.g. "this ticket was opened 30 hours
    // ago") instead of stamping it with DateTime.Now like a freshly created
    // ticket. Kept separate from Add() so normal ticket creation can't
    // accidentally backdate a ticket.
    public Ticket AddWithTimestamp(Ticket entity, DateTime createdAt)
    {
        var ticket = new Ticket
        {
            Id = _nextId++,
            Title = entity.Title,
            Module = entity.Module,
            Priority = entity.Priority,
            Status = entity.Status,
            CreatedAt = createdAt,
        };
        _tickets.Add(ticket);
        return ticket;
    }

    public Ticket? GetById(int id) =>
        _tickets.FirstOrDefault(t => t.Id == id);

    public IEnumerable<Ticket> GetAll() => _tickets.AsReadOnly();

    public void Update(Ticket entity)
    {
        // No-op for reference types stored directly in the list: since
        // Ticket is a class, the caller already holds a reference to the
        // same object that's in _tickets, so mutations are visible
        // immediately. This method exists to satisfy the interface and to
        // make the intent explicit at call sites (and so a future
        // database-backed implementation has somewhere to put a real
        // UPDATE statement).
        if (GetById(entity.Id) is null)
            throw new KeyNotFoundException($"Ticket {entity.Id} not found.");
    }

    public bool Delete(int id)
    {
        var ticket = GetById(id);
        if (ticket is null) return false;
        return _tickets.Remove(ticket);
    }
}
