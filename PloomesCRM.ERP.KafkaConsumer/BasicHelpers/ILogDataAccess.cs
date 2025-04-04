using PloomesCRMCallbackHub2.Queue.DLL.BasicCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers
{
    public interface ILogDataAccess
    {    /// <summary>Send the Log to DataDog using Http.<para>No internal try-catch.</para></summary>
         /// <param name="log">Log in DataDog format.</param>
         /// <returns>Null if success; $"Code: {StatusCode}, Reason: {ReasonPhrase}, Content: {content}" if fails.</returns>
        Task<string> SendLogToDataDogAsync(DatadogLog log);
    }
}
