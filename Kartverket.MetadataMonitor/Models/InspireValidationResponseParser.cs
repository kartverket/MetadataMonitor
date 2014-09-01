using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Kartverket.MetadataMonitor.Models
{
    public class InspireValidationResponseParser
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static readonly XNamespace NsCommon = "http://inspire.ec.europa.eu/schemas/common/1.0";
        public static readonly XNamespace NsGeo = "http://inspire.ec.europa.eu/schemas/geoportal/1.0";
        public static readonly XNamespace NsRdsi = "http://inspire.ec.europa.eu/schemas/rdsi/1.0";

        private readonly XDocument _inspireValidationResponse;

        public InspireValidationResponseParser(XDocument inspireValidationResponse)
        {
            _inspireValidationResponse = inspireValidationResponse;
        }

        internal ValidationResult ParseValidationResponseWithCompletenessIndicator()
        {
            var errors = GetErrors(_inspireValidationResponse);
            var validationResult = new ValidationResult();

            validationResult.Result = ComputeValidationResultFromCompletenessIndicator();
            validationResult.Messages = String.Join("\r\n", errors);

            return validationResult;
        }

        private int ComputeValidationResultFromCompletenessIndicator()
        {
            int result = 0;
            XElement element = _inspireValidationResponse.Descendants(NsGeo + "CompletenessIndicator").FirstOrDefault();
            if (element != null)
            {
                double completenessIndicator = 0.0;
                double.TryParse(element.Value, NumberStyles.AllowDecimalPoint ,System.Globalization.CultureInfo.CreateSpecificCulture("en-US"), out completenessIndicator);
                Log.Debug("CompletnessIndicator: " + completenessIndicator);
                if (((int)completenessIndicator) == 100)
                {
                    result = 1;
                }    
            }
            return result;
        }

        public ValidationResult ParseValidationResponse(bool allowSpatialDataThemeError, bool allowConformityError)
        {
            var errors = GetErrors(_inspireValidationResponse);

            var validationResult = new ValidationResult();
            validationResult.Result = ComputeValidationResult(errors, allowSpatialDataThemeError, allowConformityError);


            if (!validationResult.IsOk())
                validationResult.Messages = String.Join("\r\n", errors);

            return validationResult;
        }

        private List<string> GetErrors(XDocument xmlDoc)
        {
            List<string> errors = new List<string>();
            var geoPortalExceptions = xmlDoc.Descendants(NsGeo + "ValidationError").Descendants(NsGeo + "GeoportalExceptionMessage");
            foreach (var element in geoPortalExceptions)
            {
                var messageElement = element.Element(NsGeo + "Message");
                if (messageElement != null)
                {
                    var messageValue = messageElement.Value;
                    if (!String.IsNullOrEmpty(messageValue))
                    {
                        errors.Add(messageValue);
                    }
                }
            }
            return errors;
        }
        
        

        private int ComputeValidationResult(List<string> errors, bool allowSpatialDataThemeError, bool allowConformityError)
        {
            if (errors.Any() && (allowSpatialDataThemeError || allowConformityError))
            {
                for (var i = errors.Count - 1; i >= 0; i--) // loop backwards to allow removal of items while iterating
                {
                    if (allowSpatialDataThemeError && errors[i].Contains("Inspire Spatial Data Theme\" is missing"))
                    {
                        errors.RemoveAt(i);
                    } else if (allowConformityError && errors[i].Contains("Conformity\" is missing")) {
                        errors.RemoveAt(i);
                    }
                }
            }
            return !errors.Any() ? 1 : 0;
        }


        
    }
}
