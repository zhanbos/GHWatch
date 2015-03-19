using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using mshtml;
using Newtonsoft.Json;
using RestSharp;

// TODO: Consider DispatcherTimerWeakEvent to avoid potential memory leak? (Verify if there is memory leak first.)

namespace GitHubWatch
{
	internal enum CurrentTask
	{
		LoadUrlAndCheckAccess,
		LoadUrlAndCheckAccessCompleted,
		ProcessTopLists,
		ProcessTopListsInAction,
		ProcessTopListsCompleted,
		ProcessTrafficData,
		ProcessTrafficDataInAction,
		ProcessTrafficDataCompleted,
		ProcessCloneActivityData,
		ProcessCloneActivityDataInAction,
		ProcessCloneActivityDataCompleted,
		ProcessWebPage,
		ProcessWebPageInAction,
		ProcessWebPageCompleted,
		ProcessDownloadCount,
		ProcessDownloadCountInAction,
		ProcessDownloadCountCompleted,
		CheckProjectSettings,
		WaitForNextAutoCheck,
		WaitForNextAutoCheckComplete
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private int currentProjectSettingIndex = 0;

		private CurrentTask appCurrentTask;


		private CurrentTask AppCurrentTask
		{
			get
			{
				return this.appCurrentTask;
			}
			set
			{
				this.txtStatus.Text = DateTime.Now.ToLongTimeString() + " [" + AppSettings.projectSettings[currentProjectSettingIndex].Name + "] " + value.ToString();

				appCurrentTask = value;
			}
		}

		private TimeSpan defaultInterval = System.TimeSpan.FromSeconds(3);
		private TimeSpan WaitForNextAutoCheckInterval = System.TimeSpan.FromHours(2);

		private DispatcherTimer appTimer;

		public MainWindow()
		{
			InitializeComponent();
			this.Closing += MainWindow_Closing;
			Utility.SetBrowserEmulationMode();

			System.Diagnostics.Debug.Assert(AppSettings.projectSettings != null && AppSettings.projectSettings.Length > 0, "AppSettings.projectSettings are not set up correctly!");
			currentProjectSettingIndex = 0;

			this.webBrowser.LoadCompleted += webBrowser_LoadCompleted;

			this.AppCurrentTask = CurrentTask.LoadUrlAndCheckAccess;
			this.webBrowser.Source = new Uri(AppSettings.projectSettings[currentProjectSettingIndex].TrafficUrl);
		}

		void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (this.AppCurrentTask != CurrentTask.WaitForNextAutoCheckComplete) {
				MessageBox.Show("Please Wait Until Current Update is Completed.");
				e.Cancel = true;
			}
		}

