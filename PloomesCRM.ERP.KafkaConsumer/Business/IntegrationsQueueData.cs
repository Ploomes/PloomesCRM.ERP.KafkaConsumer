using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers;
using PloomesCRMCallbackHub2.Queue.DLL.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PloomesCRMCallbackHub2.Queue.DLL.Business
{
    public class IntegrationsQueueData : IIntegrationsQueueData
    {
        private readonly IDataAccess _dataAccess;

        public IntegrationsQueueData(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }



        private static Integration_Facebook_Queue AddToObject(DbDataReader dataReader)
        {
            Integration_Facebook_Queue integration_Facebook_Queue = new();
            integration_Facebook_Queue.Id = (int)dataReader["Id"];
            integration_Facebook_Queue.Body = (string)dataReader["Body"];

            return integration_Facebook_Queue;
        }

        public async Task SendQueueToCallbackAsync()
        {

            foreach (Integration_Facebook_Queue integration_Facebook_Queues in listIntegration_Facebook_Queues)
            {
                //var data = JsonSerializer.Serialize(integration_Facebook_Queues,);
                var data = new StringContent(JsonSerializer.Serialize(integration_Facebook_Queues),
                    System.Text.Encoding.UTF8, "application/json");

                //        await _dataAccess.SendObjectToCallBackHub2Async($"https://facebookleadsads.ploomes.com/facebookleadsads/executequeue", data, string.Empty);
                //_dataAccess.SendObjectToCallBackHub2Async($"https://facebookleadsads.ploomes.com/facebookleadsads/executequeue", data).Forget();
                // WebRequest.Request($"https://facebookleadsads.ploomes.com/facebookleadsads/executequeue", "POST", JObject.FromObject(integration_Facebook_Queues).ToString());
            }
        }

        public void SaveQueue(JObject body)
        {
            string post =
                @$"INSERT INTO Integration_Facebook_Queue (Body, [Hour], ErrorTime, ExecutedWithSuccess, AccountKey, FormIsMapped, AccountIsActive, ExistLeadGen) 
                                    VALUES ('{body}', DATEADD(hour, -3, getdate()), null, 0, null, 1, 1, 1)";

            _dataAccess.SaveQueue(post);
        }
    }
}