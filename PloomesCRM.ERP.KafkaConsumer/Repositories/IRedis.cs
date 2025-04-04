using System;
using System.Threading.Tasks;
using PloomesCRMCallbackHub2.Queue.DLL.Business;
using PloomesCRMCallbackHub2.Queue.DLL.Entities;

namespace PloomesCRMCallbackHub2.Queue.DLL.Repositories;

public interface IRedis
{
    Task<bool> CheckIfRequestForThisItemWasAlreadyExecutedAsync(string key);
    Task RemoveKeyAsync(GenericIntegrationObject integrationObject);

    #region Neppo

    Task WaitAndAcquireSemaphoreAsync(string semaphoreKey, int maxCount, TimeSpan expiry);

    #endregion
}