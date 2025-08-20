using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.Messages;

namespace GQIMonitor
{
	internal sealed class WebAPI
	{
		private readonly HttpClient _httpClient = new HttpClient();

		private readonly GeneralInfoEventMessage _localAgentInfo;
		private readonly string _connectionTicket;

		public WebAPI(IConnection connection)
		{
			_localAgentInfo = GetLocalAgentInfo(connection);
			WebAPIOrigin = GetWebAPIOrigin(_localAgentInfo);
			_connectionTicket = GetConnectionTicket(connection);
		}

		public string WebAPIOrigin { get; }

		public async Task<string> GetWebAPIConnectionId()
		{
			var request = new ConnectWithTicketRequest
			{
				ConnectionTicket = _connectionTicket,
				Config = new ConnectionConfig { AppName = Info.AppName },
			};
			var endpoint = $"{WebAPIOrigin}/api/v1/internal.asmx/ConnectWithTicket";
			var response = await SendWebAPIRequest<ConnectionInfo>(endpoint, request);
			return response.Connection;
		}

		public async Task<T> SendWebAPIRequest<T>(string endpoint, object request)
		{
			var jsonRequest = JsonConvert.SerializeObject(request, Info.JsonSerializerSettings);
			var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

			var httpResponse = await _httpClient.PostAsync(endpoint, content);
			if (!httpResponse.IsSuccessStatusCode)
				throw new GenIfException($"WebAPI request \"{endpoint}\" failed.");

			var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
			WebAPIResponse<T> response;
			try
			{
				response = JsonConvert.DeserializeObject<WebAPIResponse<T>>(jsonResponse, Info.JsonSerializerSettings);
			}
			catch (JsonException ex)
			{
				throw new Exception($"Failed to deserialize response from WebAPI request \"{endpoint}\". Response was: {jsonResponse}", ex);
			}

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
