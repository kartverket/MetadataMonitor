using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Kartverket.MetadataMonitor.Models;

namespace Kartverket.MetadataMonitor.Controllers
{
    public class DashboardController : Controller
    {
        private readonly MetadataRepository _metadataRepository;

        private DashboardController(MetadataRepository metadataRepository)
        {
            _metadataRepository = metadataRepository;
        }

        public DashboardController() : this(new MetadataRepository()) { }

        public ActionResult Index()
        {
            var results = _metadataRepository.GetMetadataListWithLatestValidationResult(null, null, null, null);

            var totalResultCount = results.Count();
            var totalResultNotValidated = results.Count(n => n.isNotValidated());
            var totalResultOk = results.Count(n => n.IsOk());
            var totalResultFailed = totalResultCount - totalResultNotValidated - totalResultOk;

            var inspireResults = results.Where(n => n.InspireResource);
            var norgeDigitaltResults = results.Where(n => n.InspireResource == false);

            var model = new DashboardViewModel
                {
                    TotalResultCount = totalResultCount,
                    TotalResultOk = totalResultOk,
                    TotalResultFailed = totalResultFailed,
                    TotalNotValidated = totalResultNotValidated,
                    
                    InspireService = GetResultsForResourceType(inspireResults, "service"),
                    InspireDataset = GetResultsForResourceType(inspireResults, "dataset"),
                    InspireSeries = GetResultsForResourceType(inspireResults, "series"),
                    NdService = GetResultsForResourceType(norgeDigitaltResults, "service"),
                    NdDataset = GetResultsForResourceType(norgeDigitaltResults, "dataset"),
                    NdSeries = GetResultsForResourceType(norgeDigitaltResults, "series"),
                    NdSoftware = GetResultsForResourceType(norgeDigitaltResults, "software"),
                };
            return View(model);
        }
        
        private static Result GetResultsForResourceType(IEnumerable<MetadataEntry> results, string resourceType)
        {
            var allResourceResults = results.Where(n => n.ResourceType == resourceType);
            var resourceResults = new Result()
                {
                    Failed = allResourceResults.Count(n => !n.IsOk()),
                    Ok = allResourceResults.Count(n => n.IsOk()),
                    Unknown = allResourceResults.Count(n => n.isNotValidated())
                };
            return resourceResults;
        }
    }
}
