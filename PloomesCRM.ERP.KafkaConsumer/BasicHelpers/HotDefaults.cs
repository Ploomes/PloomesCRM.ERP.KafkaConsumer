using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers
{
    public class HotDefaults
    {
        /// <summary>Time between requests od the same integrationKey. </summary>
        public static int NextAccountTimeToSend { get; } = 2;
        /// <summary>Time between Two timed logs: 5. </summary>
        public static int DeltaToNextLog { get; } = 5;
        /// <summary>Max number of requests sent to cb2 per queue round. </summary>
        public static int MaxNumerOfRequests { get; } = 100;
        /// <summary>Omie Entities interval requests in minutes. </summary>
        public static double OmieEntitiesRequestInterval { get; } = 1;
        public static int BadHeaderDelay { get; set; }
        public static string BadHeaderString { get; set; }
        public static int SecretErrorDelay { get; set; }
        public static string SecretErrorString { get; set; }
    }
}