using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;

namespace Pesky
{
    public class PawnRenderNode_FlightAnimated : PawnRenderNode
    {
        private Graphic[] animFrames;
        private string cachedTexPath;
        private Color cachedColor;

        public PawnRenderNode_FlightAnimated(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) 
            : base(pawn, props, tree)
        {
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            if (pawn == null) return GetBaseGraphic(pawn);

            var state = FlightUtility.GetState(pawn);
            if (state == null) return GetBaseGraphic(pawn);

            var registry = FlightUtility.GetRegistry(pawn);

            var ext = GetMyExtension(pawn);
            FlightSourceType myType = ext?.sourceType ?? FlightSourceType.Gene;
            
            if (ext == null)
            {
                Log.WarningOnce($"[Pesky] PawnRenderNode_FlightAnimated could not find FlightSourceExtension for texPath {this.props.texPath}. Defaulting to Gene.", this.props.GetHashCode());
            }

            // If I am biological wings, should I hide?
            if (myType == FlightSourceType.Gene && registry.ShouldHideBiologicalWings())
                return null;

            // Find if there's any active source that matches my type
            var mySource = registry.sources.FirstOrDefault(s => s.SourceType == myType);
            bool isSpecificSourceActive = mySource != null && mySource.IsActive;

            // If another source is active and has a higher priority, and it's not my type, maybe I should be suppressed?
            // Actually, biological wings aren't suppressed visually by thruster boots, only by implants. We already handled implants above.
            // But if I am apparel or implant, and I am NOT the active flight source, I shouldn't animate flight.
            
            var frames = GetAnimFrames(this.props.texPath, base.ColorFor(pawn));
            if (frames == null || frames.Length == 0)
                return GetBaseGraphic(pawn);

            if (isSpecificSourceActive && (state.flightEnabled || state.currentHeight > 0f))
            {
                return frames[state.currentFrame % frames.Length];
            }
            
            if (myType == FlightSourceType.Apparel)
            {
                // Apparel is drawn by normal apparel rendering when idle. We don't want to draw idle frames here.
                return null;
            }

            if (myType == FlightSourceType.Implant)
            {
                // Implants must draw their own idle graphic unless disabled or suppressed.
                if (mySource != null && registry.IsSuppressed(mySource)) return null;
                if (ext != null && !ext.showIdleGraphic) return null;
                return GetBaseGraphic(pawn);
            }

            // Idle frame for biological wings
            return GetBaseGraphic(pawn);
        }

        private FlightSourceExtension GetMyExtension(Pawn pawn)
        {
            if (this.apparel != null) return this.apparel.def.GetModExtension<FlightSourceExtension>();
            if (this.gene != null) return this.gene.def.GetModExtension<FlightSourceExtension>();
            if (this.hediff != null) return this.hediff.def.GetModExtension<FlightSourceExtension>();
            return null;
        }

        private Graphic GetBaseGraphic(Pawn pawn)
        {
            if (this.props.texPath.NullOrEmpty()) return base.GraphicFor(pawn);
            return GraphicDatabase.Get<Graphic_FlightAnimMulti>(this.props.texPath, ShaderDatabase.Cutout, Vector2.one, base.ColorFor(pawn));
        }

        public Graphic[] GetAnimFrames(string texPath, Color color)
        {
            if (animFrames != null && cachedTexPath == texPath && cachedColor == color)
                return animFrames;

            cachedTexPath = texPath;
            cachedColor = color;

            int slashIndex = texPath.LastIndexOf('/');
            if (slashIndex < 0) return null;

            string dir = texPath.Substring(0, slashIndex);
            string fileName = texPath.Substring(slashIndex + 1);

            animFrames = new Graphic[4];
            animFrames[0] = GraphicDatabase.Get<Graphic_FlightAnimMulti>(dir + "/Animation/" + fileName + "_Up", ShaderDatabase.Cutout, Vector2.one, color);
            animFrames[1] = GraphicDatabase.Get<Graphic_FlightAnimMulti>(dir + "/Animation/" + fileName + "_Mid1", ShaderDatabase.Cutout, Vector2.one, color);
            animFrames[2] = GraphicDatabase.Get<Graphic_FlightAnimMulti>(dir + "/Animation/" + fileName + "_Down", ShaderDatabase.Cutout, Vector2.one, color);
            animFrames[3] = GraphicDatabase.Get<Graphic_FlightAnimMulti>(dir + "/Animation/" + fileName + "_Mid2", ShaderDatabase.Cutout, Vector2.one, color);

            // Fallback to normal graphic if missing frames
            if (animFrames[0] == null || animFrames[0].MatSingle == BaseContent.BadMat)
                return null;

            return animFrames;
        }
    }
    public class Graphic_FlightAnimMulti : Graphic
    {
        private Material matSouth;
        private Material matEast;
        private Material matWest;

        public override Material MatSingle => matSouth;
        public override Material MatWest => matWest ?? matEast;
        public override Material MatSouth => matSouth;
        public override Material MatEast => matEast;
        public override Material MatNorth => matSouth; // North explicitly uses South

        public override void Init(GraphicRequest req)
        {
            this.data = req.graphicData;
            this.path = req.path;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;

            Texture2D texSouth = ContentFinder<Texture2D>.Get(req.path + "_south", false);
            if (texSouth == null) texSouth = ContentFinder<Texture2D>.Get(req.path, false);

            if (texSouth != null)
            {
                MaterialRequest reqS = default(MaterialRequest);
                reqS.mainTex = texSouth;
                reqS.shader = req.shader;
                reqS.color = req.color;
                reqS.colorTwo = req.colorTwo;
                reqS.renderQueue = req.renderQueue;
                reqS.shaderParameters = req.shaderParameters;
                matSouth = MaterialPool.MatFrom(reqS);
            }
            else
            {
                matSouth = BaseContent.BadMat;
            }

            Texture2D texEast = ContentFinder<Texture2D>.Get(req.path + "_east", false);
            if (texEast != null)
            {
                MaterialRequest reqE = default(MaterialRequest);
                reqE.mainTex = texEast;
                reqE.shader = req.shader;
                reqE.color = req.color;
                reqE.colorTwo = req.colorTwo;
                reqE.renderQueue = req.renderQueue;
                reqE.shaderParameters = req.shaderParameters;
                matEast = MaterialPool.MatFrom(reqE);
            }
            else
            {
                matEast = matSouth;
            }

            Texture2D texWest = ContentFinder<Texture2D>.Get(req.path + "_west", false);
            if (texWest != null)
            {
                MaterialRequest reqW = default(MaterialRequest);
                reqW.mainTex = texWest;
                reqW.shader = req.shader;
                reqW.color = req.color;
                reqW.colorTwo = req.colorTwo;
                reqW.renderQueue = req.renderQueue;
                reqW.shaderParameters = req.shaderParameters;
                matWest = MaterialPool.MatFrom(reqW);
            }
            else
            {
                matWest = matEast;
            }
        }

        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            switch (rot.AsInt)
            {
                case 0: return MatNorth;
                case 1: return MatEast;
                case 2: return MatSouth;
                case 3: return MatWest;
                default: return BaseContent.BadMat;
            }
        }
    }
}
