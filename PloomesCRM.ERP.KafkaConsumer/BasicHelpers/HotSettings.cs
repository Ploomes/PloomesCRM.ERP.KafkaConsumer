using PloomesCRMCallbackHub2.Queue.DLL.BasicCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers
{
    public class HotSettings
    {
        #region Basic API info        
        private static string _secretsFile;
        private static string _clusterName;
        public static string SecretsFile { get => _secretsFile; }
        public static string ClusterName { get => _clusterName; }
        public static string HeaderAdminKey { get; private set; }
        public static string HeaderAdminValue { get; private set; }
        #endregion Basic API info               

        #region ConnectionString
        public static string ConnectionString { get => _connectionString; }
        private static string _connectionString;
        #endregion

        #region DataDog Log Info
        public static string DDService { get => _dDService; }
        public static string DDTags { get => _dDTags; }
        private static string _dDService;
        private static string _dDTags;

        public static string DataDogPostUrl { get => _dataDogPostUrl; }
        public static string HeaderDataDogApiKey { get => _headerDataDogApiKey; }
        public static string HeaderDataDogApiValue { get => _headerDataDogApiValue; }
        public static string HeaderDataDogAppKey { get => _headerDataDogAppKey; }
        public static string HeaderDataDogAppValue { get => _headerDataDogAppValue; }
        private static string _dataDogPostUrl;
        private static string _headerDataDogApiKey;
        private static string _headerDataDogApiValue;
        private static string _headerDataDogAppKey;
        private static string _headerDataDogAppValue;

        public static string K8sNode { get => _k8sNode; } // GET FROM ENV, NOT APPSETTINGS
        private static string _k8sNode;

        public static string DDHostName { get => _dDHostName; } //Build from other values
        private static string _dDHostName;
        #endregion DataDog Log Info

        #region CallbackHub2 Endpoints

        public static string OmiePloomesBaseEndpoint { get; private set; }
        public static string FacebookPloomesBaseEndpoint { get; private set; }

        #endregion

        #region Kafka
      
        public static string KafkaTopic { get; private set; }
      
        public static string KafkaHost { get; private set; }
        public static string SendGridApiKey { get; private set; }
        public static int KafkaPartition { get; private set; }

        #endregion

        public static void StartValues(PloomesSettings settings)
        {
            #region Basic API INFO            
            _secretsFile = settings.SecretsFile;
            _clusterName = settings.ClusterName;
            HeaderAdminKey = settings.HeaderAdminKey;
            HeaderAdminValue = settings.HeaderAdminValue;
            #endregion Basic API INFO

            #region Conection Strings and Info
            _connectionString = settings.ConnectionString;


            #endregion Conection Strings and Info

            #region DataDog Log Info            
            _dDService = settings.DDService;
            _dDTags = $"{settings.DDTags},version:{ApiInfo.DdVersion}";

            _dataDogPostUrl = settings.DataDogPostUrl;
            _headerDataDogApiKey = settings.HeaderDataDogApiKey;
            _headerDataDogApiValue = settings.HeaderDataDogApiValue;
            _headerDataDogAppKey = settings.HeaderDataDogAppKey;
            _headerDataDogAppValue = settings.HeaderDataDogAppValue;

            _k8sNode = Environment.GetEnvironmentVariable("NODE_NAME") ?? "unknown";
            _dDHostName = $"{_k8sNode}-{_clusterName}";
            #endregion DataDog Log Info

            #region CallbackHub2 Endpoints
            OmiePloomesBaseEndpoint = settings.OmiePloomesBaseEndpoint;
            FacebookPloomesBaseEndpoint = settings.FacebookPloomesBaseEndpoint;
            #endregion
            #region Kafka
         
            KafkaTopic = settings.KafkaTopic;
            KafkaHost = settings.KafkaHost;
            KafkaPartition = settings.KafkaPartition;
            #endregion
            SendGridApiKey = settings.SendGridApiKey;
        }

    }
}
