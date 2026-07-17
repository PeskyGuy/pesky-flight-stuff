using Verse;

namespace Pesky
{
    public class FlightSourceExtension : DefModExtension
    {
        public int priority = 100;
        public FlightSourceType sourceType = FlightSourceType.Gene;
        public bool showIdleGraphic = true;
    }
}
