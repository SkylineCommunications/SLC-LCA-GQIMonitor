namespace GQI.Converters
{
	using System.Collections.Generic;
	using GQI.Caches;
	using GQIMonitor;
	using Skyline.DataMiner.Analytics.GenericInterface;

	internal class AppNameConverter
	{
		private Dictionary<string, Application> _applications = null;

		public AppNameConverter(GQIDMS dms, IGQILogger logger)
		{
			_applications = Cache.Instance.Applications
				.GetApplications(dms, logger);
		}

		public static string GetAppId(string queryTag)
		{
			if (queryTag.Length < 40)
				return null;

			if (!queryTag.StartsWith("app/"))
				return null;

			return queryTag.Substring(4, 36);
		}

		public string GetAppNameByTag(string queryTag)
		{
			if (_applications is null)
				return "<Other>";

			var appId = GetAppId(queryTag);
			if (appId is null)
				return "<Other>";
			if (_applications.TryGetValue(appId, out var app))
				return app.Name;

			return "<Other>";
		}

		public string GetAppNameById(string appId)
		{
			if (_applications is null)
				return "<Other>";

			if (appId is null)
				return "<Other>";
			if (_applications.TryGetValue(appId, out var app))
				return app.Name;
			return "<Other>";
		}
	}
}
