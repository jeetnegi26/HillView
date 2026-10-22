using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using DTOs;

namespace Services
{
    public class SectionService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public SectionService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        public async Task<List<SectionVM>> GetSectionsAsync(string ip)
        {
            var sections = new List<SectionVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT
                        Id, SectionTypeId, Title, Heading, SubHeading,
                        Description, InternalName, BackgroundColor,
                        CssClass, SettingsJson, Status, CreatedAt, UpdatedAt
                    FROM dbo.Sections
                    ORDER BY CreatedAt DESC;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            sections.Add(MapSection(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Sections", "GET", 0, ip, "GetSections");

                throw;
            }

            return sections;
        }

        public async Task<SectionVM> GetSectionByIdAsync(Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT
                        Id, SectionTypeId, Title, Heading, SubHeading,
                        Description, InternalName, BackgroundColor,
                        CssClass, SettingsJson, Status, CreatedAt, UpdatedAt
                    FROM dbo.Sections
                    WHERE Id = @Id;", connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapSection(reader);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionAction", "GET", 0, ip, "GetSectionById");

                throw;
            }
        }

        public async Task<bool> SaveSectionAsync(SectionVM model, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string sql;

                    if (model.Id == Guid.Empty)
                    {
                        sql = @"
                            INSERT INTO dbo.Sections
                            (
                                SectionTypeId, Title, Heading, SubHeading,
                                Description, InternalName, BackgroundColor,
                                CssClass, SettingsJson, Status
                            )
                            VALUES
                            (
                                @SectionTypeId, @Title, @Heading, @SubHeading,
                                @Description, @InternalName, @BackgroundColor,
                                @CssClass, @SettingsJson, @Status
                            );";
                    }
                    else
                    {
                        sql = @"
                            UPDATE dbo.Sections
                            SET
                                SectionTypeId = @SectionTypeId,
                                Title = @Title,
                                Heading = @Heading,
                                SubHeading = @SubHeading,
                                Description = @Description,
                                InternalName = @InternalName,
                                BackgroundColor = @BackgroundColor,
                                CssClass = @CssClass,
                                SettingsJson = @SettingsJson,
                                Status = @Status,
                                UpdatedAt = SYSUTCDATETIME()
                            WHERE Id = @Id;";
                    }

                    using (var command = new SqlCommand(sql, connection))
                    {
                        if (model.Id != Guid.Empty)
                        {
                            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                                .Value = model.Id;
                        }

                        command.Parameters.Add("@SectionTypeId", SqlDbType.Int)
                            .Value = model.SectionTypeId;

                        command.Parameters.Add("@Title", SqlDbType.NVarChar, 200)
                            .Value = model.Title;

                        AddNullableParameter(command, "@Heading",
                            SqlDbType.NVarChar, 200, model.Heading);

                        AddNullableParameter(command, "@SubHeading",
                            SqlDbType.NVarChar, 200, model.SubHeading);

                        AddNullableParameter(command, "@Description",
                            SqlDbType.NVarChar, -1, model.Description);

                        AddNullableParameter(command, "@InternalName",
                            SqlDbType.NVarChar, 100, model.InternalName);

                        AddNullableParameter(command, "@BackgroundColor",
                            SqlDbType.NVarChar, 50, model.BackgroundColor);

                        AddNullableParameter(command, "@CssClass",
                            SqlDbType.NVarChar, 200, model.CssClass);

                        AddNullableParameter(command, "@SettingsJson",
                            SqlDbType.NVarChar, -1, model.SettingsJson);

                        command.Parameters.Add("@Status", SqlDbType.Bit)
                            .Value = model.Status;

                        await connection.OpenAsync();

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionAction", "POST", 0, ip, "SaveSection");

                throw;
            }
        }

        public async Task<bool> DeleteSectionAsync(Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(
                    "DELETE FROM dbo.Sections WHERE Id = @Id;", connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                        .Value = id;

                    await connection.OpenAsync();

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Sections", "DELETE", 0, ip, "DeleteSection");

                throw;
            }
        }

        public async Task<List<SectionTypeVM>> GetSectionTypesAsync(string ip)
        {
            var sectionTypes = new List<SectionTypeVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, Name, Title, Description, Status, DisplayOrder
                    FROM dbo.SectionTypes
                    WHERE Status = 1
                    ORDER BY DisplayOrder, Title;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            sectionTypes.Add(new SectionTypeVM
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("Description")),
                                Status = reader.GetBoolean(reader.GetOrdinal("Status")),
                                DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder"))
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionAction", "GET", 0, ip, "GetSectionTypes");

                throw;
            }

            return sectionTypes;
        }

        private static SectionVM MapSection(SqlDataReader reader)
        {
            return new SectionVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                SectionTypeId = reader.GetInt32(reader.GetOrdinal("SectionTypeId")),
                Title = reader.GetString(reader.GetOrdinal("Title")),

                Heading = reader.IsDBNull(reader.GetOrdinal("Heading"))
                    ? null : reader.GetString(reader.GetOrdinal("Heading")),

                SubHeading = reader.IsDBNull(reader.GetOrdinal("SubHeading"))
                    ? null : reader.GetString(reader.GetOrdinal("SubHeading")),

                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null : reader.GetString(reader.GetOrdinal("Description")),

                InternalName = reader.IsDBNull(reader.GetOrdinal("InternalName"))
                    ? null : reader.GetString(reader.GetOrdinal("InternalName")),

                BackgroundColor = reader.IsDBNull(reader.GetOrdinal("BackgroundColor"))
                    ? null : reader.GetString(reader.GetOrdinal("BackgroundColor")),

                CssClass = reader.IsDBNull(reader.GetOrdinal("CssClass"))
                    ? null : reader.GetString(reader.GetOrdinal("CssClass")),

                SettingsJson = reader.IsDBNull(reader.GetOrdinal("SettingsJson"))
                    ? null : reader.GetString(reader.GetOrdinal("SettingsJson")),

                Status = reader.GetBoolean(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),

                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                    ? (DateTime?)null
                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        private static void AddNullableParameter(
            SqlCommand command,
            string name,
            SqlDbType type,
            int size,
            string value)
        {
            var parameter = command.Parameters.Add(name, type);

            if (size != 0)
            {
                parameter.Size = size;
            }

            parameter.Value = string.IsNullOrWhiteSpace(value)
                ? (object)DBNull.Value
                : value;
        }
    }
}