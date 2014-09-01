using System.Text;
using System.Xml.Linq;
using System.Security.Cryptography;
using System;

namespace Kartverket.MetadataMonitor.Models
{
    public class InspireValidator
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly HttpRequestExecutor _httpRequestExecutor;

        public InspireValidator() : this(new HttpRequestExecutor())
        {
            
        }

        public InspireValidator(HttpRequestExecutor httpRequestExecutor)
        {
            _httpRequestExecutor = httpRequestExecutor;
        }

        public ValidationResult Validate(string rawXmlProcessed)
        {
            return Validate(rawXmlProcessed, false);
        }

        public ValidationResult Validate(string rawXmlProcessed, bool allowSpatialDataThemeError, bool allowConformityError = false)
        {
            string inspireValidationResponse = RunInspireValidation(rawXmlProcessed);
            
            //Log.Debug(inspireValidationResponse);

            XDocument xmlDoc = XDocument.Parse(inspireValidationResponse);
            InspireValidationResponseParser parser = new InspireValidationResponseParser(xmlDoc);

            return parser.ParseValidationResponseWithCompletenessIndicator();
        }

        private string RunInspireValidation(string data)
        {
            /* 
             * BAD HACK: Search and replace norwegian language code with english language code. 
             * INSPIRE does not validate metadata with norwegian language code.
             * This removes validation errors related to conformity and language code.
             */
            data = data.Replace(">nob</gco:CharacterString>", ">eng</gco:CharacterString>");
            data = data.Replace(">nor</gco:CharacterString>", ">eng</gco:CharacterString>");

            // formating request according to 
            // http://inspire-geoportal.ec.europa.eu/validator2/html/usingaswebservice.html

            string boundary = createHash(DateTime.Now.Ticks.ToString());

            string eol = "\r\n";
            StringBuilder builder = new StringBuilder();
            builder.Append("--").Append(boundary).Append(eol);
            builder.Append("Content-Disposition: form-data; name=\"resourceRepresentation\"").Append(eol).Append(eol);
            builder.Append(data);
            builder.Append(eol);
            builder.Append("--").Append(boundary).Append("--").Append(eol).Append(eol);

            string postData = builder.ToString();

            string contentType = "multipart/form-data; boundary=" + boundary;

            Log.Info("Sending metadata to INSPIRE validator.");
            string responseBody = _httpRequestExecutor.PostRequest(Constants.EndpointUrlInspire, "application/xml", contentType, postData);

            return responseBody;
        }

        private string createHash(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }
    }
}