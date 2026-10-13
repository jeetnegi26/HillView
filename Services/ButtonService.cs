
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using DTOs;

namespace Services
{
    public class ButtonService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public ButtonService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        public async Task<List<ButtonVM>> GetButtonsAsync(string ip)
        {
            var buttons = new List<ButtonVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, SectionId, SectionItemId, Title, Url,
                           Target, CssClass, ButtonType, DisplayOrder,
                           Status, CreatedAt, UpdatedAt
                    FROM Buttons
                    ORDER BY DisplayOrder, CreatedAt DESC;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            buttons.Add(MapButton(reader));
                        }
                    }
                }

                return buttons;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Buttons", "GET", 0, ip, "GetButtonsAsync");

                throw;
            }
        }

        public async Task<ButtonVM> GetButtonByIdAsync(Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, SectionId, SectionItemId, Title, Url,
                           Target, CssClass, ButtonType, DisplayOrder,
                           Status, CreatedAt, UpdatedAt
                    FROM Buttons
                    WHERE Id = @Id;", connection))
                {
                    command.Parameters.Add(
                        "@Id", SqlDbType.UniqueIdentifier).Value = id;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapButton(reader);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/ButtonAction", "GET", 0, ip,
                    "GetButtonByIdAsync");

                throw;
            }
        }

        public async Task<bool> SaveButtonAsync(ButtonVM model, string ip)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            bool hasSection = model.SectionId.HasValue &&
                              model.SectionId.Value != Guid.Empty;

            bool hasSectionItem = model.SectionItemId.HasValue &&
                                  model.SectionItemId.Value != Guid.Empty;

            if (hasSection == hasSectionItem)
            {
                throw new ArgumentException(
                    "Select either a section or a section item, but not both.");
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;

                    if (model.Id == Guid.Empty)
                    {
                        command.CommandText = @"
                            INSERT INTO Buttons
                            (
                                SectionId, SectionItemId, Title, Url,
                                Target, CssClass, ButtonType,
                                DisplayOrder, Status
                            )
                            VALUES
                            (
                                @SectionId, @SectionItemId, @Title, @Url,
                                @Target, @CssClass, @ButtonType,
                                @DisplayOrder, @Status
                            );";
                    }
                    else
                    {
                        command.CommandText = @"
                            UPDATE Buttons
                            SET SectionId = @SectionId,
                                SectionItemId = @SectionItemId,
                                Title = @Title,
                                Url = @Url,
                                Target = @Target,
                                CssClass = @CssClass,
                                ButtonType = @ButtonType,
                                DisplayOrder = @DisplayOrder,
                                Status = @Status,
                                UpdatedAt = SYSUTCDATETIME()
                            WHERE Id = @Id;";

                        command.Parameters.Add(
                            "@Id", SqlDbType.UniqueIdentifier).Value = model.Id;
                    }

                    command.Parameters.Add(
                        "@SectionId", SqlDbType.UniqueIdentifier).Value =
                        hasSection ? (object)model.SectionId.Value : DBNull.Value;

                    command.Parameters.Add(
                        "@SectionItemId", SqlDbType.UniqueIdentifier).Value =
                        hasSectionItem ? (object)model.SectionItemId.Value : DBNull.Value;

                    AddTextParameter(command, "@Title", model.Title, 200);
                    AddTextParameter(command, "@Url", model.Url, 1000);
                    AddTextParameter(command, "@Target", model.Target, 50);
                    AddTextParameter(command, "@CssClass", model.CssClass, 200);
                    AddTextParameter(command, "@ButtonType", model.ButtonType, 100);

                    command.Parameters.Add(
                        "@DisplayOrder", SqlDbType.Int).Value = model.DisplayOrder;

                    command.Parameters.Add(
                        "@Status", SqlDbType.Bit).Value = model.Status;

                    await connection.OpenAsync();

                    int affected = await command.ExecuteNonQueryAsync();

                    return affected > 0;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/ButtonAction", "POST", 0, ip,
                    "SaveButtonAsync");

                throw;
            }
        }

        public async Task<bool> DeleteButtonAsync(Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(
                    "DELETE FROM Buttons WHERE Id = @Id;", connection))
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
                    ex, "Admin/Buttons", "DELETE", 0, ip,
                    "DeleteButtonAsync");

                throw;
            }
        }

        public async Task<List<SectionVM>> GetSectionsAsync(string ip)
        {
            var sections = new List<SectionVM>();

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
                            sections.Add(new SectionVM
                            {
                                Id = reader.GetGuid(0),
                                Title = reader.GetString(1)
                            });
                        }
                    }
                }

                return sections;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/ButtonAction", "GET", 0, ip,
                    "GetSectionsAsync");

                throw;
            }
        }

        public async Task<List<SectionItemVM>> GetSectionItemsAsync(string ip)
        {
            var items = new List<SectionItemVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, Title
                    FROM SectionItems
                    WHERE Status = 1
                    ORDER BY Title;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(new SectionItemVM
                            {
                                Id = reader.GetGuid(0),
                                Title = reader.IsDBNull(1)
                                    ? "(Untitled item)"
                                    : reader.GetString(1)
                            });
                        }
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/ButtonAction", "GET", 0, ip,
                    "GetSectionItemsAsync");

                throw;
            }
        }

        private static ButtonVM MapButton(SqlDataReader reader)
        {
            return new ButtonVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                SectionId = GetGuid(reader, "SectionId"),
                SectionItemId = GetGuid(reader, "SectionItemId"),
                Title = GetString(reader, "Title"),
                Url = GetString(reader, "Url"),
                Target = GetString(reader, "Target"),
                CssClass = GetString(reader, "CssClass"),
                ButtonType = GetString(reader, "ButtonType"),
                DisplayOrder = reader.GetInt32(
                    reader.GetOrdinal("DisplayOrder")),
                Status = reader.GetBoolean(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                    ? (DateTime?)null
                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        private static Guid? GetGuid(SqlDataReader reader, string column)
        {
            int ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(ordinal)
                ? (Guid?)null
                : reader.GetGuid(ordinal);
        }

        private static string GetString(SqlDataReader reader, string column)
        {
            int ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetString(ordinal);
        }

        private static void AddTextParameter(
            SqlCommand command, string name, string value, int size)
        {
            var parameter = command.Parameters.Add(
                name, SqlDbType.NVarChar, size);

            parameter.Value = (object)value ?? DBNull.Value;
        }
    }
}
