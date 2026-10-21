
using DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Services
{
    public class TestimonialService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public TestimonialService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        // 1. Get all testimonials
        public async Task<List<TestimonialVM>> GetTestimonialsAsync(
            string ip)
        {
            var testimonials = new List<TestimonialVM>();

            try
            {
                using (var connection =
                    new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, ClientName, ReviewText, Rating,
                           DisplayOrder, Status, CreatedAt, UpdatedAt,
                           CreatedBy, UserHostAdd
                    FROM dbo.Testimonials
                    ORDER BY DisplayOrder, CreatedAt DESC;",
                    connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            testimonials.Add(MapTestimonial(reader));
                        }
                    }
                }

                return testimonials;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Testimonials", "GET", 0,
                    ip, "GetTestimonialsAsync");

                throw;
            }
        }

        // 2. Get testimonial by ID
        public async Task<TestimonialVM> GetTestimonialByIdAsync(
            Guid id, string ip)
        {
            try
            {
                using (var connection =
                    new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, ClientName, ReviewText, Rating,
                           DisplayOrder, Status, CreatedAt, UpdatedAt,
                           CreatedBy, UserHostAdd
                    FROM dbo.Testimonials
                    WHERE Id = @Id;",
                    connection))
                {
                    command.Parameters.Add(
                        "@Id", SqlDbType.UniqueIdentifier).Value = id;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapTestimonial(reader);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Testimonials", "GET", 0,
                    ip, "GetTestimonialByIdAsync");

                throw;
            }
        }

        // 3. Insert or update testimonial
        public async Task<Guid?> SaveTestimonialAsync(
            TestimonialVM model, string ip)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            try
            {
                using (var connection =
                    new SqlConnection(_connectionString))
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;

                    if (model.Id == Guid.Empty)
                    {
                        command.CommandText = @"
                            INSERT INTO dbo.Testimonials
                            (
                                ClientName, ReviewText, Rating,
                                DisplayOrder, Status, CreatedBy,
                                UserHostAdd
                            )
                            OUTPUT INSERTED.Id
                            VALUES
                            (
                                @ClientName, @ReviewText, @Rating,
                                @DisplayOrder, @Status, @CreatedBy,
                                @UserHostAdd
                            );";

                        command.Parameters.Add(
                            "@CreatedBy", SqlDbType.NVarChar, 100).Value =
                            string.IsNullOrWhiteSpace(model.CreatedBy)
                                ? (object)DBNull.Value
                                : model.CreatedBy.Trim();

                        command.Parameters.Add(
                            "@UserHostAdd", SqlDbType.NVarChar, 50).Value =
                            string.IsNullOrWhiteSpace(model.UserHostAdd)
                                ? (object)DBNull.Value
                                : model.UserHostAdd.Trim();
                    }
                    else
                    {
                        command.CommandText = @"
                            UPDATE dbo.Testimonials
                            SET ClientName = @ClientName,
                                ReviewText = @ReviewText,
                                Rating = @Rating,
                                DisplayOrder = @DisplayOrder,
                                Status = @Status,
                                UpdatedAt = SYSUTCDATETIME()
                            WHERE Id = @Id;

                            SELECT Id
                            FROM dbo.Testimonials
                            WHERE Id = @Id;";

                        command.Parameters.Add(
                            "@Id", SqlDbType.UniqueIdentifier).Value =
                            model.Id;
                    }

                    command.Parameters.Add(
                        "@ClientName", SqlDbType.NVarChar, 150).Value =
                        model.ClientName.Trim();

                    command.Parameters.Add(
                        "@ReviewText", SqlDbType.NVarChar, 1000).Value =
                        model.ReviewText.Trim();

                    command.Parameters.Add(
                        "@Rating", SqlDbType.Int).Value = model.Rating;

                    command.Parameters.Add(
                        "@DisplayOrder", SqlDbType.Int).Value =
                        model.DisplayOrder;

                    command.Parameters.Add(
                        "@Status", SqlDbType.Bit).Value = model.Status;

                    await connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();

                    if (result == null || result == DBNull.Value)
                        return null;

                    return (Guid)result;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Testimonials", "POST", 0,
                    ip, "SaveTestimonialAsync");

                throw;
            }
        }

        // 4. Delete testimonial
        public async Task<bool> DeleteTestimonialAsync(
            Guid id, string ip)
        {
            try
            {
                using (var connection =
                    new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    DELETE FROM dbo.Testimonials
                    WHERE Id = @Id;",
                    connection))
                {
                    command.Parameters.Add(
                        "@Id", SqlDbType.UniqueIdentifier).Value = id;

                    await connection.OpenAsync();

                    int affected = await command.ExecuteNonQueryAsync();

                    return affected > 0;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Testimonials", "DELETE", 0,
                    ip, "DeleteTestimonialAsync");

                throw;
            }
        }

        // 5. Map database row to DTO
        private TestimonialVM MapTestimonial(SqlDataReader reader)
        {
            return new TestimonialVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),

                ClientName = reader.GetString(
                    reader.GetOrdinal("ClientName")),

                ReviewText = reader.GetString(
                    reader.GetOrdinal("ReviewText")),

                Rating = reader.GetInt32(
                    reader.GetOrdinal("Rating")),

                DisplayOrder = reader.GetInt32(
                    reader.GetOrdinal("DisplayOrder")),

                Status = reader.GetBoolean(
                    reader.GetOrdinal("Status")),

                CreatedAt = reader.GetDateTime(
                    reader.GetOrdinal("CreatedAt")),

                UpdatedAt = reader.IsDBNull(
                    reader.GetOrdinal("UpdatedAt"))
                    ? (DateTime?)null
                    : reader.GetDateTime(
                        reader.GetOrdinal("UpdatedAt")),

                CreatedBy = reader.IsDBNull(
                    reader.GetOrdinal("CreatedBy"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal("CreatedBy")),

                UserHostAdd = reader.IsDBNull(
                    reader.GetOrdinal("UserHostAdd"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal("UserHostAdd"))
            };
        }
    }
}
