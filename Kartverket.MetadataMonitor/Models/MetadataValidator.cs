using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Arkitektum.GIS.Lib.SerializeUtil;
using www.opengis.net;

namespace Kartverket.MetadataMonitor.Models
{
    public class MetadataValidator
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string ContentTypeXml = "application/xml";
        private readonly HttpRequestExecutor _httpRequestExecutor;

        private MetadataValidator(HttpRequestExecutor httpRequestExecutor)
        {
            _httpRequestExecutor = httpRequestExecutor;
        }

        public MetadataValidator() : this(new HttpRequestExecutor()) { }


        public MetadataEntry RetrieveAndValidate(string uuid)
        {
            MetadataEntry metadataEntry = null;
            try
            {
                var getCswRecordRequest = CreateGetCswRecordRequest(uuid);
                Log.Info("Henter metadata for uuid=" + uuid + " fra GeoNorge.");
                string cswRecordResponse = _httpRequestExecutor.PostRequest(Constants.EndpointUrlGeoNorgeCsw,
                                                                            ContentTypeXml, ContentTypeXml,
                                                                            getCswRecordRequest);
                
                /* Quick and dirty hacks to fix exceptions in serialization due to invalid xml */

                Regex fixWrongDecimalInRealElements = new Regex("<gco:Real>([0-9]+),([0-9]+)</gco:Real>");

                var fixedResponse = cswRecordResponse.Replace("<gco:Boolean />", "<gco:Boolean>false</gco:Boolean>")
                                                     .Replace("<gco:Real />", "<gco:Real>0.0</gco:Real>")
                                                     .Replace("<gco:DateTime />", "")
                                                     .Replace("<gmd:MD_TopicCategoryCode />", "");

                var rawXmlProcessed = fixWrongDecimalInRealElements.Replace(fixedResponse, "<gco:Real>$1.$2</gco:Real>");

                GetRecordByIdResponseType getRecordResponse = SerializeUtil.DeserializeFromString<GetRecordByIdResponseType>(rawXmlProcessed);
                MD_Metadata_Type metadata = getRecordResponse.Items[0] as MD_Metadata_Type;

                metadataEntry = ParseCswRecordResponse(uuid, metadata);
                ValidationResult validationResult;
                if (metadataEntry.ResourceType == "unknown")
                {
                    validationResult = new ValidationResult
                        {
                            Messages = "Unknown resource type, please check value of hierarchyLevel element.",
                            Result = -1,
                            Timestamp = DateTime.Now
                        };
                }
                else
                {
                    if (metadataEntry.InspireResource)
                    {
                        Log.Info("Validating metadata with INSPIRE-validator.");
                        validationResult = new InspireValidator(_httpRequestExecutor).Validate(rawXmlProcessed);
                    }
                    else
                    {
                        Log.Info("Validating metadata with Norge Digitalt-validator.");
                        validationResult = new NorgeDigitaltValidator().Validate(metadataEntry, metadata, rawXmlProcessed);
                    }
                }
                Log.Info("Validation result: " + validationResult.Messages);
                metadataEntry.ValidationResults.Add(validationResult);
            }
            catch (Exception e)
            {
                metadataEntry = ParseCswRecordResponse(uuid, null);

                string message = e.Message;
                if (e.InnerException != null)
                    message += e.InnerException.Message;

                metadataEntry.ValidationResults.Add(new ValidationResult { Messages="Exception during validation: " + message, Result = -1, Timestamp = DateTime.Now });
                Log.Error("Exception occured for uuid=" + uuid + ", not validated. " + message);
            }
            return metadataEntry;
        }

