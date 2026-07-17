using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace Pesky
{
    [StaticConstructorOnStartup]
    public static class FlightPatches
    {
        static FlightPatches()
        {
            var harmony = new Harmony("Pesky.FlightStuff");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Verse.AI.Pawn_PathFollower), "CostToMoveIntoCell", new System.Type[] { typeof(Pawn), typeof(IntVec3) })]
    public static class Patch_CostToMoveIntoCell
    {
        public static void Postfix(Pawn pawn, IntVec3 c, ref float __result)
        {
            if (pawn != null && FlightUtility.IsFlying(pawn))
            {
                float baseCost = (c.x == pawn.Position.x || c.z == pawn.Position.z) ? pawn.TicksPerMoveCardinal : pawn.TicksPerMoveDiagonal;
                __result = baseCost / 1.4f;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "get_DrawPos")]
    public static class Pawn_DrawPos_Patch
    {
        public static void Postfix(Pawn __instance, ref Vector3 __result)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                var state = FlightUtility.GetState(__instance);
                if (state != null && state.currentHeight > 0f)
                {
                    float bobAmplitude = 0.08f;
                    float bobSpeed = 0.05f; 
                    float timeOffset = (__instance.thingIDNumber % 1000) * 10f;
                    float bobFactor = state.currentHeight / 1.3f;
                    float floatOffset = Mathf.Sin(((float)Find.TickManager.TicksGame + timeOffset) * bobSpeed) * bobAmplitude * bobFactor;
                    
                    __result.z += (state.currentHeight + floatOffset);
                }
            }
        }
    }

    [HarmonyPatch(typeof(PawnRenderTree), "AdjustParms")]
    public static class PawnRenderTree_AdjustParms_Patch
    {
        public static void Postfix(ref PawnDrawParms parms)
        {
            if (parms.pawn != null && Current.ProgramState == ProgramState.Playing)
            {
                if (!parms.flags.HasFlag(PawnRenderFlags.Portrait))
                {
                    var state = FlightUtility.GetState(parms.pawn);
                    if (state != null && state.currentTilt != 0f)
                    {
                        parms.matrix *= Matrix4x4.Rotate(Quaternion.Euler(0, state.currentTilt, 0));
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Pawn_Tick_Patch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.Spawned && Current.ProgramState == ProgramState.Playing)
            {
                if (__instance.IsHashIntervalTick(1) && FlightUtility.flightStates.TryGetValue(__instance, out var state))
                {
                    state.Tick(__instance);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "DrawAt")]
    public static class Pawn_DrawAt_Patch
    {
        private static Material shadowMaterial;

        public static void Postfix(Pawn __instance, Vector3 drawLoc, bool flip)
        {
            if (shadowMaterial == null)
            {
                shadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent);
            }

            if (__instance.Spawned && Current.ProgramState == ProgramState.Playing)
            {
                var state = FlightUtility.GetState(__instance);
                if (state != null && state.currentHeight > 0f)
                {
                    float bobAmplitude = 0.08f;
                    float bobSpeed = 0.05f; 
                    float timeOffset = (__instance.thingIDNumber % 1000) * 10f;
                    float bobFactor = state.currentHeight / 1.3f;
                    float floatOffset = Mathf.Sin(((float)Find.TickManager.TicksGame + timeOffset) * bobSpeed) * bobAmplitude * bobFactor;
                    
                    float totalOffset = state.currentHeight + floatOffset;
                    
                    Vector3 groundLoc = drawLoc;
                    groundLoc.z -= (totalOffset + 0.20f);
                    groundLoc.y = AltitudeLayer.Shadows.AltitudeFor();

                    float shadowSize = 1.8f - (totalOffset * 0.3f); 
                    if (shadowSize < 0.8f) shadowSize = 0.8f;

                    Matrix4x4 matrix = default(Matrix4x4);
                    matrix.SetTRS(groundLoc, Quaternion.identity, new Vector3(shadowSize, 1f, shadowSize));
                    Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterial, 0);
                }
            }
        }
    }
}
