using RimWorld;
using Verse;

namespace Pesky
{
    public static class FlightBehaviorUtility
    {
        public static void TickAIController(Pawn pawn, bool currentFlightActive, System.Action<bool> toggleAction)
        {
            if (pawn.IsHashIntervalTick(60) && pawn.Faction != Faction.OfPlayer && !pawn.Downed && pawn.Awake() && !pawn.InBed())
            {
                bool shouldFly = pawn.mindState?.enemyTarget != null || (pawn.mindState?.duty != null && pawn.mindState.duty.def.alwaysShowWeapon);
                if (shouldFly && !currentFlightActive) toggleAction(true);
                else if (!shouldFly && currentFlightActive) toggleAction(false);
            }

            if (currentFlightActive && (pawn.Downed || pawn.InBed() || !pawn.Awake()))
            {
                toggleAction(false);
            }
        }

        public static void ToggleFlight(Pawn pawn, bool state, ref bool flightActiveField)
        {
            if (state && (pawn.Downed || !pawn.Awake() || pawn.InBed()))
            {
                if (pawn.Faction == Faction.OfPlayer)
                {
                    Messages.Message("Cannot fly while downed or sleeping.", pawn, MessageTypeDefOf.RejectInput, false);
                }
                return;
            }
            flightActiveField = state;
            FlightUtility.CheckFlightState(pawn);
        }

        public static void TickFuel(Pawn pawn, ref float fuel, float maxFuel, float drainPerTick, float rechargePerTick, bool flightActive, System.Action<bool> toggleAction)
        {
            if (flightActive)
            {
                fuel -= drainPerTick;
                if (fuel <= 0)
                {
                    fuel = 0;
                    toggleAction(false);
                }
            }
            else
            {
                if (fuel < maxFuel)
                {
                    fuel += rechargePerTick;
                    if (fuel > maxFuel) fuel = maxFuel;
                }
            }
        }
    }
}
