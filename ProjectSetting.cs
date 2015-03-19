using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubWatch
{
	/// <summary>
	/// For each GitHub Project, customize its settings
	/// </summary>
	internal class ProjectSetting
	{
		/// <summary>
		/// Project Name as seen in the project URL.
		/// </summary>
		public string Name { get; set; }

		private string trafficUrl = null;
		public string TrafficUrl
		{
			get
			{
				if (this.trafficUrl == null)
				{
					return "https://github.com/Microsoft/" + Name +"/graphs/traffic";
				}
				else
				{
					return this.trafficUrl;
				}
			}

			set
			{
				this.trafficUrl = value;
			}
		}

		/// <summary>
		/// If specified, this app will call GitHub API to get download_count for each relase ID.
		/// If null (default value), this app won't bother.
		/// </summary>
		public int[] DownloadCountReleaseIds
		{
			get;
			set;
		}

		public int ReferringSitesCount
		{
			get;
			set;
		}
	}
}
