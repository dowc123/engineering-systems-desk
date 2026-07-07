using System.Globalization;
using EngineeringSystemsDesk.Models;
using EngineeringSystemsDesk.Repositories;
using EngineeringSystemsDesk.Services;
using EngineeringSystemsDesk.Testing;

namespace EngineeringSystemsDesk;

public static class Program
{
    public static void Main(string[] args)
    {
        // Run the hand-rolled test suite instead of the app when asked.
        // No xUnit/NUnit here since NuGet isn't reachable in the sandbox
        // this was written in - see Testing/SelfTests.cs for details.
        if (args.Contains("--test"))
        {
            SelfTests.RunAll();
            return;
        }

        var repository = new InMemoryTicketRepository();
        var service = new TicketService(repository);

        SeedFromCsv(service, "Data/seed_tickets.csv");

        Console.WriteLine("Engineering Systems Support Desk");
        Console.WriteLine("Loaded seed tickets. Type 'help' for commands.\n");

        RunMenu(service);
    }

    private static void RunMenu(TicketService service)
    {
        bool running = true;
        while (running)
        {
            Console.Write("> ");
            string? input = Console.ReadLine()?.Trim().ToLowerInvariant();

            switch (input)
            {
                case "help":
                    PrintHelp();
                    break;
                case "list":
                    foreach (var t in service.GetAll())
                        Console.WriteLine(t);
                    break;
                case "breaches":
                    var breached = service.GetBreachedTickets().ToList();
                    if (breached.Count == 0)
                        Console.WriteLine("No SLA breaches.");
                    else
                        foreach (var t in breached)
                            Console.WriteLine($"{t} (open {t.Age.TotalHours:F1}h)");
                    break;
                case "report":
                    ReportService.PrintSummary(service.GetAll());
                    break;
                case "add":
                    AddTicketInteractive(service);
                    break;
                case "resolve":
                    ResolveTicketInteractive(service);
                    break;
                case "exit":
                case "quit":
                    running = false;
                    break;
                default:
                    Console.WriteLine("Unknown command. Type 'help' for a list of commands.");
                    break;
            }
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            Commands:
              list      - show all tickets
              add       - create a new ticket
              resolve   - mark a ticket resolved by id
              breaches  - show tickets that have missed their SLA target
              report    - print summary statistics
              exit      - quit
            """);
    }

    private static void AddTicketInteractive(TicketService service)
    {
        Console.Write("Title: ");
        string title = Console.ReadLine() ?? string.Empty;

        Console.Write($"Module ({string.Join("/", Enum.GetNames<SystemModule>())}): ");
        if (!Enum.TryParse<SystemModule>(Console.ReadLine(), true, out var module))
        {
            Console.WriteLine("Invalid module, defaulting to Other.");
            module = SystemModule.Other;
        }

        Console.Write($"Priority ({string.Join("/", Enum.GetNames<Priority>())}): ");
        if (!Enum.TryParse<Priority>(Console.ReadLine(), true, out var priority))
        {
            Console.WriteLine("Invalid priority, defaulting to Medium.");
            priority = Priority.Medium;
        }

        try
        {
            var ticket = service.CreateTicket(title, module, priority);
            Console.WriteLine($"Created {ticket}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Could not create ticket: {ex.Message}");
        }
    }

    private static void ResolveTicketInteractive(TicketService service)
    {
        Console.Write("Ticket id to resolve: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Not a valid ticket id.");
            return;
        }

        try
        {
            service.ResolveTicket(id);
            Console.WriteLine($"Ticket {id} resolved.");
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            Console.WriteLine(ex.Message);
        }
    }

    // Loads seed_tickets.csv, using TryParse throughout so a single bad row
    // logs a warning and gets skipped instead of crashing the whole import -
    // the same defensive pattern used in the drill-hole validator.
    private static void SeedFromCsv(TicketService service, string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Seed file not found at {path}, starting with an empty desk.");
            return;
        }

        var lines = File.ReadAllLines(path);
        int imported = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] fields = lines[i].Split(',');

            if (fields.Length != 5)
            {
                Console.WriteLine($"Skipping seed row {i + 1}: expected 5 columns, found {fields.Length}");
                continue;
            }

            string title = fields[0].Trim();
            bool moduleOk = Enum.TryParse<SystemModule>(fields[1].Trim(), true, out var module);
            bool priorityOk = Enum.TryParse<Priority>(fields[2].Trim(), true, out var priority);
            bool statusOk = Enum.TryParse<TicketStatus>(fields[3].Trim(), true, out var status);
            bool hoursOk = double.TryParse(fields[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double hoursAgo);

            if (!moduleOk || !priorityOk || !statusOk || !hoursOk)
            {
                Console.WriteLine($"Skipping seed row {i + 1}: could not parse one or more fields.");
                continue;
            }

            var createdAt = DateTime.Now - TimeSpan.FromHours(hoursAgo);
            var ticket = service.ImportSeedTicket(title, module, priority, status, createdAt);

            // For rows already marked Resolved/Closed, backfill a plausible
            // ResolvedAt so average-resolution-time reporting has data to
            // work with, instead of every seeded "resolved" ticket showing
            // as still open.
            if (status is TicketStatus.Resolved or TicketStatus.Closed)
            {
                ticket.ResolvedAt = createdAt + TimeSpan.FromHours(hoursAgo * 0.6);
            }

            imported++;
        }

        Console.WriteLine($"Imported {imported} seed tickets from {path}.");
    }
}
