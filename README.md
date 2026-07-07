# Engineering Systems Support Desk (C#)

A console app simulating a lightweight incident/problem ticket tracker for
mine engineering systems (Drill & Blast, Geotechnical, Reserves &
Resources) — the kind of "incident, problem, defect, improvement
management" work described in the posting.

This is a step up in complexity from the earlier drill-hole validator: a
proper layered architecture, interfaces, generics, dependency injection
(done by hand, no framework), business rules, LINQ-based reporting, and a
hand-rolled test suite.

## Architecture

```
Models/          Ticket, Comment, and the SystemModule/Priority/TicketStatus enums
Repositories/     IRepository<T> (generic) and ITicketRepository (Ticket-specific),
                  with InMemoryTicketRepository as the concrete implementation
Services/         TicketService (business logic), SlaPolicy (business rule),
                  ReportService (LINQ-based reporting)
Testing/          SelfTests.cs — hand-rolled test runner (see below)
Data/             seed_tickets.csv — sample tickets loaded on startup
Program.cs        Console menu + CSV import wiring everything together
```

The point of splitting it this way: `Program.cs` never touches
`InMemoryTicketRepository` directly, only `TicketService`, which only
depends on the `ITicketRepository` interface. Swap in a real SQL Server
repository later and nothing above the repository layer has to change —
this is the core idea behind "coding to an interface."

## How to run it

Requires the .NET 8 SDK. As with the earlier project, I could not run this
myself in this sandbox (no NuGet/SDK access here), so please run it before
relying on it:

```bash
cd EngineeringSystemsDesk
dotnet run
```

This loads `Data/seed_tickets.csv` (8 sample tickets with varying ages, so
some already violate their SLA target) and drops you into a menu:

```
> help
> list
> report
> breaches
> add
> resolve
> exit
```

## Running the tests

```bash
dotnet run -- --test
```

This runs 11 hand-written tests (unit tests for the repository, service,
and SLA rule in isolation, plus one integration test that wires repository
+ service + reporting together) and prints PASS/FAIL for each. No test
framework is installed — `SelfTests.cs` explains why and reimplements just
enough of an assert/report pattern to demonstrate the same testing habits
you'd use with xUnit or NUnit.

## Business rule being tested: SLA breach detection

Each ticket has a target resolution time based on priority (Critical: 4h,
High: 24h, Medium: 72h, Low: 168h). A ticket "breaches" if it's still open
and older than its target — this logic lives in one place
(`Services/SlaPolicy.cs`) and is covered by three tests: a ticket that
should breach, one that shouldn't because it's resolved, and one that
shouldn't because it's still within target.

## Honest framing

Same note as the drill-hole project: this was built in one session to
learn C# by building something real, not developed over months. If asked
about it, that's the accurate story — and it's a reasonable one, since it
shows you can pick up a new language by applying patterns (layered
architecture, interfaces, testing discipline) you already use elsewhere.
