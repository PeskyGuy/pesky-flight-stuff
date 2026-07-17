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
        public Def SourceDef => def;

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
                var ext = def.GetModExtension<FlightSourceExtension>();
                Command_Toggle toggle = new Command_Toggle();
                toggle.defaultLabel = "Toggle Flight";
                toggle.defaultDesc = "Toggle flight. Bypasses terrain movement penalties and traps.";
                toggle.icon = (!def.iconPath.NullOrEmpty() ? ContentFinder<Texture2D>.Get(def.iconPath, false) : null) ?? ContentFinder<Texture2D>.Get(ext?.iconPath ?? "UI/Icons/FlightToggle", false) ?? BaseContent.BadTex;
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
            FlightBehaviorUtility.ToggleFlight(pawn, enable, ref flightActive);
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

            FlightBehaviorUtility.TickAIController(pawn, flightActive, state => ToggleFlight(state));

            if (flightActive && pawn.IsHashIntervalTick(60) && pawn.needs?.food != null)
            {
                var ext = def.GetModExtension<FlightSourceExtension>();
                float drain = ext?.hungerDrainPer60Ticks ?? 0.005f;
                if (drain > 0f)
                {
                    pawn.needs.food.CurLevel -= drain;
                    if (pawn.needs.food.CurLevel <= 0f)
                    {
                        pawn.needs.food.CurLevel = 0f;
                        ToggleFlight(false); // Stop flying if starving
                    }
                }
            }
        }
    }
}
