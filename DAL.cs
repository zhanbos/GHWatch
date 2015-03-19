using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubWatch
{
	/// <summary>
	/// Data Access Layer
	/// </summary>
	internal static class DAL
	{
		private const string ConnectionString = "Data Source=SQL201516;Initial Catalog=GITHUB;Integrated Security=true;";

		public static void InsertIntoReferringSites(int dateId, string project, string site, int viewCount, int uniqueVisitorsCount)
		{
			using (SqlConnection connection = new SqlConnection(ConnectionString))
			{
				using (SqlCommand command = connection.CreateCommand())
				{
					command.CommandText = "INSERT INTO [dbo].[ReferringSites] ([DateId],[Project],[Site],[ViewsCount],[UniqueVisitorsCount]) " + 
										"VALUES (@DateId, @Project, @Site, @ViewsCount, @UniqueVisitorsCount)";

					command.Parameters.AddWithValue("@DateId", dateId);
					command.Parameters.AddWithValue("@Project", project);
					command.Parameters.AddWithValue("@Site", site);
					command.Parameters.AddWithValue("@ViewsCount", viewCount);
					command.Parameters.AddWithValue("@UniqueVisitorsCount", uniqueVisitorsCount);

					connection.Open();
					command.ExecuteNonQuery();
				}
			}
		}

		public static int QueryReferringSitesRecordCount(int dateId, string project)
		{
			using (SqlConnection connection = new SqlConnection(ConnectionString))
			{
				using (SqlCommand command = connection.CreateCommand())
				{
					command.CommandText = "SELECT COUNT(*) FROM [dbo].[ReferringSites] " +
										"WHERE DateId = @DateId AND Project = @Project";

					command.Parameters.AddWithValue("@DateId", dateId);
					command.Parameters.AddWithValue("@Project", project);

					connection.Open();
					return (int)command.ExecuteScalar();
				}
			}
		}


		public static void InsertIntoSummary(int dateId, string project, string category, long count)
		{
			using (SqlConnection connection = new SqlConnection(ConnectionString))
			{
				using (SqlCommand command = connection.CreateCommand())
				{
					command.CommandText = "INSERT INTO [dbo].[Summary] ([DateId],[Project],[Category],[Count]) " +
										"VALUES (@DateId, @Project, @Category, @Count)";

					command.Parameters.AddWithValue("@DateId", dateId);
					command.Parameters.AddWithValue("@Project", project);
					command.Parameters.AddWithValue("@Category", category);
					command.Parameters.AddWithValue("@Count", count);

					connection.Open();
					command.ExecuteNonQuery();
				}
			}
		}

		public static long QuerySummaryCount(int dateId, string project, string category)
		{
			using (SqlConnection connection = new SqlConnection(ConnectionString))
			{
				using (SqlCommand command = connection.CreateCommand())
				{
					command.CommandText = "SELECT [Count] FROM [dbo].[Summary] " +
										"WHERE DateId = @DateId AND Project = @Project AND Category = @Category";

					command.Parameters.AddWithValue("@DateId", dateId);
					command.Parameters.AddWithValue("@Project", project);
					command.Parameters.AddWithValue("@Category", category);

					connection.Open();

					SqlDataReader reader = command.ExecuteReader();

					long retValue = -1; //-1 means no record found
					while (reader.Read())
					{
						if (retValue == -1)
						{
							retValue = (long)reader[0];
						}
						else
						{
							System.Diagnostics.Debug.Fail("Investigate why QuerySummaryCount can get more than one result from DB!");
						}
					}
					reader.Close();
					return retValue;
				}
			}
		}

		public static void UpdateSummary(int dateId, string project, string category, long newCount)
		{
			using (SqlConnection connection = new SqlConnection(ConnectionString))
			{
				using (SqlCommand command = connection.CreateCommand())
				{
					command.CommandText = "UPDATE [dbo].[Summary] SET [Count] = @NewCount " +
										"WHERE DateId = @DateId AND Project = @Project AND Category = @Category";

					command.Parameters.AddWithValue("@DateId", dateId);
					command.Parameters.AddWithValue("@Project", project);
					command.Parameters.AddWithValue("@Category", category);
					command.Parameters.AddWithValue("@NewCount", newCount);

					connection.Open();
					command.ExecuteNonQuery();
				}
			}
		}

		public static void InsertIntoReleases(int dateId, string project, int releaseId, int downloadCount)
		{
			using (SqlConnection connection = new SqlConnection(ConnectionString)) {
				using (SqlCommand command = connection.CreateCommand()) {
					command.CommandText = "INSERT INTO [dbo].[Releases] ([DateId],[Project],[ReleaseId],[DownloadCount]) " +
										"VALUES (@DateId, @Project, @ReleaseId, @DownloadCount)";

					command.Parameters.AddWithValue("@DateId", dateId);
					command.Parameters.AddWithValue("@Project", project);
					command.Parameters.AddWithValue("@ReleaseId", releaseId);
					command.Parameters.AddWithValue("@DownloadCount", downloadCount);

					connection.Open();
					command.ExecuteNonQuery();
				}
			}
		}

		public static int QueryReleasesDownloadCount(int dateId, string project, int releaseId)
		{
			using (SqlConnection connection = new SqlConnection(ConnectionString)) {
				using (SqlCommand command = connection.CreateCommand()) {
					command.CommandText = "SELECT [DownloadCount] FROM [dbo].[Releases] " +
										"WHERE DateId = @DateId AND Project = @Project AND ReleaseId = @ReleaseId";

					command.Parameters.AddWithValue("@DateId", dateId);
					command.Parameters.AddWithValue("@Project", project);
					command.Parameters.AddWithValue("@ReleaseId", releaseId);

					connection.Open();

					SqlDataReader reader = command.ExecuteReader();

					int retValue = -1; //-1 means no record found
					while (reader.Read()) {
						if (retValue == -1) {
							retValue = (int)reader[0];
						}
						else {
							System.Diagnostics.Debug.Fail("Investigate why QueryReleasesDownloadCount can get more than one result from DB!");
						}
					}
					reader.Close();
					return retValue;
				}
			}
		}

		public static void UpdateReleasesDownloadCount(int dateId, string project, int releaseId, int newDownloadCount)
		{
			using (SqlConnection connection = new SqlConnection(ConnectionString)) {
				using (SqlCommand command = connection.CreateCommand()) {
					command.CommandText = "UPDATE [dbo].[Releases] SET [DownloadCount] = @NewDownloadCount " +
										"WHERE DateId = @DateId AND Project = @Project AND ReleaseId = @ReleaseId";

					command.Parameters.AddWithValue("@DateId", dateId);
					command.Parameters.AddWithValue("@Project", project);
					command.Parameters.AddWithValue("@ReleaseId", releaseId);
					command.Parameters.AddWithValue("@NewDownloadCount", newDownloadCount);

					connection.Open();
					command.ExecuteNonQuery();
				}
			}
		}


	}
}