		void appTimer_Tick(object sender, EventArgs e)
		{
			switch (this.AppCurrentTask) {

				case CurrentTask.LoadUrlAndCheckAccessCompleted:
					this.AppCurrentTask = CurrentTask.ProcessTopLists;
					break;
				case CurrentTask.ProcessTopLists:
					DisableBtnAction();
					this.webBrowser.Source = new Uri("https://github.com/Microsoft/" + AppSettings.projectSettings[currentProjectSettingIndex].Name + "/graphs/traffic?partial=top_lists");

					this.AppCurrentTask = CurrentTask.ProcessTopListsInAction;
					break;
				case CurrentTask.ProcessTopListsCompleted:
					this.AppCurrentTask = CurrentTask.ProcessTrafficData;
					break;
				case CurrentTask.ProcessTrafficData:
					this.webBrowser.Source = new Uri("https://github.com/Microsoft/" + AppSettings.projectSettings[currentProjectSettingIndex].Name + "/graphs/traffic-data");
					this.AppCurrentTask = CurrentTask.ProcessTrafficDataInAction;
					break;
				case CurrentTask.ProcessTrafficDataCompleted:
					this.AppCurrentTask = CurrentTask.ProcessCloneActivityData;
					break;
				case CurrentTask.ProcessCloneActivityData:
					this.webBrowser.Source = new Uri("https://github.com/Microsoft/" + AppSettings.projectSettings[currentProjectSettingIndex].Name + "/graphs/clone-activity-data");

					this.AppCurrentTask = CurrentTask.ProcessCloneActivityDataInAction;
					break;
				case CurrentTask.ProcessCloneActivityDataCompleted:
					this.AppCurrentTask = CurrentTask.ProcessWebPage;
					break;
				case CurrentTask.ProcessWebPage:
					this.webBrowser.Source = new Uri(AppSettings.projectSettings[currentProjectSettingIndex].TrafficUrl);

					this.AppCurrentTask = CurrentTask.ProcessWebPageInAction;
					break;
				case CurrentTask.ProcessWebPageCompleted:
					if (AppSettings.projectSettings[currentProjectSettingIndex].DownloadCountReleaseIds != null) {
						this.AppCurrentTask = CurrentTask.ProcessDownloadCount;
					}
					else {
						this.AppCurrentTask = CurrentTask.CheckProjectSettings;
					}
					break;
				case CurrentTask.ProcessDownloadCount:
					this.AppCurrentTask = CurrentTask.ProcessDownloadCountInAction;
					break;
				case CurrentTask.ProcessDownloadCountInAction:
					ActionProcessDownloadCount();
					break;
				case CurrentTask.ProcessDownloadCountCompleted:
					this.AppCurrentTask = CurrentTask.CheckProjectSettings;
					break;
				case CurrentTask.CheckProjectSettings:
					if (currentProjectSettingIndex < AppSettings.projectSettings.Length - 1) {
						currentProjectSettingIndex++;
						this.AppCurrentTask = CurrentTask.ProcessTopLists;
					}
					else {
						currentProjectSettingIndex = 0;
						this.AppCurrentTask = CurrentTask.WaitForNextAutoCheck;
					}
					break;
				case CurrentTask.WaitForNextAutoCheck:
					this.appTimer.Interval = this.WaitForNextAutoCheckInterval;
					this.AppCurrentTask = CurrentTask.WaitForNextAutoCheckComplete;

					this.txtMain.Text = "Next Auto Check Is Scheduled Around " + System.DateTime.Now.AddHours(this.WaitForNextAutoCheckInterval.Hours).ToLocalTime() + " local time. You can click on \"Manual Check Now\" button to initialize a manual check.";

					this.btnAction.IsEnabled = true;
					this.btnAction.Content = "Manual Check Now!";
					this.btnAction.Tag = "ManualCheckNow";
					break;
				case CurrentTask.WaitForNextAutoCheckComplete:
					// For Robustness over long time, restart the app itself.
					//Process.Start(Application.ResourceAssembly.Location);
					//Application.Current.Shutdown();

					this.appTimer.Interval = this.defaultInterval;
					this.AppCurrentTask = CurrentTask.ProcessTopLists;

					break;
			}
		}

		private void DisableBtnAction()
		{
			this.btnAction.IsEnabled = false;
			this.btnAction.Content = AppSettings.AppCaption;
			this.btnAction.Tag = "";
		}

		void webBrowser_LoadCompleted(object sender, NavigationEventArgs e)
		{
			switch (appCurrentTask) {
				case CurrentTask.LoadUrlAndCheckAccess:
					ActionLoadUrlAndCheckAccess();
					break;

				case CurrentTask.ProcessTopListsInAction:
					ActionProcessTopLists();
					break;

				case CurrentTask.ProcessTrafficDataInAction:
					ActionProcessTrafficData();
					break;

				case CurrentTask.ProcessCloneActivityDataInAction:
					ActionProcessCloneActivityData();
					break;

				case CurrentTask.ProcessWebPageInAction:
					ActionProcessWebPageInAction();
					break;
				default:
					System.Diagnostics.Debug.Fail("LoadCompleted for invalid task - " + appCurrentTask);
					break;
			}
		}

