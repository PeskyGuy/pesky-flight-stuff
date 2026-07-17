using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace Pesky
{
    public class Gene_Flight : Gene, IFlightSource
    {
        private bool flightActive = false;

        public int Priority => def.GetModExtension<FlightSourceExtension>()?.priority ?? 100;
        public FlightSourceType SourceType => def.GetModExtension<FlightSourceExtension>()?.sourceType ?? FlightSourceType.Gene;
        public bool IsActive { get => flightActive; set => flightActive = value; }
        public bool CanFly => true; // Genes don't run out of fuel
        public string SourceId => "Gene_" + def.defName;

        public override void PostAdd()
        {
            base.PostAdd();
            FlightUtility.GetRegistry(pawn).Register(this);
        }

        public override void PostRemove()
        {
            base.PostRemove();
            FlightUtility.GetRegistry(pawn).Unregister(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref flightActive, "flightActive", false);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            var baseGizmos = base.GetGizmos();
            if (baseGizmos != null)
            {
                foreach (Gizmo gizmo in baseGizmos)
                {
                    yield return gizmo;
                }
            }

            if (FlightUtility.GetRegistry(pawn).IsSuppressed(this)) yield break;

            if (pawn != null && pawn.Faction == Faction.OfPlayer)
            {
                Command_Toggle toggle = new Command_Toggle();
                toggle.defaultLabel = "Toggle Flight";
                toggle.defaultDesc = "Toggle flight. Bypasses terrain movement penalties and traps.";
                toggle.icon = ContentFinder<Texture2D>.Get("UI/Icons/FlightToggle", false) ?? BaseContent.BadTex;
                toggle.isActive = () => flightActive;
                toggle.toggleAction = delegate
                {
                    ToggleFlight(!flightActive);
                };

                if (pawn.Downed)
                {
                    toggle.Disable("Cannot fly while downed.");
                }
                else if (!pawn.Awake())
                {
                    toggle.Disable("Cannot fly while sleeping.");
                }

                yield return toggle;
            }
        }

        private void ToggleFlight(bool enable)
        {
            if (enable && (pawn.Downed || !pawn.Awake() || pawn.InBed()))
            {
                Messages.Message("Cannot fly while downed or sleeping.", pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }
            flightActive = enable;
            FlightUtility.CheckFlightState(pawn);
        }

        public override void Tick()
        {
            base.Tick();

            var registry = FlightUtility.GetRegistry(pawn);
            if (!registry.IsRegistered(this)) registry.Register(this);

            if (registry.IsSuppressed(this))
            {
                if (flightActive) ToggleFlight(false);
                return;
            }

            if (pawn.IsHashIntervalTick(60) && pawn.Faction != Faction.OfPlayer && !pawn.Downed && pawn.Awake() && !pawn.InBed())
            {
                bool shouldFly = pawn.mindState?.enemyTarget != null || (pawn.mindState?.duty != null && pawn.mindState.duty.def.alwaysShowWeapon);
                if (shouldFly && !flightActive) ToggleFlight(true);
                else if (!shouldFly && flightActive) ToggleFlight(false);
            }

            if (flightActive)
            {
                if (pawn.Downed || pawn.InBed() || !pawn.Awake())
                {
                    ToggleFlight(false);
                }
            }
        }
    }
}
