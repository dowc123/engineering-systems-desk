using EngineeringSystemsDesk.Models;

namespace EngineeringSystemsDesk.Repositories;

// Extends the generic IRepository<Ticket> with a ticket-specific operation.
// This is a common real-world pattern: keep a generic repository interface
// for the operations every entity shares, and layer entity-specific
// interfaces on top for anything that doesn't generalize (here, seeding
// historical data with an explicit CreatedAt).
public interface ITicketRepository : IRepository<Ticket>
{
    Ticket AddWithTimestamp(Ticket entity, DateTime createdAt);
}
