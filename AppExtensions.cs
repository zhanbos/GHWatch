using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubWatch
{
	public static class AppExtensions
	{
		public static int ToDateIDFromBucket(this long bucket)
		{
			// The number of milliseconds between midnight, January 1, 1970, and the current date and time.
			long ticks = bucket * 1000;

			DateTime dt = new DateTime(1970, 1, 1).AddMilliseconds(ticks);

			return dt.ToDateIDFromDateTime();
		}

		public static int ToDateIDFromDateTime(this DateTime dt)
		{
			return dt.Year * 10000 + dt.Month * 100 + dt.Day;
		}
	}

}
