using System;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net;

namespace SLCASGQIMonitorVersionCheck
{
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

		private string GetGQIVersion(IConnection connection)
		{
			var versionFetcher = new GQIMonitor.VersionFetcher();
			return versionFetcher.GetGQIVersion(connection);
		}

		private void RunSafe(IEngine engine)
		{
			var requiredVersionParam = engine.GetScriptParam(2);

			if (requiredVersionParam is null || string.IsNullOrEmpty(requiredVersionParam.Value))
				return;

			var requiredVersion = requiredVersionParam.Value;
			var actualVersion = GetGQIVersion(engine.GetUserConnection());

			if (IsVersionGreaterOrEqual(actualVersion, requiredVersion))
				return;

			UIBuilder uiBuilder = new UIBuilder
			{
				RequireResponse = true,
				RowDefs = "a;a",
				ColumnDefs = "a",
			};

			UIBlockDefinition blockStaticText = new UIBlockDefinition();
			blockStaticText.Type = UIBlockType.StaticText;
			blockStaticText.Text = $"This page requires GQI version {requiredVersion}, is {actualVersion}";
			blockStaticText.Height = 20;
			blockStaticText.Width = 400;
			blockStaticText.Row = 0;
			blockStaticText.Column = 0;
			uiBuilder.AppendBlock(blockStaticText);

			UIBlockDefinition blockButton = new UIBlockDefinition();
			blockButton.Type = UIBlockType.Button;
			blockButton.Text = "OK";
			blockButton.Height = 20;
			blockButton.Width = 75;
			blockButton.Row = 1;
			blockButton.Column = 0;
			uiBuilder.AppendBlock(blockButton);

			engine.ShowUI(uiBuilder);
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

	internal class VersionAcceptedException : Exception
	{
		public VersionAcceptedException(string message) : base(message) { }
	}
}