		private void ActionProcessWebPageInAction()
		{
			int watchTotal = -1;
			int starTotal = -1;
			int forkTotal = -1;

			HTMLDocument doc = webBrowser.Document as HTMLDocument;
			string innerHtml = doc.body.innerHTML;

			string[] lines = innerHtml.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
			int linesScanIndex;

			for (linesScanIndex = 0; linesScanIndex < lines.Length; linesScanIndex++) {
				string line = lines[linesScanIndex];
				if (line.Contains("social-count")) {
					if (line.Contains("watchers")) {
						linesScanIndex++;
						watchTotal = int.Parse(lines[linesScanIndex].Trim(), NumberStyles.AllowThousands);
					}
					else if (line.Contains("stargazers")) {
						linesScanIndex++;
						starTotal = int.Parse(lines[linesScanIndex].Trim(), NumberStyles.AllowThousands);
					}
					else if (line.Contains("network")) {
						forkTotal = ScanForNumber(line, "network");
					}
					else {
						// Assert
						System.Diagnostics.Debug.Fail("Unexpected Match in ActionProcessWebPageInAction. Please Investigate!");
					}
				}
			}

			int dateId = DateTime.UtcNow.ToDateIDFromDateTime();
			StringBuilder sb = new StringBuilder();

			HelperProcessSummaryData(sb, dateId, "WatchTotal", watchTotal);
			HelperProcessSummaryData(sb, dateId, "StarTotal", starTotal);
			HelperProcessSummaryData(sb, dateId, "ForkTotal", forkTotal);

			this.txtMain.Text = sb.ToString();
			this.AppCurrentTask = CurrentTask.ProcessWebPageCompleted;
		}

		private void ActionProcessDownloadCount()
		{
			var client = new RestClient(AppSettings.GitHubDomain);

			int dateId = DateTime.UtcNow.ToDateIDFromDateTime();
			StringBuilder sb = new StringBuilder("DownloadCount");
			sb.AppendLine();
			foreach (int releaseId in AppSettings.projectSettings[currentProjectSettingIndex].DownloadCountReleaseIds) {
				sb.AppendLine("Release ID: " + releaseId);

				var request = new RestRequest("repos/{owner}/{repo}/releases/{id}", Method.GET);

				request.AddParameter("owner", "Microsoft", ParameterType.UrlSegment);
				request.AddParameter("repo", AppSettings.projectSettings[currentProjectSettingIndex].Name, ParameterType.UrlSegment);
				request.AddParameter("id", releaseId, ParameterType.UrlSegment);

				// execute the request
				var response = client.Execute(request);

				if (response.ResponseStatus == ResponseStatus.Error) {
					MessageBox.Show("Error in getting API call response.", AppSettings.AppCaption);
				}
				else {
					string content = response.Content;
					string[] parts = content.Split(new string[] { "\"download_count\":" }, StringSplitOptions.RemoveEmptyEntries);

					Debug.Assert(parts.Length == 2, "Should only find one download_count");
					parts = parts[1].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
					int downloadCount = int.Parse(parts[0]);

					HelperProcessDownloadCount(sb, dateId, releaseId, downloadCount);
				}
			}

			this.txtMain.Text = sb.ToString();
			this.AppCurrentTask = CurrentTask.ProcessDownloadCountCompleted;
		}

