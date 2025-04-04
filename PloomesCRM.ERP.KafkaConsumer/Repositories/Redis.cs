using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers;
using PloomesCRMCallbackHub2.Queue.DLL.Entities;
using StackExchange.Redis;

namespace PloomesCRMCallbackHub2.Queue.DLL.Repositories;

public class Redis : IRedis
{
    private readonly IDatabase _cache;
    private readonly int _delaySeconds;

    public Redis(IOptions<PloomesSettings> options)
    {
        try
        {
            var redis = ConnectionMultiplexer.Connect(options.Value.RedisConnectionString);
            _cache = redis.GetDatabase();
            _delaySeconds = options.Value.NeppoSemaphoreRetryDelaySeconds;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task<bool> CheckIfRequestForThisItemWasAlreadyExecutedAsync(string key)
    {
        if (_cache is null)
        {
            return true;
        }
        try
        {
            var resultFound = await _cache.StringGetAsync(key);
            if (resultFound == RedisValue.Null || string.IsNullOrEmpty(resultFound))
            {
                return true;
            }
            return false;

        }
        catch (Exception)
        {
            await Task.Delay(2000);
            var resultFoundOnRetry = await _cache.StringGetAsync(key);
            if (resultFoundOnRetry == RedisValue.Null || string.IsNullOrEmpty(resultFoundOnRetry))
            {
                return true;
            }
            return false;
        }
    }

    public async Task RemoveKeyAsync(GenericIntegrationObject integrationObject)
    {
        try
        {
            if (_cache is null)
            {
                return;
            }

            await _cache.SetRemoveAsync($"{integrationObject.IntegrationsType}.requests", $"{integrationObject.AccountKey}{integrationObject.IntegrationId}");
            await _cache.KeyDeleteAsync($"{integrationObject.AccountKey}{integrationObject.IntegrationId}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    #region Neppo

    public async Task WaitAndAcquireSemaphoreAsync(string semaphoreKey, int maxCount, TimeSpan expiry)
    {
        bool acquired = false;
        while (!acquired)
        {
            acquired = await AcquireSemaphoreAsync(semaphoreKey, maxCount, expiry);
            if (!acquired)
            {
                await Task.Delay(TimeSpan.FromSeconds(_delaySeconds));
            }
        }
    }

    private async Task<bool> AcquireSemaphoreAsync(string semaphoreKey, int maxCount, TimeSpan expiry)
    {
        if (_cache is null)
        {
            Console.WriteLine("Cache está nulo, semáforo não está ativo.");
            return true;
        }

        var currentCount = await _cache.StringIncrementAsync(semaphoreKey);

        if (currentCount > maxCount)
        {
            await _cache.StringDecrementAsync(semaphoreKey);
            return false;
        }

        await _cache.KeyExpireAsync(semaphoreKey, expiry);
        return true;
    }

    #endregion
}