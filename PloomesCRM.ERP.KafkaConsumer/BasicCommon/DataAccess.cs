using Microsoft.Extensions.Options;
using PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers;
using PloomesCRMCallbackHub2.Queue.DLL.Business;
using PloomesCRMCallbackHub2.Queue.DLL.Repositories;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PloomesCRMCallbackHub2.Queue.DLL.Entities;
using System.Text;

namespace PloomesCRMCallbackHub2.Queue.DLL.BasicCommon
{
    public class DataAccess : IDataAccess
    {
        #region Ctor and Properties

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRedis _redis;
        //private readonly SemaphoreSlim _semaphoreSlim = new(12);


        public DataAccess(IHttpClientFactory httpClientFactory, IOptions<PloomesSettings> options, IRedis redis)
        {
            _httpClientFactory = httpClientFactory;
            _redis = redis;
        }

        #endregion

        #region Logs, datadog etc

        public async Task<string> SendLogToDataDogAsync(DatadogLog log)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add(HotSettings.HeaderDataDogApiKey, HotSettings.HeaderDataDogApiValue);
            var data = new StringContent(System.Text.Json.JsonSerializer.Serialize(log), System.Text.Encoding.UTF8,
                "application/json");
            var response = await httpClient.PostAsync(HotSettings.DataDogPostUrl, data);
            if (response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return $"Code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {content}";
        }

        #endregion

        #region Send Data to CallbackHub2