        private MetadataEntry ParseCswRecordResponse(string uuid, MD_Metadata_Type metadata)
        {
            var title = "unknown";
            var resourceType = "unknown";
            var organization = "unknown";
            var inspireResource = false;
            string purpose = null;
            string abstractText = null;

            StringBuilder contactInformation = new StringBuilder();

            List<string> keywords = new List<string>();

            if (metadata != null)
            {
                if (metadata.hierarchyLevel != null && metadata.hierarchyLevel[0] != null)
                {
                    resourceType = metadata.hierarchyLevel[0].MD_ScopeCode.codeListValue;
                }

                var dataIdentification = metadata.identificationInfo[0].AbstractMD_Identification;
                if (dataIdentification != null)
                {
                    title = dataIdentification.citation.CI_Citation.title.CharacterString;

                    if (dataIdentification.pointOfContact != null
                        && dataIdentification.pointOfContact[0] != null
                        && dataIdentification.pointOfContact[0].CI_ResponsibleParty != null)
                    {
                        CI_ResponsibleParty_Type responsibleParty = dataIdentification.pointOfContact[0].CI_ResponsibleParty;
                        
                        if (responsibleParty.organisationName != null && !string.IsNullOrWhiteSpace(responsibleParty.organisationName.CharacterString))
                            organization = responsibleParty.organisationName.CharacterString;
                        
                        if (responsibleParty.individualName != null &&
                            !string.IsNullOrWhiteSpace(responsibleParty.individualName.CharacterString))
                        {
                            contactInformation.Append(responsibleParty.individualName.CharacterString);
                        }

                        if (responsibleParty.contactInfo != null && responsibleParty.contactInfo.CI_Contact != null)
                        {
                            var contact = responsibleParty.contactInfo.CI_Contact;
                            

                            if (contact.address != null && contact.address.CI_Address != null)
                            {
                                var address = contact.address.CI_Address;
                                if (address.electronicMailAddress != null && address.electronicMailAddress[0] != null
                                    && !string.IsNullOrWhiteSpace(address.electronicMailAddress[0].CharacterString))
                                {
                                    contactInformation.Append(" - ").Append(address.electronicMailAddress[0].CharacterString).Append("\n");

                                }
                                else
                                {
                                    contactInformation.Append("\n");
                                }


                                if (address.deliveryPoint != null && address.deliveryPoint[0] != null &&
                                    !string.IsNullOrWhiteSpace(address.deliveryPoint[0].CharacterString))
                                {
                                    contactInformation.Append(address.deliveryPoint[0].CharacterString).Append(", ");
                                }

                                if (address.postalCode != null && !string.IsNullOrWhiteSpace(address.postalCode.CharacterString))
                                {
                                    contactInformation.Append(address.postalCode.CharacterString).Append(" ");
                                }

                                if (address.city != null && !string.IsNullOrWhiteSpace(address.city.CharacterString))
                                {
                                    contactInformation.Append(address.city.CharacterString);
                                }
                            }
                            


                            if (contact.phone != null && contact.phone.CI_Telephone != null
                                && contact.phone.CI_Telephone.voice != null && contact.phone.CI_Telephone.voice[0] != null
                                && !string.IsNullOrWhiteSpace(contact.phone.CI_Telephone.voice[0].CharacterString))
                            {
                                contactInformation.Append("\nTlf: ").Append(contact.phone.CI_Telephone.voice[0].CharacterString);
                            }
                        }


                    }


                    // collect keywords
                    if (dataIdentification.descriptiveKeywords != null)
                    {
                        foreach (var descriptiveKeyword in dataIdentification.descriptiveKeywords)
                        {
                            if (descriptiveKeyword.MD_Keywords != null && descriptiveKeyword.MD_Keywords.keyword != null)
                            {
                                foreach (var singleKeyword in descriptiveKeyword.MD_Keywords.keyword)
                                {
                                    if (singleKeyword.CharacterString != null)
                                    {
                                        keywords.Add(singleKeyword.CharacterString);

                                        /** old way of determine inspire or norge digitalt
                                        if (singleKeyword.CharacterString.Equals("annet", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            inspireResource = false;
                                        }
                                         **/
                                    }
                                }
                            }
                        }
                    }


                    inspireResource = IsInspireResource(metadata);
 
                    // collect purpose

                    if (dataIdentification.purpose != null && !string.IsNullOrWhiteSpace(dataIdentification.purpose.CharacterString))
                    {
                        purpose = dataIdentification.purpose.CharacterString;
                    }


                    // collect abstract

                    if (dataIdentification.@abstract != null && !string.IsNullOrWhiteSpace(dataIdentification.@abstract.CharacterString))
                    {
                        abstractText = dataIdentification.@abstract.CharacterString;
                    }
                }
    

                
            }

            if (inspireResource)
            {
                inspireResource = IsApplicableForInspireValidation(resourceType);
            }

            return new MetadataEntry()
                {
                    Uuid = uuid,
                    Title = title, 
                    ResourceType = resourceType,
                    ResponsibleOrganization = organization,
                    InspireResource = inspireResource,
                    ValidationResults = new List<ValidationResult>(),
                    ContactInformation = contactInformation.ToString(),
                    Keywords = string.Join(", ", keywords),
                    Purpose = purpose,
                    Abstract = abstractText
                };
        }

        private bool IsInspireResource(MD_Metadata_Type metadata)
        {
            bool isInspireResource = false;

            if (metadata.dataQualityInfo != null && metadata.dataQualityInfo[0] != null && metadata.dataQualityInfo[0].DQ_DataQuality != null
                && metadata.dataQualityInfo[0].DQ_DataQuality.report != null && metadata.dataQualityInfo[0].DQ_DataQuality.report[0] != null
                && metadata.dataQualityInfo[0].DQ_DataQuality.report[0].AbstractDQ_Element != null
                && metadata.dataQualityInfo[0].DQ_DataQuality.report[0].AbstractDQ_Element.result != null
                && metadata.dataQualityInfo[0].DQ_DataQuality.report[0].AbstractDQ_Element.result[0] != null
                && metadata.dataQualityInfo[0].DQ_DataQuality.report[0].AbstractDQ_Element.result[0].AbstractDQ_Result != null)
            {
                var result = metadata.dataQualityInfo[0].DQ_DataQuality.report[0].AbstractDQ_Element.result[0].AbstractDQ_Result as DQ_ConformanceResult_Type;

                if (result != null && result.specification != null && result.specification.CI_Citation != null
                    && result.specification.CI_Citation.title != null
                    && result.specification.CI_Citation.title.CharacterString != null
                    && result.specification.CI_Citation.title.CharacterString.ToUpper().Contains("COMMISSION REGULATION (EU)"))
                {
                    isInspireResource = true;
                }

            }
            return isInspireResource;
        }

        private bool IsApplicableForInspireValidation(string resourceType)
        {
            // ReSharper disable ReplaceWithSingleAssignment.False
            bool result = false;
            if (resourceType != null && (resourceType == "dataset" || resourceType == "service" || resourceType == "series"))
            {
                result = true;
            }
            // ReSharper restore ReplaceWithSingleAssignment.False
            return result;
        }

        private static string CreateGetCswRecordRequest(string uuid)
        {
            GetRecordByIdType getRecordbyId = new GetRecordByIdType();
            getRecordbyId.service = "CSW";
            getRecordbyId.version = "2.0.2";
            getRecordbyId.outputSchema = "csw:IsoRecord";
            getRecordbyId.Id = new[] { uuid };
            getRecordbyId.ElementSetName = new ElementSetNameType { Value = ElementSetType.full };

            return SerializeUtil.SerializeToString(getRecordbyId);
        }
        
        


    }
}