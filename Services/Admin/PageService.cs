
using DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Services
{
    public class PageService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public PageService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        // Get all pages
        public async Task<List<PageVM>> GetPagesAsync(string ip)
        {
            var pages = new List<PageVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, Title, Slug, MetaTitle, MetaDescription,
                           MetaKeywords, CanonicalUrl, Status,IsNav,
                           DisplayOrder, CreatedAt, UpdatedAt
                    FROM dbo.Pages
                    ORDER BY DisplayOrder, Title;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            pages.Add(MapPage(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Pages", "GET", 0, ip, "GetPages");
                throw;
            }

            return pages;
        }

        // Get one page by ID
        public async Task<PageVM> GetPageByIdAsync(Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, Title, Slug, MetaTitle, MetaDescription,
                           MetaKeywords, CanonicalUrl, Status, IsNav,
                           DisplayOrder, CreatedAt, UpdatedAt
                    FROM dbo.Pages
                    WHERE Id = @Id;", connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                        .Value = id;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            return MapPage(reader);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Pages", "GET", 0, ip, "GetPageById");
                throw;
            }
        }

        // Insert or update a page
        public async Task<bool> SavePageAsync(PageVM page, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;

                    if (page.Id == Guid.Empty)
                    {
                        command.CommandText = @"
                            INSERT INTO dbo.Pages
                            (
                                Title, Slug, MetaTitle, MetaDescription,
                                MetaKeywords, CanonicalUrl, Status, IsNav,
                                DisplayOrder
                            )
                            VALUES
                            (
                                @Title, @Slug, @MetaTitle, @MetaDescription,
                                @MetaKeywords, @CanonicalUrl, @Status, @IsNav,
                                @DisplayOrder
                            );";
                    }
                    else
                    {
                         command.CommandText = @"
                            UPDATE dbo.Pages
                            SET Title = @Title,
                                Slug = @Slug,
                                MetaTitle = @MetaTitle,
                                MetaDescription = @MetaDescription,
                                MetaKeywords = @MetaKeywords,
                                CanonicalUrl = @CanonicalUrl,
                                Status = @Status,
                                IsNav = @IsNav,
                                DisplayOrder = @DisplayOrder,
                                UpdatedAt = SYSUTCDATETIME()
                            WHERE Id = @Id;";

                        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                            .Value = page.Id;
                    }

                    command.Parameters.Add("@Title", SqlDbType.NVarChar, 200)
                        .Value = page.Title.Trim();

                    command.Parameters.Add("@Slug", SqlDbType.NVarChar, 250)
                        .Value = page.Slug.Trim().ToLowerInvariant();

                    command.Parameters.Add("@MetaTitle", SqlDbType.NVarChar, 200)
                        .Value = (object)page.MetaTitle ?? DBNull.Value;

                    command.Parameters.Add("@MetaDescription", SqlDbType.NVarChar, 500)
                        .Value = (object)page.MetaDescription ?? DBNull.Value;

                    command.Parameters.Add("@MetaKeywords", SqlDbType.NVarChar, 1000)
                        .Value = (object)page.MetaKeywords ?? DBNull.Value;

                    command.Parameters.Add("@CanonicalUrl", SqlDbType.NVarChar, 500)
                        .Value = (object)page.CanonicalUrl ?? DBNull.Value;

                    command.Parameters.Add("@Status", SqlDbType.Bit)
                        .Value = page.Status;
                    command.Parameters.Add("@IsNav", SqlDbType.Bit)
    .Value = page.IsNav;

                    command.Parameters.Add("@DisplayOrder", SqlDbType.Int)
                        .Value = page.DisplayOrder;

                    await connection.OpenAsync();

                    int affectedRows = await command.ExecuteNonQueryAsync();

                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Pages", "POST", 0, ip, "SavePage");
                throw;
            }
        }

        // Delete a page
        public async Task<bool> DeletePageAsync(Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    DELETE FROM dbo.Pages
                    WHERE Id = @Id;", connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                        .Value = id;

                    await connection.OpenAsync();

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Pages", "DELETE", 0, ip, "DeletePage");
                throw;
            }
        }

        private static PageVM MapPage(SqlDataReader reader)
        {
            return new PageVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Title = reader["Title"].ToString(),
                Slug = reader["Slug"].ToString(),
                MetaTitle = reader["MetaTitle"] as string,
                MetaDescription = reader["MetaDescription"] as string,
                MetaKeywords = reader["MetaKeywords"] as string,
                CanonicalUrl = reader["CanonicalUrl"] as string,
                Status = Convert.ToBoolean(reader["Status"]),
                IsNav = Convert.ToBoolean(reader["IsNav"]),
                DisplayOrder = Convert.ToInt32(reader["DisplayOrder"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                UpdatedAt = reader["UpdatedAt"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(reader["UpdatedAt"])
            };
        }
    }
}
