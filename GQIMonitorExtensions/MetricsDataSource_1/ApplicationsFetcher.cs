using Newtonsoft.Json;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Messages;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MetricsDataSource_1
{
    internal sealed class ApplicationsFetcher
    {
        public static readonly GQIColumn[] Columns = new GQIColumn[]
        {
            new GQIStringColumn("ID"),
            new GQIStringColumn("Name"),
        };

        private const string FilePath = GQIMonitor.DocumentsPath + @"\applications.json";

        private readonly HttpClient _httpClient = new HttpClient();

        public GQIRow[] GetApplicationRows(Config config, GQIDMS dms, IGQILogger logger)
        {
            logger.Information("Retrieving applications...");
            try
            {

                var applications = GetApplications(config, dms, logger);
                return GetRows(applications);
            }
            catch (Exception ex)
            {
                throw new GenIfException("Failed to retrieve applications.", ex);
            }
        }

        private ApplicationCollection GetApplications(Config config, GQIDMS dms, IGQILogger logger)
        {
            switch (config.Mode)
            {
                case Mode.Snapshot:
                    return GetApplicationsFromFile(FilePath, logger);
                default:
                    return GetApplicationsFromWebAPI(dms, logger);
            }
        }

        private static ApplicationCollection GetApplicationsFromFile(string filePath, IGQILogger logger)
        {
            if (!File.Exists(filePath))
            {
                logger.Warning($"Applications file \"{FilePath}\" does not exist");
                return null;
            }

            var json = File.ReadAllText(filePath);
            var apiResult = JsonConvert.DeserializeObject<WebAPIResponse<ApplicationCollection>>(json, GQIMonitor.JsonSerializerSettings);
            return apiResult.Data;
        }

        private ApplicationCollection GetApplicationsFromWebAPI(GQIDMS dms, IGQILogger logger)
        {
            return GetApplicationsFromWebAPIAsync(dms, logger).GetAwaiter().GetResult();
        }

        private async Task<ApplicationCollection> GetApplicationsFromWebAPIAsync(GQIDMS dms, IGQILogger logger)
        {
            var localAgentInfo = GetLocalAgentInfo(dms);
            var origin = GetWebAPIOrigin(localAgentInfo);
            var connectionTicket = GetConnectionTicket(dms);
            var connectionId = await GetWebAPIConnectionId(origin, connectionTicket);
            return await GetApplications(origin, connectionId);
        }

        private async Task<string> GetWebAPIConnectionId(string origin, string connectionTicket)
        {
            var request = new ConnectWithTicketRequest
            {
                ConnectionTicket = connectionTicket,
                Config = new ConnectionConfig { AppName = GQIMonitor.AppName },
            };
            var endpoint = $"{origin}/api/v1/internal.asmx/ConnectWithTicket";
            var response = await SendWebAPIRequest<ConnectionInfo>(endpoint, request);
            return response.Connection;
        }

        private Task<ApplicationCollection> GetApplications(string origin, string connectionId)
        {
            var request = new GetApplicationsRequest
            {
                ConnectionId = connectionId,
                IncludeDynamic = true,
                IncludeStatic = false,
            };
            var endpoint = $"{origin}/api/v1/internal.asmx/GetApplications";
            return SendWebAPIRequest<ApplicationCollection>(endpoint, request);
        }

        private async Task<T> SendWebAPIRequest<T>(string endpoint, object request)
        {
            var jsonRequest = JsonConvert.SerializeObject(request, GQIMonitor.JsonSerializerSettings);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var httpResponse = await _httpClient.PostAsync(endpoint, content);
            if (!httpResponse.IsSuccessStatusCode)
                throw new GenIfException($"WebAPI request \"{endpoint}\" failed.");

            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<WebAPIResponse<T>>(jsonResponse, GQIMonitor.JsonSerializerSettings);
            return response.Data;
        }

        private static string GetWebAPIOrigin(GeneralInfoEventMessage localInfo)
        {
            if (localInfo is null || !localInfo.HTTPS || string.IsNullOrWhiteSpace(localInfo.CertificateAddressName))
                return "http://localhost";

            return $"https://{localInfo.CertificateAddressName}";
        }

        private static GeneralInfoEventMessage GetLocalAgentInfo(GQIDMS dms)
        {
            try
            {
                var request = new GetInfoMessage(InfoType.LocalGeneralInfoMessage);
                return (GeneralInfoEventMessage)dms.SendMessage(request);
            }
            catch (Exception ex)
            {
                throw new GenIfException("Failed to retrieve local agent info.", ex);
            }
        }

        private static string GetConnectionTicket(GQIDMS dms)
        {
            try
            {
                var request = new RequestTicketMessage(TicketType.Authentication, Array.Empty<byte>());
                var response = (TicketResponseMessage)dms.SendMessage(request);
                return response.Ticket;
            }
            catch (Exception ex)
            {
                throw new GenIfException("Failed to retrieve local agent info.", ex);
            }
        }

        private static GQIRow[] GetRows(ApplicationCollection collection)
        {
            var applications = collection?.Applications;
            if (applications is null)
                return Array.Empty<GQIRow>();

            return applications
                .Select(ToRow)
                .ToArray();
        }

        private static GQIRow ToRow(Application application)
        {
            var cells = new[]
            {
                new GQICell { Value = application.ID },
                new GQICell { Value = application.Name },
            };

            return new GQIRow(application.ID, cells);
        }

        private sealed class WebAPIResponse<T>
        {
            [JsonProperty("d")]
            public T Data { get; set; }
        }

        private sealed class ApplicationCollection
        {
            [JsonProperty("DynamicApplications")]
            public Application[] Applications { get; set; }
        }

        private sealed class Application
        {
            [JsonProperty("ID")]
            public string ID { get; set; }

            [JsonProperty("Name")]
            public string Name { get; set; }
        }

        private sealed class ConnectWithTicketRequest
        {
            [JsonProperty("connectionTicket")]
            public string ConnectionTicket { get; set; }

            [JsonProperty("config")]
            public ConnectionConfig Config { get; set; }
        }

        private sealed class GetApplicationsRequest
        {
            [JsonProperty("connection")]
            public string ConnectionId { get; set; }

            [JsonProperty("includeDynamic")]
            public bool IncludeDynamic { get; set; }

            [JsonProperty("includeStatic")]
            public bool IncludeStatic { get; set; }
        }

        private sealed class ConnectionInfo
        {
            public string Connection { get; set; }
        }

        private sealed class ConnectionConfig
        {
            public string AppName { get; set; }
        }
    }
}
