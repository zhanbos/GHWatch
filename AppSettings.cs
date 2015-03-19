using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubWatch
{
	internal class AppSettings
	{
		public const string Version = " V0.5";

		// Reference: https://developer.github.com/v3/
		public const string GitHubDomain = "https://api.github.com/";

		public const string AppCaption = "GitHub Watch" + Version;

		private static ProjectSetting projectSettingIEDiagnosticsAdapter = new ProjectSetting {
			Name = "IEDiagnosticsAdapter", 
			ReferringSitesCount = 4, 
			DownloadCountReleaseIds = new int[] { 1048084 } };
		
		private static ProjectSetting projectSettingTypeScript = new ProjectSetting { Name = "TypeScript", ReferringSitesCount = 10 };

		public static ProjectSetting[] projectSettings = new ProjectSetting[2] { projectSettingTypeScript, projectSettingIEDiagnosticsAdapter };

	}
}
