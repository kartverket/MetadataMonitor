using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Kartverket.MetadataMonitor.Models;

namespace Kartverket.MetadataMonitor.Controllers
{
    public class ReportController : Controller
    {
        private readonly MetadataRepository _metadataRepository;

        public ReportController() :this(new MetadataRepository()) { }

        private ReportController(MetadataRepository metadataRepository)
        {
            _metadataRepository = metadataRepository;
        }


        public ActionResult Index(string organization, string resource, bool? inspire, List<int> fields)
        {

            ViewBag.Organizations = new SelectList(_metadataRepository.GetAvailableOrganizations(), organization);

            List<string> resourceTypes = new List<string>()
                {
                    "service", "dataset", "series", "software", "unknown"
                };
            ViewBag.ResourceTypes = new SelectList(resourceTypes, resource);

            IDictionary<bool, string> inspireOptions = new Dictionary<bool, string>
                {
                    {true, "Inspire"},
                    {false, "Norge Digitalt"}
                };
            ViewBag.InspireOptions = new SelectList(inspireOptions, "Key", "Value", inspire);

            IDictionary<int, string> availableFields = new Dictionary<int, string>
                {
                    {1, "Kontaktinformasjon"},
                    {2, "Sammendrag"},
                    {3, "Formål"},
                    {4, "Nøkkelord"}
                };
            ViewBag.AvailableFields = new MultiSelectList(availableFields, "Key", "Value", fields);

            List<MetadataEntry> metadataEntries = _metadataRepository.GetMetadataList(organization, resource, inspire);

            var model = new ReportViewModel()
            {
                Organization = organization,
                ResourceType = resource,
                Inspire = inspire,
                MetadataEntries = metadataEntries,
                Fields = fields
            };

            return View(model);
        }

    }
}
