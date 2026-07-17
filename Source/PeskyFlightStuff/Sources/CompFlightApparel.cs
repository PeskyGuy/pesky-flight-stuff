using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace Pesky
{
    public class CompProperties_FlightApparel : CompProperties
    {
        public float maxFuel = 100f;
        public float fuelDrainPerTick = 0.05f; 
        public float fuelRechargePerTick = 0.01f;
        
        public CompProperties_FlightApparel()
        {
            this.compClass = typeof(CompFlightApparel);
        }
    }

    public class CompFlightApparel : ThingComp, IFlightSource
    {
        public CompProperties_FlightApparel Props => (CompProperties_FlightApparel)props;
        public float fuel = 0f;
        private bool flightActive = false;

        public int Priority => parent.def.GetModExtension<FlightSourceExtension>()?.priority ?? 200;
        public FlightSourceType SourceType => parent.def.GetModExtension<FlightSourceExtension>()?.sourceType ?? FlightSourceType.Apparel;
        public bool IsActive { get => flightActive; set => flightActive = value; }
        public string SourceId => "Apparel_" + parent.ThingID;
        public bool CanFly 
        {
            get 
            {
                var reloadable = parent.GetComp<CompApparelReloadable>();
                return reloadable != null ? reloadable.RemainingCharges > 0 : fuel > 0;
            }
        }
        public Def SourceDef => parent.def;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref fuel, "fuel", 0f);
            Scribe_Values.Look(ref flightActive, "flightActive", false);
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0f);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                fuel = Props.maxFuel;
            }
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            FlightUtility.GetRegistry(pawn).Register(this);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            FlightUtility.GetRegistry(pawn).Unregister(this);
            ToggleFlight(pawn, false);
        }

        public float tickCounter = 0f;

        public override void CompTick()
        {
            base.CompTick();
            Pawn wearer = ((Apparel)parent).Wearer;
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

                var reloadable = parent.GetComp<CompApparelReloadable>();

                if (reloadable != null)
                {
                    if (flightActive)
                    {
                        tickCounter += Props.fuelDrainPerTick;
                        while (tickCounter >= 1f)
                        {
                            tickCounter -= 1f;
                            if (reloadable.RemainingCharges > 0)
                            {
                                reloadable.UsedOnce();
                            }
                            else
                            {
                                ToggleFlight(wearer, false);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    FlightBehaviorUtility.TickFuel(wearer, ref fuel, Props.maxFuel, Props.fuelDrainPerTick, Props.fuelRechargePerTick, flightActive, state => ToggleFlight(wearer, state));
                }
            }
        }

        private void ToggleFlight(Pawn wearer, bool state)
        {
            FlightBehaviorUtility.ToggleFlight(wearer, state, ref flightActive);
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            Pawn wearer = ((Apparel)parent).Wearer;
            if (wearer == null || wearer.Faction != Faction.OfPlayer) yield break;
            
            if (FlightUtility.GetRegistry(wearer).IsSuppressed(this)) yield break;

            var ext = parent.def.GetModExtension<FlightSourceExtension>();
            Command_Toggle toggle = new Command_Toggle
            {
                defaultLabel = "Toggle Flight",
                defaultDesc = "Toggle flight. Bypasses terrain movement penalties but drains fuel.",
                icon = parent.def.uiIcon ?? ContentFinder<Texture2D>.Get(ext?.iconPath ?? "UI/Icons/FlightToggle", false) ?? BaseContent.BadTex,
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
                compApparel = this
            };
        }

    }

    [StaticConstructorOnStartup]
    public class Gizmo_FlightFuelStatus : Gizmo
    {
        public CompFlightApparel compApparel;
        public HediffComp_FlightImplant compImplant;
        private static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.8f, 0.85f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public override float GetWidth(float maxWidth) => 140f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);
            Rect rect2 = rect.ContractedBy(6f);
            Rect rect3 = rect2;
            rect3.height = rect2.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect3, "Flight Fuel");
            Rect rect4 = rect2;
            rect4.yMin = rect2.y + rect2.height / 2f;
            
            float pct = 0f;
            if (compApparel != null)
            {
                var reloadable = compApparel.parent.GetComp<CompApparelReloadable>();
                if (reloadable != null)
                {
                    pct = (float)reloadable.RemainingCharges / reloadable.MaxCharges;
                }
                else
                {
                    pct = compApparel.fuel / compApparel.Props.maxFuel;
                }
            }
            if (compImplant != null) pct = compImplant.fuel / compImplant.Props.maxFuel;

            Widgets.FillableBar(rect4, pct, FullBarTex, EmptyBarTex, true);
            Text.Font = GameFont.Small;
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
