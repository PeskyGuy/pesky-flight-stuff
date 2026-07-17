using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace Pesky
{
    public class FlightState
    {
        public bool flightEnabled = false;
        public int currentFrame = 0;
        public float currentHeight = 0f;
        public float currentTilt = 0f;
        public List<CompFlightApparel> apparelComps = new List<CompFlightApparel>();
        public bool initializedComps = false;

        public void Tick(Pawn pawn)
        {

            float targetHeight = flightEnabled ? 1.3f : 0f;
            float previousHeight = currentHeight;
            currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, 0.05f);

            bool isTransitioning = (currentHeight > 0f && currentHeight < 1.3f);
            
            if (flightEnabled || currentHeight > 0f)
            {
                float targetTilt = 0f;
                if (flightEnabled && pawn.pather != null && pawn.pather.MovingNow)
                {
                    if (pawn.Rotation == Rot4.East)
                        targetTilt = 15f; 
                    else if (pawn.Rotation == Rot4.West)
                        targetTilt = -15f;
                }
                currentTilt = Mathf.MoveTowardsAngle(currentTilt, targetTilt, 0.5f);

                int ticksPerFrame = isTransitioning ? 6 : 10;
                if (pawn.IsHashIntervalTick(ticksPerFrame))
                {
                    currentFrame = (currentFrame + 1) % 4;
                    if (pawn.Spawned)
                        pawn.Drawer?.renderer?.renderTree?.SetDirty();
                }
            }
            else if (previousHeight > 0f && currentHeight == 0f)
            {
                // Just hit 0, set dirty one last time to clear graphics
                currentTilt = 0f;
                if (pawn.Spawned)
                    pawn.Drawer?.renderer?.renderTree?.SetDirty();
            }
            else if (currentTilt != 0f)
            {
                currentTilt = 0f;
                if (pawn.Spawned) pawn.Drawer?.renderer?.renderTree?.SetDirty();
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class FlightUtility
    {
        public static ConditionalWeakTable<Pawn, FlightState> flightStates = new ConditionalWeakTable<Pawn, FlightState>();
        public static ConditionalWeakTable<Pawn, FlightSourceRegistry> registries = new ConditionalWeakTable<Pawn, FlightSourceRegistry>();

        public static FlightState GetState(Pawn pawn)
        {
            if (!flightStates.TryGetValue(pawn, out var state))
            {
                state = new FlightState();
                flightStates.Add(pawn, state);
            }
            return state;
        }

        public static FlightSourceRegistry GetRegistry(Pawn pawn)
        {
            if (!registries.TryGetValue(pawn, out var registry))
            {
                registry = new FlightSourceRegistry();
                registries.Add(pawn, registry);
                registry.RegisterExternalGenes(pawn);
            }
            return registry;
        }

        public static bool IsFlying(Pawn pawn)
        {
            if (pawn == null) return false;
            return GetState(pawn).flightEnabled;
        }

        public static void SetFlying(Pawn pawn, bool flying)
        {
            var state = GetState(pawn);
            if (state.flightEnabled != flying)
            {
                state.flightEnabled = flying;
                state.currentFrame = 0;

                if (pawn.health != null && pawn.health.hediffSet != null)
                {
                    var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Pesky_FlightActive");
                    if (hediffDef != null)
                    {
                        if (flying && !pawn.health.hediffSet.HasHediff(hediffDef))
                        {
                            pawn.health.AddHediff(hediffDef);
                        }
                        else if (!flying)
                        {
                            var h = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                            if (h != null)
                                pawn.health.RemoveHediff(h);
                        }
                    }
                }

                if (pawn.Spawned)
                    pawn.Drawer?.renderer?.renderTree?.SetDirty();
            }
        }

        public static void CheckFlightState(Pawn pawn)
        {
            var registry = GetRegistry(pawn);
            bool shouldFly = registry.GetActiveSource() != null;
            SetFlying(pawn, shouldFly);
        }
    }
}