		private void HelperProcessDownloadCount(StringBuilder sb, int dateId, int releaseId, int downloadCount)
		{
			if (downloadCount < 0) {
				MessageBox.Show("Negative Download Count Value for " + releaseId);
			}
			else {
				sb.Append(releaseId + ":" + downloadCount);
				sb.AppendLine();
			}

			long dbDownloadCount = DAL.QueryReleasesDownloadCount(dateId, AppSettings.projectSettings[currentProjectSettingIndex].Name, releaseId);
			if (dbDownloadCount < 0) {
				DAL.InsertIntoReleases(dateId, AppSettings.projectSettings[currentProjectSettingIndex].Name, releaseId, downloadCount);
				sb.Append(releaseId + ": Saved to DB with count " + downloadCount);
			}
			else {
				if (dbDownloadCount == downloadCount) {
					sb.Append(releaseId + ": Already in DB. Same Value. Skip.");
				}
				else if (dbDownloadCount < downloadCount) {
					sb.Append(releaseId + ": Already in DB. !BUT! DB value is smaller:");
					sb.Append(dbDownloadCount);
					sb.Append(". Update.");
					DAL.UpdateReleasesDownloadCount(dateId, AppSettings.projectSettings[currentProjectSettingIndex].Name, releaseId, downloadCount);
				}
				else {
					sb.Append(releaseId + ": Already in DB. !BUT! DB value is bigger:");
					sb.Append(dbDownloadCount);

					sb.AppendLine();
					sb.Append("While this is strange, we will still update DB.");
					DAL.UpdateReleasesDownloadCount(dateId, AppSettings.projectSettings[currentProjectSettingIndex].Name, releaseId, downloadCount);
				}
			}
			sb.AppendLine();
		}

		private void ActionProcessCloneActivityData()
		{
			string category = "Clone";

			HTMLDocument doc = webBrowser.Document as HTMLDocument;
			string innerText = doc.body.innerText;

			ActivityData activity = JsonConvert.DeserializeObject<ActivityData>(innerText);

			StringBuilder sb = new StringBuilder("Processing " + category + " data...");
			sb.AppendLine();

			foreach (Count c in activity.counts) {
				sb.Append(c.bucket.ToDateIDFromBucket());
				sb.Append("  Total:");
				sb.Append(c.total);
				sb.Append("  Unique:");
				sb.Append(c.unique);
				sb.AppendLine();

				HelperProcessSummaryData(sb, c.bucket.ToDateIDFromBucket(), category + "Total", c.total);
				HelperProcessSummaryData(sb, c.bucket.ToDateIDFromBucket(), category + "Unique", c.unique);
				sb.AppendLine();
			}

			this.txtMain.Text = sb.ToString();
			this.AppCurrentTask = CurrentTask.ProcessCloneActivityDataCompleted;
		}

		// Friendly name for Traffic is Visitors
		private void ActionProcessTrafficData()
		{
			string category = "Visitor";

			HTMLDocument doc = webBrowser.Document as HTMLDocument;
			string innerText = doc.body.innerText;

			TrafficData traffic = JsonConvert.DeserializeObject<TrafficData>(innerText);

			StringBuilder sb = new StringBuilder("Processing " + category + " data...");
			sb.AppendLine();

			foreach (Count c in traffic.counts) {
				sb.Append(c.bucket.ToDateIDFromBucket());
				sb.Append("  Total:");
				sb.Append(c.total);
				sb.Append("  Unique:");
				sb.Append(c.unique);
				sb.AppendLine();

				HelperProcessSummaryData(sb, c.bucket.ToDateIDFromBucket(), category + "Total", c.total);
				HelperProcessSummaryData(sb, c.bucket.ToDateIDFromBucket(), category + "Unique", c.unique);
				sb.AppendLine();
			}

			this.txtMain.Text = sb.ToString();
			this.AppCurrentTask = CurrentTask.ProcessTrafficDataCompleted;
		}

