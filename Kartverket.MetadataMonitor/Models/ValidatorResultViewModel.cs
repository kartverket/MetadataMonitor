using System.Collections.Generic;

namespace Kartverket.MetadataMonitor.Models
{
    public class ValidatorResultViewModel
    {
        public string Organization { get; set; }
        public int? Status { get; set; }
        public string ResourceType { get; set; }

        public List<MetadataEntry> MetadataEntries { get; set; }

        public bool? Inspire { get; set; }
    }
}