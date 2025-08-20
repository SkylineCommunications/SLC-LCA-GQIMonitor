namespace SLCASGQIMonitorVersionCheck
{
	using System;
	using GQIMonitor;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public sealed class Script
	{
		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(IEngine engine)
		{
			try
			{
				RunSafe(engine);
			}
			catch (ScriptAbortException)
			{
				// Catch normal abort exceptions (engine.ExitFail or engine.ExitSuccess)
				throw; // Comment if it should be treated as a normal exit of the script.
			}
			catch (ScriptForceAbortException)
			{
				// Catch forced abort exceptions, caused via external maintenance messages.
				throw;
			}
			catch (ScriptTimeoutException)
			{
				// Catch timeout exceptions for when a script has been running for too long.
				throw;
			}
			catch (InteractiveUserDetachedException)
			{
				// Catch a user detaching from the interactive script by closing the window.
				// Only applicable for interactive scripts, can be removed for non-interactive scripts.
				throw;
			}
			catch (Exception e)
			{
				engine.ExitFail("Run|Something went wrong: " + e);
			}
		}

		private void RunSafe(IEngine engine)
		{
			var requiredVersionParam = engine.GetScriptParam(2);

			if (requiredVersionParam is null || string.IsNullOrEmpty(requiredVersionParam.Value))
				return;

			var requiredVersion = requiredVersionParam.Value;
			var actualVersion = GetGQIVersion(engine.GetUserConnection(), out var featureInfo);

			if (string.IsNullOrEmpty(actualVersion))
			{
				ShowUI(engine, "Version check failed", "We couldn't verify if this page is supported. Please ensure you have:", false);
				return;
			}

			if (IsVersionGreaterOrEqual(actualVersion, requiredVersion))
				return;

			ShowUI(engine, "Unsupported version", "To access this page, you need: ", featureInfo.IsUsingDxM);
		}

		private void ShowUI(IEngine engine, string title, string subTitle, bool hasDxM)
		{
			UIBuilder uiBuilder = new UIBuilder
			{
				RequireResponse = true,
				RowDefs = "a;a;a;a",
				ColumnDefs = "a",
			};

			uiBuilder.SkipAbortConfirmation = true;

			uiBuilder.Title = title;

			UIBlockDefinition blockStaticText = new UIBlockDefinition();
			blockStaticText.Type = UIBlockType.StaticText;
			blockStaticText.Text = subTitle;
			blockStaticText.Height = 20;
			blockStaticText.Width = 400;
			blockStaticText.Row = 0;
			blockStaticText.Column = 0;
			uiBuilder.AppendBlock(blockStaticText);

			if (!hasDxM)
			{
				UIBlockDefinition blockDxMText = new UIBlockDefinition();
				blockDxMText.Type = UIBlockType.StaticText;
				blockDxMText.Text = $" -   The GQI DxM.";
				blockDxMText.Height = 20;
				blockDxMText.Width = 400;
				blockDxMText.Row = 1;
				blockDxMText.Column = 0;
				uiBuilder.AppendBlock(blockDxMText);
			}

			var webAppVersionParam = engine.GetScriptParam(3);

			if (webAppVersionParam is null || string.IsNullOrEmpty(webAppVersionParam.Value))
				return;

			UIBlockDefinition blockVersionText = new UIBlockDefinition();
			blockVersionText.Type = UIBlockType.StaticText;
			blockVersionText.Text = $" -   DataMiner web app version {webAppVersionParam.Value} or newer.";
			blockVersionText.Height = 20;
			blockVersionText.Width = 400;
			blockVersionText.Row = 2;
			blockVersionText.Column = 0;
			uiBuilder.AppendBlock(blockVersionText);

			UIBlockDefinition blockButton = new UIBlockDefinition();
			blockButton.Type = UIBlockType.Button;
			blockButton.Text = "OK";
			blockButton.Height = 20;
			blockButton.Width = 75;
			blockButton.Row = 3;
			blockButton.Column = 0;
			uiBuilder.AppendBlock(blockButton);

			engine.ShowUI(uiBuilder);
		}

		private string GetGQIVersion(IConnection connection, out DMAGenericInterfaceFeatureInfo featureInfo)
		{
			string version = string.Empty;
			featureInfo = null;

			try
			{
				featureInfo = VersionFetcher.GetGQIFeatureInfo(connection);
				version = featureInfo.SemanticVersion;
			}
			catch
			{
			}

			return version;
		}

		private bool IsVersionGreaterOrEqual(string actual, string required)
		{
			if (Version.TryParse(actual, out var actualVer) && Version.TryParse(required, out var requiredVer))
			{
				return actualVer >= requiredVer;
			}

			return false;
		}
	}
}