		private void HelperProcessSummaryData(StringBuilder sb, int dateId, string category, long count)
		{
			if (count < 0) {
				MessageBox.Show("Negative Count Value for " + category);
			}
			else {
				sb.Append(category + ":" + count);
				sb.AppendLine();
			}

			long dbCount = DAL.QuerySummaryCount(dateId, AppSettings.projectSettings[currentProjectSettingIndex].Name, category);
			if (dbCount < 0) {
				DAL.InsertIntoSummary(dateId, AppSettings.projectSettings[currentProjectSettingIndex].Name, category, count);
				sb.Append(category + ": Saved to DB with count " + count);
			}
			else {
				if (dbCount == count) {
					sb.Append(category + ": Already in DB. Same Value. Skip.");
				}
				else if (dbCount < count) {
					sb.Append(category + ": Already in DB. !BUT! DB value is smaller:");
					sb.Append(dbCount);
					sb.Append(". Update.");
					DAL.UpdateSummary(dateId, AppSettings.projectSettings[currentProjectSettingIndex].Name, category, count);
				}
				else {
					sb.Append(category + ": Already in DB. !BUT! DB value is bigger:");
					sb.Append(dbCount);

					if (dateId - System.DateTime.UtcNow.ToDateIDFromDateTime() >= -2) {
						sb.Append(". Allow Update for the past couple days!");
						DAL.UpdateSummary(dateId, AppSettings.projectSettings[currentProjectSettingIndex].Name, category, count);
					}
					else {
						MessageBox.Show("DB value is larger for " + category + ". " + dbCount + " vs " + count + ". And the date is rather old!");
					}
				}
			}
			sb.AppendLine();
		}

		private void ActionLoadUrlAndCheckAccess()
		{
			HTMLDocument doc = webBrowser.Document as HTMLDocument;
			string innerHtml = doc.body.innerHTML;

			if (innerHtml.Contains("login_field")) {
				this.txtMain.Text = "Please log into GitHub, and then click the button to 'Try Again'. Be sure to complete secondary authentication. ";

				this.btnAction.IsEnabled = true;
				this.btnAction.Content = "Try Again";
				this.btnAction.Tag = "LoadUrlAndCheckAccess";
			}
			else if (innerHtml.Contains(AppSettings.projectSettings[currentProjectSettingIndex].Name) && innerHtml.Contains("Visitors")) {
				this.txtMain.Text = "";
				this.txtStatus.Text = "";
				this.btnAction.IsEnabled = false;
				this.btnAction.Content = AppSettings.AppCaption;
				this.btnAction.Tag = "";

				this.AppCurrentTask = CurrentTask.LoadUrlAndCheckAccessCompleted;
				this.appTimer = new DispatcherTimer();
				appTimer.Interval = defaultInterval;
				appTimer.Tick += appTimer_Tick;
				this.appTimer.Start();
			}
			else {
				System.Windows.MessageBox.Show("GitHub Page Changed! Updated is needed!", AppSettings.AppCaption, System.Windows.MessageBoxButton.OK);
			}

		}

		private void ActionProcessTopLists()
		{
			HTMLDocument doc = webBrowser.Document as HTMLDocument;
			string innerHtml = doc.body.innerHTML;

			string[] lines = innerHtml.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
			int linesScanIndex = 0;

			// Expect to find 10 Referring Site
			ReferringSite[] referringSites = new ReferringSite[AppSettings.projectSettings[currentProjectSettingIndex].ReferringSitesCount];
			for (int i = 0; i < referringSites.Length && linesScanIndex < lines.Length; i++) {
				do {
					if (lines[linesScanIndex].Contains("capped-list-label")) {
						break;
					}
					linesScanIndex++;
				} while (linesScanIndex < lines.Length);

				linesScanIndex += 2;
				string site = ScanForSite(lines[linesScanIndex]);
				linesScanIndex += 2;
				int viewsCount = ScanForNumber(lines[linesScanIndex], "middle");
				linesScanIndex++;
				int uniqueVisitorsCount = ScanForNumber(lines[linesScanIndex], "middle");

				referringSites[i] = new ReferringSite { Site = site, ViewsCount = viewsCount, UniqueVisitorsCount = uniqueVisitorsCount };
			}

			if (linesScanIndex >= lines.Length) {
				System.Windows.MessageBox.Show("Scanning failure in ActionProcessTopLists");
			}

			// Informational purpose
			StringBuilder sb = new StringBuilder();
			foreach (ReferringSite site in referringSites) {
				sb.Append("Site: ");
				sb.Append(site.Site);
				sb.Append("ViewsCount: ");
				sb.Append(site.ViewsCount);
				sb.Append("UniqueVisitorsCount: ");
				sb.Append(site.UniqueVisitorsCount);

				sb.AppendLine();
				sb.AppendLine();
			}

			// Write to Database
			int dateId = DateTime.UtcNow.ToDateIDFromDateTime();
			if (DAL.QueryReferringSitesRecordCount(dateId, AppSettings.projectSettings[currentProjectSettingIndex].Name) > 0) {
				sb.AppendLine("Not Going to Write to DB as records have been found for today.");
			}
			else {
				foreach (ReferringSite site in referringSites) {
					DAL.InsertIntoReferringSites(dateId, AppSettings.projectSettings[currentProjectSettingIndex].Name, site.Site, site.ViewsCount, site.UniqueVisitorsCount);
				}
				sb.AppendLine("All written to DB.");
			}

			this.AppCurrentTask = CurrentTask.ProcessTopListsCompleted;
			this.txtMain.Text = sb.ToString();
		}

