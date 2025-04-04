using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using PloomesCRMCallbackHub2.Queue.DLL.BasicCommon;
using PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers;
using PloomesCRMCallbackHub2.Queue.DLL.Entities;

namespace PloomesCRMCallbackHub2.Queue.DLL.Business;

public class GenericObjects
{
    public readonly ConcurrentQueue<GenericIntegrationObject> Queue = new();
}

public class KafkaConsumer : BackgroundService
{
    #region Ctor and properties

    private int _counterOfObjects;
    private DateTime _lastInteraction = DateTime.UtcNow;
    private readonly ConcurrentDictionary<string, DateTime> _integrationIdControl = new();
    private readonly ConcurrentDictionary<string, GenericIntegrationObject> _objeToBeSentLater = new();
    private readonly IDataAccess _dataAccess;

    private readonly ConcurrentDictionary<string, GenericObjects> _usersKeyTimer = new();
    private readonly ConcurrentDictionary<string, DateTime> _accountKeyTimer = new();

    /// <summary> For graceful shutdown. </summary>
    private readonly IHostApplicationLifetime _lifeTime;

    public KafkaConsumer(IHostApplicationLifetime lifeTime, IDataAccess dataAccess, IOptions<PloomesSettings> options)
    {
        HotSettings.StartValues(options.Value);
        _dataAccess = dataAccess;
        LogM.Initialize(_dataAccess);
        _lifeTime = lifeTime;
        _counterOfObjects = 0;
    }

    #endregion

    private void CleanOldKeys()
    {
        var integrationObjskeysToRemove = _integrationIdControl
            .Where(kv => kv.Value < DateTime.UtcNow.AddMinutes(-2))
            .Select(kv => kv.Key)
            .ToList();

        // Remove entries based on the keysToRemove
        foreach (var key in integrationObjskeysToRemove)
        {
            try
            {
                _integrationIdControl.Remove(key, out _);
            }
            catch (Exception e)
            {
                Console.WriteLine($"e2: {e}");
            }
        }

        var accountkeysToRemove = _accountKeyTimer
            .Where(kv => kv.Value < DateTime.UtcNow.AddMinutes(-10))
            .Select(kv => kv.Key)
            .ToList();

        // Remove entries based on the keysToRemove
        foreach (var key in accountkeysToRemove)
        {
            try
            {
                _usersKeyTimer.Remove(key, out var _);
                _accountKeyTimer.TryRemove(key, out _);
            }
            catch (Exception e)
            {
                Console.WriteLine($"e3: {e}");
            }
        }
    }

    private async Task SendDataAsync(GenericIntegrationObject genericIntegrationObject, bool delay = false)
    {
        await _dataAccess.SendObjectToCallBackHub2Async(genericIntegrationObject, delay);

        _accountKeyTimer[genericIntegrationObject.AccountKey] = genericIntegrationObject.AccountKey == "8699eb3498e64671845492edd0625deb"
            ? DateTime.UtcNow.AddMilliseconds(350)
            : GetIntervalByAccountKey(genericIntegrationObject.IntegrationsType).Add(GetAdditionalIntervalByIntegrationAction(genericIntegrationObject.Url));
        if (string.IsNullOrEmpty(genericIntegrationObject.IntegrationId))
        {
            return;
        }

        if (_integrationIdControl.ContainsKey(genericIntegrationObject.IntegrationId))
        {
            _integrationIdControl[genericIntegrationObject.IntegrationId] = GetIntervalByObjectId(genericIntegrationObject.IntegrationsType);
            return;
        }

        _integrationIdControl.TryAdd(genericIntegrationObject.IntegrationId, GetIntervalByObjectId(genericIntegrationObject.IntegrationsType));
    }

    private static DateTime GetIntervalByAccountKey(IntegrationsType integrationsType)
    {
        return integrationsType switch
        {
            IntegrationsType.Omie => DateTime.UtcNow.AddMilliseconds(300),
            IntegrationsType.GoogleCalendar => DateTime.UtcNow.AddMilliseconds(500),
            IntegrationsType.Neppo => DateTime.UtcNow.AddMilliseconds(500),
            IntegrationsType.Outlook365 => DateTime.UtcNow.AddMilliseconds(500),

            _ => DateTime.UtcNow.AddMilliseconds(300)
        };
    }

    private static TimeSpan GetAdditionalIntervalByIntegrationAction(string url)
    {
        var action = url.Split('/').Reverse().Skip(1).FirstOrDefault();
        return action switch
        {
            "bringoneorder" => TimeSpan.FromSeconds(3),
            "bringonecontact" => TimeSpan.FromSeconds(3),
            "bringoneproduct" => TimeSpan.FromSeconds(3),
            "sendoneorder" => TimeSpan.FromSeconds(3),
            "sendonecontact" => TimeSpan.FromSeconds(3),
            _ => TimeSpan.Zero
        };
    }

