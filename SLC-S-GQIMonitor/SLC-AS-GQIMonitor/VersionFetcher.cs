using System.Linq;
using System.Threading.Tasks;
using Skyline.DataMiner.Net;

namespace GQIMonitor
{
	public sealed class VersionFetcher
	{
		public string GetGQIVersion(IConnection connection)
		{
			return GetGQIVersionAsync(connection).GetAwaiter().GetResult();
		}

		public async Task<string> GetGQIVersionAsync(IConnection connection)
		{
			var webAPI = new WebAPI(connection);
			var request = new GetFeatureInfoRequest
			{
				FeatureNames = new[] { "GenericInterface" },
			};
			var endpoint = $"{webAPI.WebAPIOrigin}/api/v1/internal.asmx/GetFeatureInfo";
			var featureInfos = await webAPI.SendWebAPIRequest<DMAGenericInterfaceFeatureInfo[]>(endpoint, request);

			return featureInfos.FirstOrDefault().SemanticVersion;
		}
	}
}
