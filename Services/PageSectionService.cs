
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using DTOs;

namespace Services
{
    public class PageSectionService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public PageSectionService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        public async Task<List<PageSectionVM>> GetPageSectionsAsync(string ip)
        {
            var result = new List<PageSectionVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT
                        ps.Id,
                        ps.PageId,
                        ps.SectionId,
                        p.Title AS PageTitle,
                        s.Title AS SectionTitle,
                        ps.DisplayOrder,
                        ps.Status,
                        ps.CreatedAt,
                        ps.UpdatedAt
                    FROM PageSections ps
                    INNER JOIN Pages p ON p.Id = ps.PageId
                    INNER JOIN Sections s ON s.Id = ps.SectionId
                    ORDER BY p.Title, ps.DisplayOrder, s.Title;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(MapPageSection(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, nameof(GetPageSectionsAsync), ip);
                throw;
            }

            return result;
        }

        public async Task<PageSectionVM> GetPageSectionByIdAsync(
            Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT
                        ps.Id,
                        ps.PageId,
                        ps.SectionId,
                        p.Title AS PageTitle,
                        s.Title AS SectionTitle,
                        ps.DisplayOrder,
                        ps.Status,
                        ps.CreatedAt,
                        ps.UpdatedAt
                    FROM PageSections ps
                    INNER JOIN Pages p ON p.Id = ps.PageId
                    INNER JOIN Sections s ON s.Id = ps.SectionId
                    WHERE ps.Id = @Id;", connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                        .Value = id;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapPageSection(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, nameof(GetPageSectionByIdAsync), ip);
                throw;
            }

            return null;
        }

        public async Task<bool> SavePageSectionAsync(
            PageSectionVM model, string ip)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (model.PageId == Guid.Empty)
                throw new ArgumentException("Please select a page.");

            if (model.SectionId == Guid.Empty)
                throw new ArgumentException("Please select a section.");

            if (model.DisplayOrder < 0)
                throw new ArgumentException(
                    "Display order cannot be negative.");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    IF EXISTS (
                        SELECT 1
                        FROM PageSections
                        WHERE Id = @Id
                    )
                    BEGIN
                        UPDATE PageSections
                        SET
                            PageId = @PageId,
                            SectionId = @SectionId,
                            DisplayOrder = @DisplayOrder,
                            Status = @Status,
                            UpdatedAt = GETDATE()
                        WHERE Id = @Id;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO PageSections
                        (
                            Id,
                            PageId,
                            SectionId,
                            DisplayOrder,
                            Status,
                            CreatedAt,
                            UpdatedAt
                        )
                        VALUES
                        (
                            @Id,
                            @PageId,
                            @SectionId,
                            @DisplayOrder,
                            @Status,
                            GETDATE(),
                            NULL
                        );
                    END;", connection))
                {
                    if (model.Id == Guid.Empty)
                        model.Id = Guid.NewGuid();

                    command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                        .Value = model.Id;

                    command.Parameters.Add("@PageId", SqlDbType.UniqueIdentifier)
                        .Value = model.PageId;

                    command.Parameters.Add("@SectionId", SqlDbType.UniqueIdentifier)
                        .Value = model.SectionId;

                    command.Parameters.Add("@DisplayOrder", SqlDbType.Int)
                        .Value = model.DisplayOrder;

                    command.Parameters.Add("@Status", SqlDbType.Bit)
                        .Value = model.Status;

                    await connection.OpenAsync();

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, nameof(SavePageSectionAsync), ip);
                throw;
            }
        }

        public async Task<bool> DeletePageSectionAsync(Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    DELETE FROM PageSections
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
                await LogErrorAsync(ex, nameof(DeletePageSectionAsync), ip);
                throw;
            }
        }

        public async Task<List<PageVM>> GetPagesAsync(string ip)
        {
            var result = new List<PageVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, Title
                    FROM Pages
                    WHERE Status = 1
                    ORDER BY Title;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new PageVM
                            {
                                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                Title = reader["Title"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, nameof(GetPagesAsync), ip);
                throw;
            }

            return result;
        }

        public async Task<List<SectionVM>> GetSectionsAsync(string ip)
        {
            var result = new List<SectionVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, Title
                    FROM Sections
                    WHERE Status = 1
                    ORDER BY Title;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new SectionVM
                            {
                                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                Title = reader["Title"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, nameof(GetSectionsAsync), ip);
                throw;
            }

            return result;
        }

        private static PageSectionVM MapPageSection(SqlDataReader reader)
        {
            return new PageSectionVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                PageId = reader.GetGuid(reader.GetOrdinal("PageId")),
                SectionId = reader.GetGuid(reader.GetOrdinal("SectionId")),
                PageTitle = reader["PageTitle"].ToString(),
                SectionTitle = reader["SectionTitle"].ToString(),
                DisplayOrder = Convert.ToInt32(reader["DisplayOrder"]),
                Status = Convert.ToBoolean(reader["Status"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                UpdatedAt = reader["UpdatedAt"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(reader["UpdatedAt"])
            };
        }

        private async Task LogErrorAsync(
            Exception ex, string method, string ip)
        {
             _logErrorService.LogError(
                ex,
                RequestUrlPlaceholder(),
                method,
                0,
                ip,
                null);
        }

        private static string RequestUrlPlaceholder()
        {
            return "PageSectionService";
        }
    }
}
