using RimWorld;
using Verse;

namespace Pesky
{
    public enum FlightSourceType
    {
        Gene,
        Apparel,
        Implant,
        Biological
    }

    public interface IFlightSource
    {
        int Priority { get; }
        FlightSourceType SourceType { get; }
        bool IsActive { get; set; }
        bool CanFly { get; }
        string SourceId { get; }
    }
}
