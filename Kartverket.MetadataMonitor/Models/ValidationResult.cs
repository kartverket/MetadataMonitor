using System;

namespace Kartverket.MetadataMonitor.Models
{
    public class ValidationResult
    {
        public int Result { get; set; }
        public DateTime Timestamp { get; set; }
        public string Messages { get; set; }

        public bool IsOk()
        {
            return Result == 1;
        }

        public bool IsNotValidated()
        {
            return Result == -1;
        }

        public bool IsFailed()
        {
            return Result == 0;
        }

        public string GetResultAsText()
        {
            switch (Result)
            {
                case 1:
                    return "OK";
                case 0:
                    return "Failed";
                default:
                    return "Not validated";
            }
        }
    }

}
