using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaAnalyzer
{
    public class MediaAnalyzerInput
    {
        public string InputFileUrl { get; set; }
        public string LanguageCode { get; set; }
        public Guid InputGuid { get; set; }
        public bool IsHttpJob { get; set; }
        public bool IsByteArray = false;
        public byte[] ByteArrayData { get; set; }
        public string ByteArrayName { get; set; }

        public MediaAnalyzerInput(string inputFileUrl, string languageCode, bool isHttpJob)
        {
            Initializer(inputFileUrl, languageCode);
            InputGuid = new Guid();
            IsHttpJob = isHttpJob;

        }
        public MediaAnalyzerInput(byte[] byteArrayData, string byteArrayName, string languageCode)
        {
            Initializer(byteArrayData, byteArrayName, languageCode);
            InputGuid = new Guid();
            IsHttpJob = false;
            IsByteArray = true;
        }
        private void Initializer(string inputFileUrl, string languageCode)
        {
            if (string.IsNullOrEmpty(inputFileUrl) | string.IsNullOrWhiteSpace(inputFileUrl))
            {
                throw new ArgumentException(nameof(inputFileUrl));
            }
            else
            {
                InputFileUrl = inputFileUrl;
            }
            if (string.IsNullOrEmpty(languageCode) | string.IsNullOrWhiteSpace(languageCode))
            {
                throw new ArgumentException(nameof(languageCode));
            }
            else
            {
                LanguageCode = languageCode;
            }
        }
        private void Initializer(byte[] byteArrayData, string byteArrayName, string languageCode)
        {
            if (byteArrayData == null)

            {
                throw new ArgumentNullException(nameof(byteArrayData));
            }
            else
            {
                ByteArrayData = byteArrayData;
            }
            if (string.IsNullOrEmpty(byteArrayName) | string.IsNullOrWhiteSpace(byteArrayName))
            {
                throw new ArgumentException(nameof(byteArrayData));
            }
            else
            {
                ByteArrayName = byteArrayName;
            }
            if (string.IsNullOrEmpty(languageCode) | string.IsNullOrWhiteSpace(languageCode))
            {
                throw new ArgumentException(nameof(languageCode));
            }
            else
            {
                LanguageCode = languageCode;
            }

        }
    }
}
