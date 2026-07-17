using Verse;

namespace Pesky
{
    public class FlightSourceExtension : DefModExtension
    {
        /// <summary>
        /// Determines which flight source wins when multiple are active/available on the same pawn.
        /// Higher priority suppresses lower. Convention: Gene ~100, Apparel ~200, Implant ~300.
        /// Pick a value in the appropriate band, or above 300 to always take precedence.
        /// </summary>
        public int priority = 100;
        public FlightSourceType sourceType = FlightSourceType.Gene;
        public bool showIdleGraphic = true;
        
        public float cruiseHeight = 1.3f;
        public float riseFallSpeed = 0.05f;
        public float tiltAngle = 15f;
        public int frameTicksNormal = 10;
        public int frameTicksTransition = 6;
        public float bobAmplitude = 0.08f;
        
        public string iconPath = "UI/Icons/FlightToggle";
    }
}
