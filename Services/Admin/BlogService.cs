using DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Services.Admin
{
    public class BlogPage
    {
        public List<Blogs> Items { get; set; } = new List<Blogs>();
        public int Total { get; set; }      // all rows for the selected status
        public int Filtered { get; set; }   // rows matching status + search
    }

    public class BlogService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public BlogService(string connectionString, LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        private const string ListColumns = @"
            Id, [Type], Title, Heading, SubHeading, RewriteUrl, CoverImage,
            PublishedOn, Status, CreatedBy, CreatedAt, UpdatedAt, UserHostAdd";

        // 1. List (no Description - it can be large and the list does not show it)
        public async Task<List<Blogs>> GetBlogsAsync(string ip)
        {
            var blogs = new List<Blogs>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(
                    "SELECT " + ListColumns + @"
                     FROM dbo.Blogs
                     WHERE IsDeleted = 0
                     ORDER BY ISNULL(PublishedOn, CAST(CreatedAt AS DATE)) DESC, Id DESC;",
                    connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            blogs.Add(Map(reader, false));
                    }
                }

                return blogs;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, "Admin/Blogs", "GET", 0, ip, "GetBlogsAsync");
                throw;
            }
        }

        // 1b. One page of blogs, filtered / searched / sorted in SQL Server (needs SQL Server 2012+).
        // status: null = all, true = active, false = inactive.
        public async Task<BlogPage> GetBlogsPagedAsync(
            bool? status, string search, string sortBy, bool descending,
            int start, int length, string ip)
        {
            if (start < 0) start = 0;
            if (length < 1) length = 10;
            if (length > 100) length = 100;

            string dir = descending ? "DESC" : "ASC";
            string orderBy;

            // Whitelist only - never put user text into ORDER BY.
            switch ((sortBy ?? string.Empty).ToLowerInvariant())
            {
                case "id": orderBy = "Id " + dir; break;
                case "status": orderBy = "Status " + dir + ", Id DESC"; break;
                case "title": orderBy = "Title " + dir + ", Id DESC"; break;
                case "type": orderBy = "[Type] " + dir + ", Id DESC"; break;
                case "updated": orderBy = "UpdatedAt " + dir + ", Id DESC"; break;
                default:
                    orderBy = "ISNULL(PublishedOn, CAST(CreatedAt AS DATE)) " + dir + ", Id DESC";
                    break;
            }

            string term = null;
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                if (s.Length > 100) s = s.Substring(0, 100);

                // escape LIKE wildcards so "50%" or "a_b" are searched literally
                s = s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
                term = "%" + s + "%";
            }

            const string baseWhere = "IsDeleted = 0 AND (@Status IS NULL OR Status = @Status)";
            const string searchWhere = @"
                AND (@Search IS NULL
                     OR Title LIKE @Search ESCAPE '\'
                     OR Heading LIKE @Search ESCAPE '\'
                     OR [Type] LIKE @Search ESCAPE '\'
                     OR RewriteUrl LIKE @Search ESCAPE '\')";

            string sql = $@"
                SELECT COUNT(*) FROM dbo.Blogs WHERE {baseWhere};
                SELECT COUNT(*) FROM dbo.Blogs WHERE {baseWhere}{searchWhere};
                SELECT {ListColumns}
                FROM dbo.Blogs
                WHERE {baseWhere}{searchWhere}
                ORDER BY {orderBy}
                OFFSET @Start ROWS FETCH NEXT @Length ROWS ONLY;";

            var page = new BlogPage();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Status", SqlDbType.Bit).Value =
                        status.HasValue ? (object)status.Value : DBNull.Value;
                    command.Parameters.Add("@Search", SqlDbType.NVarChar, 300).Value =
                        term == null ? (object)DBNull.Value : term;
                    command.Parameters.Add("@Start", SqlDbType.Int).Value = start;
                    command.Parameters.Add("@Length", SqlDbType.Int).Value = length;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            page.Total = reader.GetInt32(0);

                        await reader.NextResultAsync();
                        if (await reader.ReadAsync())
                            page.Filtered = reader.GetInt32(0);

                        await reader.NextResultAsync();
                        while (await reader.ReadAsync())
                            page.Items.Add(Map(reader, false));
                    }
                }

                return page;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, "Admin/Blogs", "GET", 0, ip, "GetBlogsPagedAsync");
                throw;
            }
        }

        // 2. Single blog (with Description)
        public async Task<Blogs> GetBlogByIdAsync(int id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(
                    "SELECT " + ListColumns + @", [Description]
                     FROM dbo.Blogs
                     WHERE Id = @Id AND IsDeleted = 0;",
                    connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            return Map(reader, true);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, "Admin/Blogs", "GET", id, ip, "GetBlogByIdAsync");
                throw;
            }
        }

        // 3. Insert or update. Returns the blog Id, or null if nothing was saved.
        // Throws ArgumentException for validation problems (e.g. duplicate slug).
        public async Task<int?> SaveBlogAsync(Blogs model, string ip)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            string slug = Slugify(string.IsNullOrWhiteSpace(model.RewriteUrl)
                ? model.Title
                : model.RewriteUrl);

            if (string.IsNullOrEmpty(slug))
                throw new ArgumentException("Please enter a title or URL slug that contains letters or numbers.");

            model.RewriteUrl = slug;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Friendly duplicate-slug message (the unique index is the final guard).
                    using (var check = new SqlCommand(@"
                        SELECT 1 FROM dbo.Blogs
                        WHERE RewriteUrl = @Slug AND IsDeleted = 0 AND Id <> @Id;",
                        connection))
                    {
                        check.Parameters.Add("@Slug", SqlDbType.NVarChar, 300).Value = slug;
                        check.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;

                        if (await check.ExecuteScalarAsync() != null)
                            throw new ArgumentException("This URL slug is already used by another blog. Please choose a different one.");
                    }

                    using (var command = new SqlCommand())
                    {
                        command.Connection = connection;

                        if (model.Id <= 0)
                        {
                            command.CommandText = @"
                                INSERT INTO dbo.Blogs
                                    ([Type], Title, Heading, SubHeading, [Description], RewriteUrl,
                                     CoverImage, PublishedOn, Status, CreatedBy, UserHostAdd)
                                OUTPUT INSERTED.Id
                                VALUES
                                    (@Type, @Title, @Heading, @SubHeading, @Description, @RewriteUrl,
                                     @CoverImage, @PublishedOn, @Status, @CreatedBy, @UserHostAdd);";

                            command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100).Value = DbValue(model.CreatedBy);
                            command.Parameters.Add("@UserHostAdd", SqlDbType.NVarChar, 50).Value = DbValue(model.UserHostAdd ?? ip);
                        }
                        else
                        {
                            command.CommandText = @"
                                UPDATE dbo.Blogs
                                SET [Type] = @Type,
                                    Title = @Title,
                                    Heading = @Heading,
                                    SubHeading = @SubHeading,
                                    [Description] = @Description,
                                    RewriteUrl = @RewriteUrl,
                                    CoverImage = @CoverImage,
                                    PublishedOn = @PublishedOn,
                                    Status = @Status,
                                    UpdatedAt = SYSUTCDATETIME()
                                WHERE Id = @Id AND IsDeleted = 0;

                                SELECT CASE WHEN @@ROWCOUNT > 0 THEN @Id ELSE NULL END;";

                            command.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
                        }

                        command.Parameters.Add("@Type", SqlDbType.NVarChar, 100).Value = DbValue(model.Type);
                        command.Parameters.Add("@Title", SqlDbType.NVarChar, 250).Value = model.Title.Trim();
                        command.Parameters.Add("@Heading", SqlDbType.NVarChar, 250).Value = DbValue(model.Heading);
                        command.Parameters.Add("@SubHeading", SqlDbType.NVarChar, 500).Value = DbValue(model.SubHeading);
                        command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value =
                            string.IsNullOrWhiteSpace(model.Description) ? (object)DBNull.Value : model.Description;
                        command.Parameters.Add("@RewriteUrl", SqlDbType.NVarChar, 300).Value = slug;
                        command.Parameters.Add("@CoverImage", SqlDbType.NVarChar, 1000).Value = DbValue(model.CoverImage);
                        command.Parameters.Add("@PublishedOn", SqlDbType.Date).Value =
                            model.PublishedOn.HasValue ? (object)model.PublishedOn.Value.Date : DBNull.Value;
                        command.Parameters.Add("@Status", SqlDbType.Bit).Value = model.Status;

                        var result = await command.ExecuteScalarAsync();

                        if (result == null || result == DBNull.Value)
                            return null;

                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (ArgumentException)
            {
                throw; // validation message for the user, not a system error
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, "Admin/Blogs", "POST", model.Id, ip, "SaveBlogAsync");
                throw;
            }
        }

        // 4. Active / InActive
        public async Task<bool> SetStatusAsync(int id, bool status, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    UPDATE dbo.Blogs
                    SET Status = @Status, UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @Id AND IsDeleted = 0;",
                    connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    command.Parameters.Add("@Status", SqlDbType.Bit).Value = status;

                    await connection.OpenAsync();
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, "Admin/Blogs", "POST", id, ip, "SetStatusAsync");
                throw;
            }
        }

        // 5. Soft delete (row stays in the table with IsDeleted = 1)
        public async Task<bool> DeleteBlogAsync(int id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    UPDATE dbo.Blogs
                    SET IsDeleted = 1, Status = 0, UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @Id AND IsDeleted = 0;",
                    connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                    await connection.OpenAsync();
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, "Admin/Blogs", "DELETE", id, ip, "DeleteBlogAsync");
                throw;
            }
        }

        // ---------- helpers ----------

        public static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // strip accents (é -> e)
            string normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            string slug = Regex.Replace(sb.ToString(), "[^a-z0-9]+", "-").Trim('-');

            return slug.Length > 300 ? slug.Substring(0, 300).Trim('-') : slug;
        }

        private static object DbValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static string Str(SqlDataReader reader, string column)
        {
            int i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? null : reader.GetString(i);
        }

        private static Blogs Map(SqlDataReader reader, bool withDescription)
        {
            DateTime createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
            DateTime updatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
            int published = reader.GetOrdinal("PublishedOn");

            return new Blogs
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Type = Str(reader, "Type"),
                Title = Str(reader, "Title"),
                Heading = Str(reader, "Heading"),
                SubHeading = Str(reader, "SubHeading"),
                Description = withDescription ? Str(reader, "Description") : null,
                RewriteUrl = Str(reader, "RewriteUrl"),
                CoverImage = Str(reader, "CoverImage"),
                PublishedOn = reader.IsDBNull(published) ? (DateTime?)null : reader.GetDateTime(published),
                Status = reader.GetBoolean(reader.GetOrdinal("Status")),
                CreatedBy = Str(reader, "CreatedBy"),
                UserHostAdd = Str(reader, "UserHostAdd"),
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                CreatedOnString = createdAt.ToString("MMM-dd-yyyy"),
                EditedOnString = updatedAt.ToString("MMM-dd-yyyy HH:mm")
            };
        }
    }
}