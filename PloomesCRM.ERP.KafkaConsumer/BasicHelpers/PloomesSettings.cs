using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers
{
    public class PloomesSettings
    {
        public const string SectionName = "PloomesSecrets";
        #region Basic API info
        public string SecretsFile { get; set; } //"Id" of the Secres file
        public string ClusterName { get; set; } //k8s cluster: ploomes-dev, ploomes-prd-2, ploomes-we-1
        #endregion Basic API info               

        #region DataDog Log Info
        public string DDService { get; set; }
        public string DDTags { get; set; }

        public string DataDogPostUrl { get; set; }
        public string HeaderDataDogApiKey { get; set; }
        public string HeaderDataDogApiValue { get; set; }
        public string HeaderDataDogAppKey { get; set; }
        public string HeaderDataDogAppValue { get; set; }
        #endregion DataDog Log Info        

        #region Conection Strings and Info
        public string ConnectionString { get; set; }

        #endregion Conection Strings and Info

        #region CallbackHub2 Endpoints
        public string OmiePloomesBaseEndpoint { get; set; }
        public string FacebookPloomesBaseEndpoint { get; set; }

        #endregion
        #region Kafka

        public string KafkaTopic { get; set; }
        public string KafkaHost { get; set; }
        public string HeaderAdminKey { get; set; }
        public string HeaderAdminValue { get; set; }

        #endregion

        #region Redis
        public string RedisConnectionString { get; set; }
        public string SendGridApiKey { get; set; }
        public int KafkaPartition { get; set; }

        #endregion

        #region Neppo

        public int NeppoSemaphoreRetryDelaySeconds { get; set; }

        #endregion
    }

}
