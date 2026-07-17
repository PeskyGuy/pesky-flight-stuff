using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Pesky
{
    public class FlightSourceRegistry
    {
        public List<IFlightSource> sources = new List<IFlightSource>();

        public void Register(IFlightSource source)
        {
            if (!sources.Contains(source))
            {
                sources.Add(source);
            }
        }

        public void RegisterExternalGenes(Pawn pawn)
        {
            if (pawn.genes != null)
            {
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    if (gene is IFlightSource) continue; // Already handled natively
                    
                    var ext = gene.def.GetModExtension<FlightSourceExtension>();
                    if (ext != null)
                    {
                        var adapter = new ExternalGeneFlightSource(gene, ext);
                        Register(adapter);
                    }
                }
            }
        }

        public bool IsRegistered(IFlightSource source)
        {
            return sources.Contains(source);
        }

        public void Unregister(IFlightSource source)
        {
            sources.Remove(source);
        }

        public bool IsSuppressed(IFlightSource source)
        {
            return sources.Any(s => s.Priority > source.Priority && s.CanFly && s != source);
        }

        public IFlightSource GetActiveSource()
        {
            return sources.Where(s => s.IsActive).OrderByDescending(s => s.Priority).FirstOrDefault();
        }

        public bool ShouldHideBiologicalWings()
        {
            // If any source that can fly is an Implant, hide biological wings (as they replace them)
            return sources.Any(s => s.CanFly && s.SourceType == FlightSourceType.Implant);
        }
        
        public void Clear()
        {
            sources.Clear();
        }
    }
}
