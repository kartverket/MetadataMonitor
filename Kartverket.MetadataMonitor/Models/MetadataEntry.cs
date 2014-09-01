using System.Collections.Generic;
using System.Text;

namespace Kartverket.MetadataMonitor.Models
{
    public class MetadataEntry
    {
        public string Uuid { get; set; }
        public string Title { get; set; }
        public string ResponsibleOrganization { get; set; }
        public string ResourceType { get; set; }
        public bool InspireResource { get; set; }
        public List<ValidationResult> ValidationResults { get; set; }

        public string ContactInformation { get; set; }
        public string Keywords { get; set; }

        public string Abstract { get; set; }
        public string Purpose { get; set; }

        public string GetResultAsText()
        {
            string result = "unknown";
            if (ValidationResults != null && ValidationResults[0] != null)
            {
                var validationResult = ValidationResults[0];
                result = validationResult.GetResultAsText();
            }
            return result;
        }

        public string GetValidationResultMesssages()
        {
            StringBuilder builder = new StringBuilder();
            if (ValidationResults != null && ValidationResults[0] != null)
            {
                foreach (var result in ValidationResults)
                {
                    builder.Append(result.Messages);
                }
            }
            return builder.ToString();
        }

        public bool IsOk()
        {
            bool result = false;

            if (ValidationResults != null && ValidationResults[0] != null)
            {
                var validationResult = ValidationResults[0];
                result = validationResult.IsOk();
            }
            return result;
        }

        public bool isNotValidated()
        {
            bool result = true;

            if (ValidationResults != null && ValidationResults[0] != null)
            {
                var validationResult = ValidationResults[0];
                result = validationResult.IsNotValidated();
            }
            return result;
        }

        public bool IsFailed()
        {
            bool result = false;

            if (ValidationResults != null && ValidationResults[0] != null)
            {
                var validationResult = ValidationResults[0];
                result = validationResult.IsFailed();
            }
            return result;
        }

        public bool HasResourceType(string inputResourceType)
        {
            bool result = false;
            if (!string.IsNullOrWhiteSpace(ResourceType) && !string.IsNullOrWhiteSpace(inputResourceType))
            {
                result = ResourceType == inputResourceType;
            }
            return result;
        }
    }
}