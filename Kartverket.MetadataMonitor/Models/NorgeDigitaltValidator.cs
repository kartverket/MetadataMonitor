using www.opengis.net;

namespace Kartverket.MetadataMonitor.Models
{
    public class NorgeDigitaltValidator
    {
        public ValidationResult Validate(MetadataEntry metadataEntry, MD_Metadata_Type metadata, string rawXmlProcessed)
        {
            ValidationResult validationResult = new ValidationResult();
            
            if (metadataEntry.HasResourceType("software"))
            {
                validationResult = HasDistributionUrl(metadata, validationResult);
            }
            else
            {
                bool allowSpatialDataThemeError = true;
                bool allowConformityError = true;
                validationResult = new InspireValidator().Validate(rawXmlProcessed, allowSpatialDataThemeError, allowConformityError);
            }

            return validationResult;
        }

        private static ValidationResult HasDistributionUrl(MD_Metadata_Type metadata, ValidationResult validationResult)
        {
            if (metadata.distributionInfo != null
                && metadata.distributionInfo.MD_Distribution != null
                && metadata.distributionInfo.MD_Distribution.transferOptions != null
                && metadata.distributionInfo.MD_Distribution.transferOptions[0] != null
                && metadata.distributionInfo.MD_Distribution.transferOptions[0].MD_DigitalTransferOptions != null
                && metadata.distributionInfo.MD_Distribution.transferOptions[0].MD_DigitalTransferOptions.onLine != null
                && metadata.distributionInfo.MD_Distribution.transferOptions[0].MD_DigitalTransferOptions.onLine[0] != null
                &&
                metadata.distributionInfo.MD_Distribution.transferOptions[0].MD_DigitalTransferOptions.onLine[0]
                    .CI_OnlineResource != null
                &&
                metadata.distributionInfo.MD_Distribution.transferOptions[0].MD_DigitalTransferOptions.onLine[0]
                    .CI_OnlineResource.linkage != null
                &&
                metadata.distributionInfo.MD_Distribution.transferOptions[0].MD_DigitalTransferOptions.onLine[0]
                    .CI_OnlineResource.linkage.URL != null)
            {
                string url =
                    metadata.distributionInfo.MD_Distribution.transferOptions[0].MD_DigitalTransferOptions.onLine[0]
                        .CI_OnlineResource.linkage.URL;
                if (url.Trim().Length > 1)
                {
                    validationResult.Result = 1;
                }
                else
                {
                    validationResult.Result = 0;
                    validationResult.Messages = "Empty URL in distributionInfo.";
                }
            }
            else
            {
                validationResult.Result = 0;
                validationResult.Messages = "Missing URL in distributionInfo.";
            }

            return validationResult;
        }
    }
}