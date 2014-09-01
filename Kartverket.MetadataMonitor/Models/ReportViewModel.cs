using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Kartverket.MetadataMonitor.Models
{
    public class ReportViewModel
    {
        public string Organization { get; set; }
        public string ResourceType { get; set; }
        public bool? Inspire { get; set; }

        public List<MetadataEntry> MetadataEntries { get; set; }

        public List<int> Fields { get; set; } 
    }
}