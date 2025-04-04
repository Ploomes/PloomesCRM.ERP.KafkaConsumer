using System;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PloomesCRMCallbackHub2.Queue.DLL.Entities
{
    public enum IntegrationsType
    {
        FacebookLeadsAds = 1,
        Omie = 2,
        Asana = 3,
        ActiveCampaign = 4,
        ClickSign = 5,
        DocuSign = 6,
        Econodata = 7,
        Foco = 8,
        GoogleAds = 9,
        GoogleCalendar = 10,
        Jivo = 11,
        LeadLovers = 12,
        LexDocs = 13,
        MailChimp = 14,
        Meetime = 15,
        Moviedesk = 16,
        Outlook365 = 17,
        PowerBI = 18,
        RDStation = 19,
        Reev = 20,
        Spotter = 21,
        Zenvia = 22,
        Neppo =23
    }

    public class GenericIntegrationObject
    {
        [JsonIgnore] public IntegrationsType IntegrationsType { get; set; }
        [JsonIgnore] public int? AccountId { get; set; }

        [JsonPropertyName("requestBody")] public JObject Requestbody { get; set; }

        [JsonIgnore] public string Url { get; set; }
        [JsonIgnore] public string IntegrationId { get; set; }
        [JsonPropertyName("accountKey")] public string AccountKey { get; set; }
        [JsonIgnore] public DateTime TimeToSend { get; set; }
        [JsonIgnore] public DateTime ReceveidDate { get; set; }


        public GenericIntegrationObject(IntegrationsType integrationsType, JObject requestBody, string url,
            string accountKey, string integrationId)
        {
            IntegrationsType = integrationsType;
            Requestbody = requestBody;
            Url = url;
            AccountKey = accountKey;
            IntegrationId = integrationId;
        }
    }
}