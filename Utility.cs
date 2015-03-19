using System;
using System.Diagnostics;
using Microsoft.Win32;
using RestSharp;

namespace GitHubWatch
{
	internal static class Utility
	{
		private static void SetBrowserFeatureControlKey(string feature, string appName, uint value)
		{
			using (var key = Registry.CurrentUser.CreateSubKey(
				string.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", feature),
				RegistryKeyPermissionCheck.ReadWriteSubTree)) {
				key.SetValue(appName, (UInt32)value, RegistryValueKind.DWord);
			}
		}

		public static void SetBrowserEmulationMode()
		{
			var fileName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

			if (String.Compare(fileName, "devenv.exe", true) == 0 || String.Compare(fileName, "XDesProc.exe", true) == 0)
				return;

			UInt32 mode = 10000U;
			SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", fileName, mode);
		}

		// Through api.github.com/repos/Microsoft/IEDiagnosticsAdapter 
		// We can actually get: "stargazers_count": 4, "watchers_count": 4 
		// "open_issues_count": 1, "forks": 0, "open_issues": 1,"network_count": 0, "subscribers_count": 17

		// All releases
		//api.github.com/repos/Microsoft/IEDiagnosticsAdapter/releases

		// Just one release
		// api.github.com/repos/Microsoft/IEDiagnosticsAdapter/releases/1048084
	}
}
