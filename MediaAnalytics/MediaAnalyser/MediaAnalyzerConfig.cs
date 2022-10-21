using Microsoft.Azure.Management.Media;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaAnalyzer
{
    public class MediaAnalyzerConfig
    {
        public string TokenType = "Bearer";
        public string MediaAnalyzerAccessToken { get; set; }
        public string ResourceGroup { get; set; }
        public Uri ArmEndpoint { get; set; }
        public string SubscriptionId { get; set; }
        public string AccountName { get; set; }
        public string StorageAccountName { get; set; }
        public string StorageContainerName { get; set; }
        public string StorageAccountKey { get; set; }

        public MediaAnalyzerConfig(string mediaAnalyzerAccessToken,
            string resourceGroup,
            string armEndpoint,
            string subscriptionId,
            string accountName,
            string storageAccountName,
            string storageAccountKey,
            string? storageContainerName = null)
        {
            Initializer(mediaAnalyzerAccessToken,
                resourceGroup,
                armEndpoint,
                subscriptionId,
                accountName,
                storageAccountName,
                storageAccountKey,
                storageContainerName);
        }
        private void Initializer(string mediaAnalyzerAccessToken,
            string resourceGroup,
            string armEndpoint,
            string subscriptionId,
            string accountName,
            string storageAccountName,
            string storageAccountKey,
            string? storageContainerName = null)
        {
            if (string.IsNullOrEmpty(mediaAnalyzerAccessToken) | string.IsNullOrWhiteSpace(mediaAnalyzerAccessToken))
            {
                throw new ArgumentNullException(nameof(mediaAnalyzerAccessToken));

            }
            else
            {
                MediaAnalyzerAccessToken = mediaAnalyzerAccessToken;
            }

            if (string.IsNullOrEmpty(resourceGroup) | string.IsNullOrWhiteSpace(resourceGroup))
            {
                throw new ArgumentNullException(nameof(resourceGroup));

            }
            else
            {
                ResourceGroup = resourceGroup;
            }

            if (string.IsNullOrEmpty(armEndpoint) | string.IsNullOrWhiteSpace(armEndpoint))
            {
                throw new ArgumentNullException(nameof(armEndpoint));

            }
            else
            {
                ArmEndpoint = new Uri(armEndpoint);
            }

            if (string.IsNullOrEmpty(subscriptionId) | string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));

            }
            else
            {
                SubscriptionId = subscriptionId;
            }

            if (string.IsNullOrEmpty(accountName) | string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentNullException(nameof(accountName));

            }
            else
            {
                AccountName = accountName;
            }

            if (string.IsNullOrEmpty(storageAccountName) | string.IsNullOrWhiteSpace(storageAccountName))
            {
                throw new ArgumentNullException(nameof(storageAccountName));

            }
            else
            {
                StorageAccountName = storageAccountName;
            }

            if (storageContainerName == null)
            {
                StorageContainerName = "";

            }
            else
            {
                StorageContainerName = storageContainerName;
            }

            if (string.IsNullOrEmpty(storageAccountKey) | string.IsNullOrWhiteSpace(storageAccountKey))
            {
                throw new ArgumentNullException(nameof(storageAccountKey));

            }
            else
            {
                StorageAccountKey = storageAccountKey;
            }

        }

        internal IAzureMediaServicesClient StartConfig()
        {
            ServiceClientCredentials credentials = new TokenCredentials(MediaAnalyzerAccessToken, TokenType);

            return new AzureMediaServicesClient(ArmEndpoint, credentials)
            {
                SubscriptionId = SubscriptionId,

            };
        }

    }
}