        public async Task<bool> SendObjectToCallBackHub2Async(GenericIntegrationObject objToSend, bool delay = false)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var body = new StringContent(objToSend.Requestbody.ToString(), Encoding.UTF8, "application/json");
            try
            {
                // await _semaphoreSlim.WaitAsync();

                if (objToSend.IntegrationsType == IntegrationsType.Neppo && objToSend.Requestbody.ContainsKey("Persons"))
                {
                    string semaphoreKey = $"{objToSend.AccountKey}:sync:persons:semaphore";
                    int maxCount = 1;
                    TimeSpan expiry = TimeSpan.FromMinutes(5);
                    await _redis.WaitAndAcquireSemaphoreAsync(semaphoreKey, maxCount, expiry);
                }

                var response = await httpClient.PostAsync(objToSend.Url, body);

                if (response.IsSuccessStatusCode)
                {
                    if (DateTime.UtcNow - objToSend.ReceveidDate < TimeSpan.FromMinutes(35) || WorkingHour() is false)
                    {
                        LogM.Info(
                            $"{objToSend.AccountKey}.{objToSend.IntegrationId} - Message received at: {objToSend.ReceveidDate} sent to {objToSend.Url}. Data:\n{objToSend.Requestbody}.In Delayed Dictionary = {delay}",
                            DateTime.UtcNow, objToSend.AccountId ?? 0, 0);
                    }
                    else
                    {
                        LogM.Error(
                            $"DELAY TO SEND MESSAGE {(DateTime.UtcNow - objToSend.ReceveidDate).TotalMinutes} MINUTES. {objToSend.AccountKey}.{objToSend.IntegrationId} - Message received at: {objToSend.ReceveidDate} sent to {objToSend.Url}. Data:\n{objToSend.Requestbody}.In Delayed Dictionary = {delay}",
                            DateTime.UtcNow, null, objToSend.AccountId ?? 0, 0);
                    }

                    //                    _semaphoreSlim.Release();
                    //    await RemoveKeyFromRedisAsync(objToSend);

                    return true;
                }

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
                {
                    //Console.WriteLine($"\n\n BAD REQUEST\n\n\n\n");

                    var content = await response.Content.ReadAsStringAsync();
                    LogM.Warn(
                        $"Code:{objToSend.AccountKey}.{objToSend.IntegrationId} {objToSend.Url} Data:\n{objToSend.Requestbody}{response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {content}",
                        DateTime.UtcNow, 0, 0, objToSend.AccountKey);
                    //_semaphoreSlim.Release();

                    return false;
                }

                // Console.WriteLine($"\n\n RETRY\n\n\n\n");

                await Task.Delay(1000); // Time  for retry
                var responseOnRetry = await httpClient.PostAsync(objToSend.Url, body);

                if (responseOnRetry.IsSuccessStatusCode)
                {
                    if (DateTime.UtcNow - objToSend.ReceveidDate < TimeSpan.FromMinutes(35) || WorkingHour() is false)
                    {
                        LogM.Info(
                            $"RETRY --> {objToSend.AccountKey}.{objToSend.IntegrationId} - Message received at: {objToSend.ReceveidDate} sent to {objToSend.Url}. Data:\n{objToSend.Requestbody}",
                            DateTime.UtcNow, 0, 0, objToSend.AccountKey);
                    }
                    else
                    {
                        LogM.Error(
                            $"RETRY --> DELAY TO SEND MESSAGE {(DateTime.UtcNow - objToSend.ReceveidDate).Minutes} MINUTES. {objToSend.AccountKey}.{objToSend.IntegrationId} - Message received at: {objToSend.ReceveidDate} sent to {objToSend.Url}. Data:\n{objToSend.Requestbody}",
                            DateTime.UtcNow, null, 0, 0, objToSend.AccountKey);
                    }

                    // await RemoveKeyFromRedisAsync(objToSend);

                    return true;
                }

                var retryContent = await responseOnRetry.Content.ReadAsStringAsync();
                LogM.Error(
                    $"Code:{objToSend.AccountKey}.{objToSend.IntegrationId} {objToSend.Url} Data:\n{objToSend.Requestbody}{responseOnRetry.StatusCode}, Reason: {responseOnRetry.ReasonPhrase}, Content: {retryContent}",
                    DateTime.UtcNow, null, 0, 0, objToSend.AccountKey);

                return false;
            }
            catch (Exception)
            {
                try
                {
                    await Task.Delay(3000); // Time  for retry
                    var responseOnRetry = await httpClient.PostAsync(objToSend.Url, body);

                    if (responseOnRetry.IsSuccessStatusCode)
                    {
                        LogM.Info(
                            $"{objToSend.AccountKey}.{objToSend.IntegrationId} - Message received at: {objToSend.ReceveidDate} sent to {objToSend.Url}. Data:\n{objToSend.Requestbody}",
                            DateTime.UtcNow, 0, 0, objToSend.AccountKey);
                        //   await RemoveKeyFromRedisAsync(objToSend);

                        return true;
                    }

                    var content = await responseOnRetry.Content.ReadAsStringAsync();
                    LogM.Warn(
                        $"Code:{objToSend.AccountKey}.{objToSend.IntegrationId} {objToSend.Url}  Data:\n{objToSend.Requestbody}{responseOnRetry.StatusCode}, Reason: {responseOnRetry.ReasonPhrase}, Content: {content}",
                        DateTime.UtcNow, 0, 0, objToSend.AccountKey);
                    //   _semaphoreSlim.Release();
                    return false;
                }
                catch (Exception exception)
                {
                    LogM.Error(
                        $"{objToSend.AccountKey}.{objToSend.IntegrationId}Error to Send message {exception.Message}Data:\n{objToSend.Requestbody} {objToSend.Url}",
                        DateTime.UtcNow, exception.StackTrace, 0, 0, objToSend.AccountKey);
                    //  _semaphoreSlim.Release();
                    return false;
                }
            }
        }

        private Task RemoveKeyFromRedisAsync(GenericIntegrationObject integrationObject)
        {
            return _redis.RemoveKeyAsync(integrationObject);
        }

        #endregion

        private static bool WorkingHour()
        {
            if ((DateTime.Now.DayOfWeek == DayOfWeek.Saturday) || (DateTime.Now.DayOfWeek == DayOfWeek.Sunday))
            {
                return false;
            }

            if ((DateTime.Now.ToLocalTime().DayOfWeek == DayOfWeek.Sunday) || (DateTime.Now.ToLocalTime().DayOfWeek == DayOfWeek.Saturday))
            {
                return false;
            }

            switch (DateTime.Now.ToLocalTime().Hour)
            {
                //From  06:00 to 19:00 Local, or 09:00 to 22:00 UTC
                case < 6:
                case > 22:
                    return false;
                default:
                    return true;
            }
        }

        public Task<bool> CheckIfRequestForThisItemWasAlreadyExecutedAsync(string key)
        {
            return _redis.CheckIfRequestForThisItemWasAlreadyExecutedAsync(key);
        }
    }
}