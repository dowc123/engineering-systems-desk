namespace EngineeringSystemsDesk.Repositories;

// A generic interface means this contract works for any entity type T,
// not just Ticket. In a real system you could swap InMemoryRepository<T>
// for a SqlServerRepository<T> that talks to SQL Server via ADO.NET/EF Core
// without changing any code that depends on IRepository<T> - this is the
// same idea as depending on an abstract base class rather than a concrete
// implementation, so the storage layer can change without touching the
// business logic that uses it.
public interface IRepository<T> where T : class
{
    T Add(T entity);
    T? GetById(int id);
    IEnumerable<T> GetAll();
    void Update(T entity);
    bool Delete(int id);
}
