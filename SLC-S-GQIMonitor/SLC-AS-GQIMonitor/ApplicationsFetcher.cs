using Newtonsoft.Json;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.Messages;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GQIMonitor
{
    public sealed class ApplicationsFetcher
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public ApplicationCollection GetFromWebAPI(IConnection connection)
        {
            return GetFromWebAPIAsync(connection).GetAwaiter().GetResult();
        }

        public async Task<ApplicationCollection> GetFromWebAPIAsync(IConnection connection)
        {
            var localAgentInfo = GetLocalAgentInfo(connection);
            var origin = GetWebAPIOrigin(localAgentInfo);
            var connectionTicket = GetConnectionTicket(connection);
            var connectionId = await GetWebAPIConnectionId(origin, connectionTicket);
            return await GetApplications(origin, connectionId);
        }

        private async Task<string> GetWebAPIConnectionId(string origin, string connectionTicket)
        {
            var request = new ConnectWithTicketRequest
            {
                ConnectionTicket = connectionTicket,
                Config = new ConnectionConfig { AppName = Info.AppName },
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
            var jsonRequest = JsonConvert.SerializeObject(request, Info.JsonSerializerSettings);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var httpResponse = await _httpClient.PostAsync(endpoint, content);
            if (!httpResponse.IsSuccessStatusCode)
                throw new GenIfException($"WebAPI request \"{endpoint}\" failed.");

            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<WebAPIResponse<T>>(jsonResponse, Info.JsonSerializerSettings);
            return response.Data;
        }

        private static string GetWebAPIOrigin(GeneralInfoEventMessage localInfo)
        {
            if (localInfo is null || !localInfo.HTTPS || string.IsNullOrWhiteSpace(localInfo.CertificateAddressName))
                return "http://localhost";

            return $"https://{localInfo.CertificateAddressName}";
        }

        private static GeneralInfoEventMessage GetLocalAgentInfo(IConnection connection)
        {
            try
            {
                var request = new GetInfoMessage(InfoType.LocalGeneralInfoMessage);
                return (GeneralInfoEventMessage)connection.HandleSingleResponseMessage(request);
            }
            catch (Exception ex)
            {
                throw new GenIfException("Failed to retrieve local agent info.", ex);
            }
        }

        private static string GetConnectionTicket(IConnection connection)
        {
            try
            {
                var request = new RequestTicketMessage(TicketType.Authentication, Array.Empty<byte>());
                var response = (TicketResponseMessage)connection.HandleSingleResponseMessage(request);
                return response.Ticket;
            }
            catch (Exception ex)
            {
                throw new GenIfException("Failed to retrieve local agent info.", ex);
            }
        }
    }
}
