using EngineeringSystemsDesk.Models;
using EngineeringSystemsDesk.Repositories;

namespace EngineeringSystemsDesk.Services;

// The service layer is where business logic lives, kept separate from both
// the storage layer (Repositories) and the UI layer (Program.cs). Program.cs
// should never talk to InMemoryTicketRepository directly - it goes through
// TicketService, so the rules below are enforced in exactly one place no
// matter which part of the app is creating or updating tickets.
public class TicketService
{
    private readonly ITicketRepository _repository;

    // Constructor takes the interface, not the concrete class. This is
    // "dependency injection" done by hand - the service doesn't know or
    // care whether it's talking to an in-memory list or (eventually) a real
    // database, only that it satisfies ITicketRepository.
    public TicketService(ITicketRepository repository)
    {
        _repository = repository;
    }

    public Ticket CreateTicket(string title, SystemModule module, Priority priority)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Ticket title cannot be empty.", nameof(title));

        var ticket = new Ticket { Title = title, Module = module, Priority = priority };
        return _repository.Add(ticket);
    }

    // Only used when importing seed/demo data with a known historical
    // creation time (e.g. from a CSV of "existing" tickets), so SLA breach
    // demos are realistic instead of everything looking freshly created.
    public Ticket ImportSeedTicket(string title, SystemModule module, Priority priority, TicketStatus status, DateTime createdAt)
    {
        var ticket = new Ticket { Title = title, Module = module, Priority = priority, Status = status };
        return _repository.AddWithTimestamp(ticket, createdAt);
    }

    public IEnumerable<Ticket> GetAll() => _repository.GetAll();

    public Ticket? GetById(int id) => _repository.GetById(id);

    public void ResolveTicket(int id)
    {
        var ticket = _repository.GetById(id)
            ?? throw new KeyNotFoundException($"Ticket {id} not found.");
        ticket.Resolve();
        _repository.Update(ticket);
    }

    public void AddComment(int id, string author, string text)
    {
        var ticket = _repository.GetById(id)
            ?? throw new KeyNotFoundException($"Ticket {id} not found.");
        ticket.AddComment(author, text);
        _repository.Update(ticket);
    }

    // Tickets that have blown past their SLA target and are still open.
    // This is the kind of "who needs attention right now" view a support
    // desk actually cares about day to day.
    public IEnumerable<Ticket> GetBreachedTickets() =>
        _repository.GetAll().Where(SlaPolicy.HasBreached);
}
