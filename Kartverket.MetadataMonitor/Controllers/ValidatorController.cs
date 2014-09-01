using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using Kartverket.MetadataMonitor.Models;

namespace Kartverket.MetadataMonitor.Controllers
{
    public class ValidatorController : Controller
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly MetadataRepository _metadataRepository;
        private readonly ValidatorService _validatorService;

        private ValidatorController(MetadataRepository metadataRepository, ValidatorService validatorService)
        {
            _metadataRepository = metadataRepository;
            _validatorService = validatorService;
        }

        public ValidatorController() : this(new MetadataRepository(), new ValidatorService()) { }

        public ActionResult Index(int? status, string organization, string resource, bool? inspire)
        {
            List<MetadataEntry> metadataEntries = _metadataRepository.GetMetadataListWithLatestValidationResult(status, organization, resource, inspire);

            var myTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

            var model = new ValidatorResultViewModel()
                {
                    Organization = organization,
                    Status = status,
                    ResourceType = resource,
                    Inspire = inspire,
                    MetadataEntries = metadataEntries,
                };

            ViewBag.Organizations = new SelectList(_metadataRepository.GetAvailableOrganizations(), organization);
            IDictionary<int, string> statusOptions = new Dictionary<int, string>
                {
                    {1, "OK"},
                    {0, "Feil"},
                    {-1, "Ikke validert"}
                };
            ViewBag.StatusOptions = new SelectList(statusOptions, "Key", "Value", status);

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

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public ActionResult RunValidate(string uuid, string organization, string resource, int? status, bool? inspire)
        {
            if (string.IsNullOrEmpty(uuid))
                uuid = "9d118d31-182c-495b-b7be-d819cc7444b1";

            
            MetadataEntry metadataEntry = _validatorService.ValidateMetadata(uuid);

            string message = "Validering gjennomført!";

            if (metadataEntry == null) {
                message = "Validering feilet pga en ukjent feil.";
            }
            else if (metadataEntry.isNotValidated())
            {
                message = "Validering feilet: " + metadataEntry.GetValidationResultMesssages();
            }
            
            TempData["message"] = message;

            return RedirectToAction("Index", new
                {
                    organization = organization, 
                    status = status, 
                    resource = resource,
                    inspire = inspire
                });
        }

        [HttpGet]
        public ActionResult ValidateAll()
        {
            Log.Info("Start validating all metadata.");            

            new Thread(() => new ValidatorService().ValidateAllMetadata(false)).Start();

            TempData["message"] = "Full validering startet!";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult DeactivateValidateAll()
        {
            Log.Info("Start validating all metadata, with deactivating.");

            new Thread(() => new ValidatorService().ValidateAllMetadata(true)).Start();

            TempData["message"] = "Full validering startet! - med deaktivering";

            return RedirectToAction("Index");
        }
       
    }
}
