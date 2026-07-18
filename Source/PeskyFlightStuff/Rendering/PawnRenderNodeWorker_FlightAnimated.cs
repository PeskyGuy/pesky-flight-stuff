using RimWorld;
using UnityEngine;
using Verse;

namespace Pesky
{
    public class PawnRenderNodeWorker_FlightAnimated : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms))
                return false;

            // Only draw if the pawn has the appropriate item or gene.
            return true;
        }

        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            if (parms.facing == Rot4.North)
                return 90f;
            if (parms.facing == Rot4.East || parms.facing == Rot4.West)
                return 90f;
            return base.LayerFor(node, parms);
        }
    }
}
