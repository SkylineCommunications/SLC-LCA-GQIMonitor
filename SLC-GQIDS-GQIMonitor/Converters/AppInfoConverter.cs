namespace GQI.Converters
{
	using System.Collections.Generic;
	using GQI.Caches;
	using GQIMonitor;
	using Skyline.DataMiner.Analytics.GenericInterface;

	internal static class AppInfoConverter
	{
		private static Dictionary<string, Application> _applications = null;

		public static string GetAppId(string queryTag)
		{
			if (queryTag.Length < 40)
				return null;

			if (!queryTag.StartsWith("app/"))
				return null;

			return queryTag.Substring(4, 36);
		}

		public static void RefreshApplicationsCache(GQIDMS dms, IGQILogger logger)
		{
			_applications = Cache.Instance.Applications
				.GetApplications(dms, logger);
		}

		public static string GetAppName(string queryTag)
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
	}
}
