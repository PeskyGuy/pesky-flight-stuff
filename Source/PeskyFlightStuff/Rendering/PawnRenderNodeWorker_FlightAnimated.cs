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
            // But since this node is attached via Gene or Apparel, it's only active if they have it.
            return true;
        }
    }
}
