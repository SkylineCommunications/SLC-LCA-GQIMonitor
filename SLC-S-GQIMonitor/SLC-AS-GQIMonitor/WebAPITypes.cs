using Newtonsoft.Json;

namespace GQIMonitor
{
	internal sealed class WebAPIResponse<T>
	{
		[JsonProperty("d")]
		public T Data { get; set; }
	}

	public sealed class ApplicationCollection
	{
		[JsonProperty("DynamicApplications")]
		public Application[] Applications { get; set; }
	}

	public sealed class Application
	{
		[JsonProperty("ID")]
		public string ID { get; set; }

		[JsonProperty("Name")]
		public string Name { get; set; }
	}

	internal sealed class ConnectWithTicketRequest
	{
		[JsonProperty("connectionTicket")]
		public string ConnectionTicket { get; set; }

		[JsonProperty("config")]
		public ConnectionConfig Config { get; set; }
	}

	internal sealed class GetApplicationsRequest
	{
		[JsonProperty("connection")]
		public string ConnectionId { get; set; }

		[JsonProperty("includeDynamic")]
		public bool IncludeDynamic { get; set; }

		[JsonProperty("includeStatic")]
		public bool IncludeStatic { get; set; }
	}

	internal sealed class ConnectionInfo
	{
		public string Connection { get; set; }
	}

	internal sealed class ConnectionConfig
	{
		public string AppName { get; set; }
	}

	internal sealed class GetFeatureInfoRequest
	{
		[JsonProperty("featureNames")]
		public string[] FeatureNames { get; set; }
	}

	internal sealed class DMAGenericInterfaceFeatureInfo
	{
		[JsonProperty("SemanticVersion")]
		public string SemanticVersion { get; set; }
	}
}
