using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubWatch
{
	public class Count
	{
		public long bucket { get; set; }
		public long total { get; set; }
		public long unique { get; set; }
	}

	public class Summary
	{
		public int total { get; set; }
		public int unique { get; set; }
	}

	public class ActivityData
	{
		public List<Count> counts { get; set; }
		public Summary summary { get; set; }
	}

	// While TrafficData has the same definition as Activity Data, we define two classes for clarity purpose.
	public class TrafficData
	{
		public List<Count> counts { get; set; }
		public Summary summary { get; set; }
	}

}