		private int ScanForNumber(string line, string sanityCheckText)
		{
			// Sanity Check
			System.Diagnostics.Debug.Assert(line.Contains(sanityCheckText), "ScanForNumber Fails for line: " + line);

			string[] parts = line.Trim().Split(new string[] { ">", "<" }, StringSplitOptions.RemoveEmptyEntries);
			return int.Parse(parts[1].Trim(), NumberStyles.AllowThousands);

		}

		private string ScanForSite(string line)
		{
			if (!line.Contains(">")) {
				return line.Trim();
			}
			else {
				string[] parts = line.Trim().Split(new string[] { ">", "<" }, StringSplitOptions.RemoveEmptyEntries);
				return parts[1].Trim();
			}
		}

		private void LoadUrlAndCheckAccess()
		{
			webBrowser.Source = new Uri(AppSettings.projectSettings[currentProjectSettingIndex].TrafficUrl);
			webBrowser.LoadCompleted += (sender, e) => {
				HTMLDocument doc = webBrowser.Document as HTMLDocument;
				string innerHtml = doc.body.innerHTML;

				if (innerHtml.Contains("login_field")) {
					this.txtMain.Text = "Please log into GitHub, and then click the button to 'Try Again'. Be sure to complete secondary authentication. ";

					this.btnAction.IsEnabled = true;
					this.btnAction.Content = "Try Again";
					this.btnAction.Tag = "LoadUrlAndCheckAccess";
				}
				else if (innerHtml.Contains("TypeScript") && innerHtml.Contains("Visitors")) {
					this.txtMain.Text = "";
					this.btnAction.IsEnabled = false;
					this.btnAction.Content = AppSettings.AppCaption;
					this.btnAction.Tag = "";
				}
				else {
					System.Windows.MessageBox.Show("GitHub Page Changed! Updated is needed in LoadUrlAndCheckAccess", AppSettings.AppCaption, System.Windows.MessageBoxButton.OK);
				}
			};
		}

		private void MenuItemExit_Click(object sender, RoutedEventArgs e)
		{
			// TODO: Disable Exit if we are within 30 minutes of next update.

			if (MessageBox.Show("Attention: It is Critical that you restart this app ASAP!", AppSettings.AppCaption, MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
				this.Close();
			}
		}

		private void MenuItemQuickTest_Click(object sender, RoutedEventArgs e)
		{
			
		}

		private void btnAction_Click(object sender, RoutedEventArgs e)
		{
			string tag = (string)this.btnAction.Tag;
			if (tag == "LoadUrlAndCheckAccess") {
				LoadUrlAndCheckAccess();
			}
			else if (tag == "ManualCheckNow") {
				this.appTimer.Interval = this.defaultInterval;
				this.AppCurrentTask = CurrentTask.ProcessTopLists;
			}
			else if (tag == "") {
				MessageBox.Show("We have received your command. Click OK to dismiss this info.", AppSettings.AppCaption, MessageBoxButton.OK);
			}
			this.btnAction.Tag = "";
		}
	}
}
