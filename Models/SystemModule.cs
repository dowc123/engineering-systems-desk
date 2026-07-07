namespace EngineeringSystemsDesk.Models;

// Which engineering system a ticket relates to. Mirrors the systems named
// in the posting: Drill and Blast, Geology/Reserves, Geotechnical.
public enum SystemModule
{
    DrillAndBlast,
    Geotechnical,
    ReservesAndResources,
    Other
}
