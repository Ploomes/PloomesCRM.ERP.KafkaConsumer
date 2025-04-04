using PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers;
using System;
using System.Text.Json.Serialization;


namespace PloomesCRMCallbackHub2.Queue.DLL.BasicCommon
{
    public class DatadogLog
    {
        #region Properties

        [JsonPropertyName("ddsource")] public string Source { get; set; }

        [JsonPropertyName("hostname")] public string Hostname { get; set; }
        [JsonPropertyName("service")] public string Service { get; set; }
        [JsonPropertyName("ddtags")] public string Tags { get; set; }

        [JsonPropertyName("message")] public string Message { get; set; }

        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("account")] public int Account { get; set; }
        [JsonPropertyName("user")] public int User { get; set; }
        [JsonPropertyName("method")] public string Method { get; set; }

        #endregion Properties

        public DatadogLog(string type, string message, DateTime date, int? account, int? user, string methodName, string accountKey)
        {
            Source = "integrations.hub"; //HARDCODED FOR EMAIL APIS
            Hostname = string.IsNullOrEmpty(accountKey) ? HotSettings.DDHostName : accountKey;
            Service = HotSettings.DDService;
            Tags = HotSettings.DDTags; //Tags = "env:prod,version:1.0";
            Type = type;
            Account = account ?? 0;
            User = user ?? 0;
            Method = string.IsNullOrWhiteSpace(methodName) ? "none" : methodName;
            Message = $"{type.ToUpperInvariant()} : {message}, at: {date}";
        }
    }
}