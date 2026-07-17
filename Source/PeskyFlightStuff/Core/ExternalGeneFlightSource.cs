using System.Reflection;
using Verse;

namespace Pesky
{
    public class ExternalGeneFlightSource : IFlightSource
    {
        private Gene gene;
        private FlightSourceExtension ext;
        private FieldInfo flightEnabledField;

        public ExternalGeneFlightSource(Gene gene, FlightSourceExtension ext)
        {
            this.gene = gene;
            this.ext = ext;
            flightEnabledField = gene.GetType().GetField("flightEnabled", BindingFlags.Public | BindingFlags.Instance);
        }

        public int Priority => ext.priority;
        public FlightSourceType SourceType => ext.sourceType;
        public bool CanFly => true;
        public string SourceId => "ExternalGene_" + gene.def.defName;

        public bool IsActive
        {
            get
            {
                if (flightEnabledField != null)
                {
                    return (bool)flightEnabledField.GetValue(gene);
                }
                return false;
            }
            set
            {
                if (flightEnabledField != null)
                {
                    flightEnabledField.SetValue(gene, value);
                }
            }
        }
        
        public override bool Equals(object obj)
        {
            if (obj is ExternalGeneFlightSource other) return other.gene == this.gene;
            return false;
        }
        
        public override int GetHashCode() => gene.GetHashCode();
    }
}