    private static DateTime GetIntervalByObjectId(IntegrationsType integrationsType)
    {
        return integrationsType switch
        {
            IntegrationsType.Omie => DateTime.UtcNow.AddMinutes(3),
            IntegrationsType.GoogleCalendar => DateTime.UtcNow.AddMinutes(1),
            IntegrationsType.Outlook365 => DateTime.UtcNow.AddMinutes(1),
            _ => DateTime.UtcNow.AddSeconds(15)
        };
    }

    private void OnStopping()
    {
        try
        {
            LogM.Ok($" API {HotSettings.DDService} Stopping Messages Yet To be processed {JsonConvert.SerializeObject(_objeToBeSentLater)} {DateTime.UtcNow}. ", DateTime.UtcNow);
            Thread.Sleep(5000); //20000
        }
        catch (Exception e)
        {
            LogM.Error($"OnStopping v{ApiInfo.Version}: {e.Message}.", DateTime.UtcNow, e.StackTrace);
        }

        LogM.Ok($"OnStopping v{ApiInfo.Version} finished at {DateTime.UtcNow}.", DateTime.UtcNow);
    }

    private async Task SendMessagesThatAreWaitingAsync()
    {
        var messagesToSentAfterDelay = _integrationIdControl.Where(x => x.Value < DateTime.UtcNow).OrderBy(y => y.Value).Select(x => x.Key)
            .ToList();
        if (messagesToSentAfterDelay.Any())
        {
            foreach (var integrationId in messagesToSentAfterDelay.Where(key => _objeToBeSentLater.ContainsKey(key)))
            {
                try
                {
                    if (_objeToBeSentLater.ContainsKey(integrationId) is false)
                    {
                        LogM.Debug($"There is no Key {integrationId} in the dictionary", DateTime.UtcNow);
                        _integrationIdControl.TryRemove(integrationId, out _);

                        continue;
                    }

                    if (_accountKeyTimer.TryGetValue(_objeToBeSentLater[integrationId].AccountKey, out var time))
                    {
                        if (time > DateTime.UtcNow)
                        {
                            continue;
                        }
                        if (_objeToBeSentLater[integrationId].IntegrationsType == IntegrationsType.Omie)
                        {
                            if (await _dataAccess.CheckIfRequestForThisItemWasAlreadyExecutedAsync($"{_objeToBeSentLater[integrationId].AccountKey}_omie_id:{integrationId}") is false)
                            {
                                continue;
                            }
                        }

                        await SendDataAsync(_objeToBeSentLater[integrationId], true);
                        _counterOfObjects--;
                        var removed = _objeToBeSentLater.TryRemove(integrationId, out _);
                        if (removed is false)
                        {
                            LogM.Warn($"UNABLE TO REMOVE JsonOBJECT with integrationId of {integrationId}", DateTime.UtcNow);
                        }
                    }
                    else
                    {
                        var removed = _objeToBeSentLater.TryRemove(integrationId, out _);
                        if (removed is false)
                        {
                            LogM.Warn($"UNABLE TO REMOVE JsonOBJECT with integrationId of {integrationId}", DateTime.UtcNow);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception checking MessagesTo Sent AfterDelay {ex.Message}.{ex.StackTrace}");
                    if (ex.Message.Contains("not present in the dictionary"))
                    {
                        Console.WriteLine($"UNABLE TO REMOVE JsonOBJECT with integrationId of {ex.Message}.{ex.StackTrace}");
                    }
                }
            }
        }
    }

    private static void Subcribe(IConsumer<string, string> consumer)
    {
        consumer.Subscribe(HotSettings.KafkaTopic);
    }

    protected override async Task ExecuteAsync(CancellationToken cT)
    {
        var timedLog = DateTime.UtcNow.AddMinutes(1);
        _lifeTime.ApplicationStopping.Register(OnStopping);
        using var consumer = new ConsumerBuilder<string, string>(GetKafkaConfig()).Build();
        Subcribe(consumer);
        LogM.Ok($"Starting Queue: {ApiInfo.ServiceName}, Version: {ApiInfo.DdVersion},Env: {ApiInfo.Env}, Date: {ApiInfo.Version} ", DateTime.UtcNow);
        while (cT.IsCancellationRequested is false)
        {
            try
            {
                if (timedLog < DateTime.UtcNow)
                {
                    if (_objeToBeSentLater.IsEmpty is false)
                    {
                        LogM.Debug($"Just checking requests to be sent later by objectId:\n{JsonConvert.SerializeObject(_objeToBeSentLater.Select(x => x.Key).ToList())}. And number total of objects in the pod: {_counterOfObjects}", DateTime.UtcNow);
                    }

                    timedLog = DateTime.UtcNow.AddMinutes(1);
                }

                await SendMessagesThatAreWaitingAsync();


                try
                {
                    if (_objeToBeSentLater.Count < 80 || _counterOfObjects < 200 || (DateTime.UtcNow - _lastInteraction).Seconds < 30)
                    {
                        var consumeResult = consumer.Consume(150);
                        _lastInteraction = DateTime.UtcNow;
                        var genericObject = consumeResult?.Message is null ? null : JsonConvert.DeserializeObject<GenericIntegrationObject>(consumeResult.Message.Value);
                        if (genericObject is not null)
                        {
                            //  Console.WriteLine("Message consumed");
                            if (_accountKeyTimer.ContainsKey(genericObject.AccountKey) is false)
                            {
                                _accountKeyTimer.TryAdd(genericObject.AccountKey, DateTime.UtcNow);

                                GenericObjects genericObjects = new();
                                if (_usersKeyTimer.ContainsKey(genericObject.AccountKey) is false)
                                {
                                    _usersKeyTimer.TryAdd(genericObject.AccountKey, genericObjects);
                                }
                            }

                            _usersKeyTimer[genericObject.AccountKey].Queue.Enqueue(genericObject);
                            _counterOfObjects++;
                        }
                    }

                    //Look for account keys that had finished waiting the minimun amount of time to send the next request  
                    var accountsKeyReadyToSend = _accountKeyTimer
                        .Where(x => x.Value < DateTime.UtcNow)
                        .OrderBy(y => y.Value)
                        .Select(x => x.Key).ToList();
                    if (accountsKeyReadyToSend.Any() is false)
                    {
                        continue;
                    }

                    var accountTasks = new List<Task>(10);
                    foreach (var accountKey in accountsKeyReadyToSend.Where(accountKey => _usersKeyTimer[accountKey].Queue.Any()))
                    {
                        var task = Task.Run(async () => { await ManipulaAccountAsync(accountKey); }, cT);

                        accountTasks.Add(task);
                        if (accountTasks.Count < 10)
                        {
                            continue;
                        }

                        var completedTask = await Task.WhenAny(accountTasks);

                        accountTasks.Remove(completedTask);
                    }

                    await Task.WhenAll(accountTasks);

                    CleanOldKeys();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error to process Queue {ex.Message}.{ex.StackTrace} ");
                    if (ex.Message.Contains("Application maximum poll interval"))
                    {
                        Subcribe(consumer);
                    }
                }
            }
            catch (OutOfMemoryException exception)
            {
                Console.WriteLine($"Expetion {exception.Message}.{exception.StackTrace}");
                await Task.Delay(5000, cT);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception to deserialize obj from kafka {e.Message}.{e.StackTrace}");
                Subcribe(consumer);
            }
        }
    }

    private async Task ManipulaAccountAsync(string accountKey)
    {
        if (_usersKeyTimer[accountKey].Queue.Any() is false)
        {
            return;
        }

        var requestSent = false; // this property is used to get one message per accountkey at time, maybe is not the best aproach, but it's a try to let the balance of messages per AK 
        while (requestSent is false)
        {
            if (_usersKeyTimer[accountKey].Queue.TryDequeue(out var genericIntegrationObject))
            {
                try
                {
                    if (string.IsNullOrEmpty(genericIntegrationObject.IntegrationId) is false)
                    {
                        if (_integrationIdControl.ContainsKey(genericIntegrationObject.IntegrationId) is false)
                        {
                            _integrationIdControl.TryAdd(genericIntegrationObject.IntegrationId, GetIntervalByObjectId(genericIntegrationObject.IntegrationsType));
                        }
                        else
                        {
                            if (_integrationIdControl[genericIntegrationObject.IntegrationId] >
                                DateTime.UtcNow)
                            {
                                requestSent = true;
                                if (_objeToBeSentLater.ContainsKey(genericIntegrationObject.IntegrationId))
                                {
                                    _objeToBeSentLater[genericIntegrationObject.IntegrationId] = genericIntegrationObject;
                                    continue;
                                }

                                _objeToBeSentLater.TryAdd(genericIntegrationObject.IntegrationId, genericIntegrationObject);
                                continue;
                            }

                            if (genericIntegrationObject.IntegrationsType == IntegrationsType.Omie)
                            {

                                if (await _dataAccess.CheckIfRequestForThisItemWasAlreadyExecutedAsync($"{accountKey}_omie_id:{genericIntegrationObject.IntegrationId}") is false)
                                {
                                    _objeToBeSentLater.TryAdd(genericIntegrationObject.IntegrationId, genericIntegrationObject);
                                    continue;
                                }
                            }
                        }
                    }

                    await SendDataAsync(genericIntegrationObject);
                    _counterOfObjects--;
                    requestSent = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Intern error to process Queue for ak {accountKey} {e.Message}.{e.StackTrace} ");
                    requestSent = true;
                    _counterOfObjects--;
                }
            }
            else
            {
                Console.WriteLine($"Unable to deque {accountKey}");
            }
        }
    }

    private static ConsumerConfig GetKafkaConfig()
    {
        return new ConsumerConfig
        {
            BootstrapServers =
                HotSettings.KafkaHost,
            GroupId = "generic",
        };
    }
}