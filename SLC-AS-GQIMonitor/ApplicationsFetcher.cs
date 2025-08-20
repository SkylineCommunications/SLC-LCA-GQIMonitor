using System.Threading.Tasks;
using Skyline.DataMiner.Net;

namespace GQIMonitor
{
	public sealed class ApplicationsFetcher
	{
		public ApplicationCollection GetFromWebAPI(IConnection connection)
		{
			return GetFromWebAPIAsync(connection).GetAwaiter().GetResult();
		}

		public async Task<ApplicationCollection> GetFromWebAPIAsync(IConnection connection)
		{
			var webAPI = new WebAPI(connection);
			var connectionId = await webAPI.GetWebAPIConnectionId();
			return await GetApplications(webAPI, connectionId);
		}

		private Task<ApplicationCollection> GetApplications(WebAPI webAPI, string connectionId)
		{
			var request = new GetApplicationsRequest
			{
				ConnectionId = connectionId,
				IncludeDynamic = true,
				IncludeStatic = false,
			};
			var endpoint = $"{webAPI.WebAPIOrigin}/api/v1/internal.asmx/GetApplications";
			return webAPI.SendWebAPIRequest<ApplicationCollection>(endpoint, request);
		}
	}
}
