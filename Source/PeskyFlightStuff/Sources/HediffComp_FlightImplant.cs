using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace Pesky
{
    public class HediffCompProperties_FlightImplant : HediffCompProperties
    {
        public float maxFuel = 100f;
        public float fuelDrainPerTick = 0.05f;
        public float fuelRechargePerTick = 0.01f;

        public HediffCompProperties_FlightImplant()
        {
            this.compClass = typeof(HediffComp_FlightImplant);
        }
    }

    public class HediffComp_FlightImplant : HediffComp, IFlightSource
    {
        public HediffCompProperties_FlightImplant Props => (HediffCompProperties_FlightImplant)props;
        public float fuel = 0f;
        private bool flightActive = false;

        public int Priority => Def.GetModExtension<FlightSourceExtension>()?.priority ?? 300;
        public FlightSourceType SourceType => Def.GetModExtension<FlightSourceExtension>()?.sourceType ?? FlightSourceType.Implant;
        public bool IsActive { get => flightActive; set => flightActive = value; }
        public bool CanFly => fuel > 0;
        public string SourceId => "Implant_" + parent.GetUniqueLoadID();
        public Def SourceDef => Def;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref fuel, "fuel", 0f);
            Scribe_Values.Look(ref flightActive, "flightActive", false);
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            fuel = Props.maxFuel;
            if (Pawn != null) FlightUtility.GetRegistry(Pawn).Register(this);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (Pawn != null) FlightUtility.GetRegistry(Pawn).Unregister(this);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn wearer = Pawn;
            if (wearer != null)
            {
                var registry = FlightUtility.GetRegistry(wearer);
                if (!registry.IsRegistered(this)) registry.Register(this);

                if (registry.IsSuppressed(this))
                {
                    if (flightActive) ToggleFlight(wearer, false);
                    return;
                }
                
                FlightBehaviorUtility.TickAIController(wearer, flightActive, state => ToggleFlight(wearer, state));
                FlightBehaviorUtility.TickFuel(wearer, ref fuel, Props.maxFuel, Props.fuelDrainPerTick, Props.fuelRechargePerTick, flightActive, state => ToggleFlight(wearer, state));
            }
        }

        private void ToggleFlight(Pawn wearer, bool state)
        {
            FlightBehaviorUtility.ToggleFlight(wearer, state, ref flightActive);
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            Pawn wearer = Pawn;
            if (wearer == null || wearer.Faction != Faction.OfPlayer) yield break;

            if (FlightUtility.GetRegistry(wearer).IsSuppressed(this)) yield break;

            var ext = Def.GetModExtension<FlightSourceExtension>();
            Command_Toggle toggle = new Command_Toggle
            {
                defaultLabel = "Toggle Flight",
                defaultDesc = "Toggle flight. Bypasses terrain movement penalties but drains fuel.",
                icon = ContentFinder<Texture2D>.Get(ext?.iconPath ?? "UI/Icons/FlightToggle", false) ?? BaseContent.BadTex,
                isActive = () => flightActive,
                toggleAction = delegate
                {
                    ToggleFlight(wearer, !flightActive);
                }
            };

            if (wearer.Downed)
            {
                toggle.Disable("Cannot fly while downed.");
            }
            else if (!wearer.Awake())
            {
                toggle.Disable("Cannot fly while sleeping.");
            }

            yield return toggle;

            yield return new Gizmo_FlightFuelStatus
            {
                compImplant = this
            };
        }
    }
}
