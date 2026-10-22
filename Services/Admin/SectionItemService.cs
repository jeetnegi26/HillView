
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using DTOs;

namespace Services
{
    public class SectionItemService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public SectionItemService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        // Get top-level section items only.
        public async Task<List<SectionItemVM>> GetSectionItemsAsync(string ip)
        {
            var items = new List<SectionItemVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, SectionId, ParentSectionItemId,
                           Title, Heading, SubHeading, Description,
                           Value, Label, LinkUrl, AdditionalData,
                           DisplayOrder, Status, CreatedAt, UpdatedAt
                    FROM SectionItems
                    WHERE ParentSectionItemId IS NULL
                    ORDER BY CreatedAt DESC;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            items.Add(MapSectionItem(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionItems", "GET", 0, ip,
                    "GetSectionItemsAsync");
                throw;
            }

            return items;
        }

        public async Task<SectionItemVM> GetSectionItemByIdAsync(
            Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, SectionId, ParentSectionItemId,
                           Title, Heading, SubHeading, Description,
                           Value, Label, LinkUrl, AdditionalData,
                           DisplayOrder, Status, CreatedAt, UpdatedAt
                    FROM SectionItems
                    WHERE Id = @Id;", connection))
                {
                    command.Parameters.Add(
                        "@Id", SqlDbType.UniqueIdentifier).Value = id;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            return MapSectionItem(reader);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionItemAction", "GET", 0,
                    ip, "GetSectionItemByIdAsync");
                throw;
            }
        }

        // Get child items belonging to a particular parent item.
        public async Task<List<SectionItemVM>> GetChildSectionItemsAsync(
            Guid parentSectionItemId, string ip)
        {
            var items = new List<SectionItemVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, SectionId, ParentSectionItemId,
                           Title, Heading, SubHeading, Description,
                           Value, Label, LinkUrl, AdditionalData,
                           DisplayOrder, Status, CreatedAt, UpdatedAt
                    FROM SectionItems
                    WHERE ParentSectionItemId = @ParentSectionItemId
                    ORDER BY DisplayOrder, CreatedAt;", connection))
                {
                    command.Parameters.Add(
                        "@ParentSectionItemId",
                        SqlDbType.UniqueIdentifier).Value = parentSectionItemId;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            items.Add(MapSectionItem(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionItemChildren", "GET", 0,
                    ip, "GetChildSectionItemsAsync");
                throw;
            }

            return items;
        }

        public async Task<Guid?> SaveSectionItemAsync(
            SectionItemVM model, string ip)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction =
                        connection.BeginTransaction())
                    {
                        try
                        {
                            // Validate that a parent exists in the same
                            // section and is itself a top-level item.
                            if (model.ParentSectionItemId.HasValue)
                            {
                                if (model.ParentSectionItemId.Value == Guid.Empty ||
                                    model.ParentSectionItemId.Value == model.Id)
                                {
                                    throw new InvalidOperationException(
                                        "The selected parent item is invalid.");
                                }

                                using (var validateCommand = new SqlCommand(@"
                                    SELECT COUNT(1)
                                    FROM SectionItems
                                    WHERE Id = @ParentId
                                      AND SectionId = @SectionId
                                      AND ParentSectionItemId IS NULL;",
                                    connection, transaction))
                                {
                                    validateCommand.Parameters.Add(
                                        "@ParentId",
                                        SqlDbType.UniqueIdentifier).Value =
                                        model.ParentSectionItemId.Value;

                                    validateCommand.Parameters.Add(
                                        "@SectionId",
                                        SqlDbType.UniqueIdentifier).Value =
                                        model.SectionId;

                                    int validParent = Convert.ToInt32(
                                        await validateCommand.ExecuteScalarAsync());

                                    if (validParent == 0)
                                    {
                                        throw new InvalidOperationException(
                                            "The parent item must exist in the same section and must be a top-level item.");
                                    }
                                }
                            }

                            bool isNew = model.Id == Guid.Empty;
                            Guid savedId;

                            using (var command = new SqlCommand())
                            {
                                command.Connection = connection;
                                command.Transaction = transaction;

                                if (isNew)
                                {
                                    command.CommandText = @"
                                        INSERT INTO SectionItems
                                        (
                                            SectionId, ParentSectionItemId,
                                            Title, Heading, SubHeading,
                                            Description, Value, Label,
                                            LinkUrl, AdditionalData,
                                            DisplayOrder, Status
                                        )
                                        OUTPUT INSERTED.Id
                                        VALUES
                                        (
                                            @SectionId, @ParentSectionItemId,
                                            @Title, @Heading, @SubHeading,
                                            @Description, @Value, @Label,
                                            @LinkUrl, @AdditionalData,
                                            @DisplayOrder, @Status
                                        );";
                                }
                                else
                                {
                                    command.CommandText = @"
                                        UPDATE SectionItems
                                        SET SectionId = @SectionId,
                                            ParentSectionItemId = @ParentSectionItemId,
                                            Title = @Title,
                                            Heading = @Heading,
                                            SubHeading = @SubHeading,
                                            Description = @Description,
                                            Value = @Value,
                                            Label = @Label,
                                            LinkUrl = @LinkUrl,
                                            AdditionalData = @AdditionalData,
                                            DisplayOrder = @DisplayOrder,
                                            Status = @Status,
                                            UpdatedAt = SYSUTCDATETIME()
                                        WHERE Id = @Id;";

                                    command.Parameters.Add(
                                        "@Id", SqlDbType.UniqueIdentifier).Value =
                                        model.Id;
                                }

                                command.Parameters.Add(
                                    "@SectionId",
                                    SqlDbType.UniqueIdentifier).Value =
                                    model.SectionId;

                                command.Parameters.Add(
                                    "@ParentSectionItemId",
                                    SqlDbType.UniqueIdentifier).Value =
                                    (object)model.ParentSectionItemId ?? DBNull.Value;

                                AddTextParameter(command, "@Title", model.Title, 200);
                                AddTextParameter(command, "@Heading", model.Heading, 200);
                                AddTextParameter(command, "@SubHeading", model.SubHeading, 200);
                                AddTextParameter(command, "@Description", model.Description, -1);
                                AddTextParameter(command, "@Value", model.Value, 500);
                                AddTextParameter(command, "@Label", model.Label, 200);
                                AddTextParameter(command, "@LinkUrl", model.LinkUrl, 1000);
                                AddTextParameter(command, "@AdditionalData", model.AdditionalData, -1);

                                command.Parameters.Add(
                                    "@DisplayOrder", SqlDbType.Int).Value =
                                    model.DisplayOrder;

                                command.Parameters.Add(
                                    "@Status", SqlDbType.Bit).Value =
                                    model.Status;

                                if (isNew)
                                {
                                    object result =
                                        await command.ExecuteScalarAsync();

                                    if (result == null || result == DBNull.Value)
                                    {
                                        transaction.Rollback();
                                        return null;
                                    }

                                    savedId = (Guid)result;
                                }
                                else
                                {
                                    int affected =
                                        await command.ExecuteNonQueryAsync();

                                    if (affected == 0)
                                    {
                                        transaction.Rollback();
                                        return null;
                                    }

                                    savedId = model.Id;
                                }
                            }

                            transaction.Commit();
                            return savedId;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionItemAction", "POST", 0,
                    ip, "SaveSectionItemAsync");
                throw;
            }
        }

        // Delete a parent and its direct children atomically.
        public async Task<bool> DeleteSectionItemAsync(Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int affected;

                            using (var childCommand = new SqlCommand(@"
                                DELETE FROM SectionItems
                                WHERE ParentSectionItemId = @Id;",
                                connection, transaction))
                            {
                                childCommand.Parameters.Add(
                                    "@Id", SqlDbType.UniqueIdentifier).Value = id;

                                await childCommand.ExecuteNonQueryAsync();
                            }

                            using (var parentCommand = new SqlCommand(@"
                                DELETE FROM SectionItems
                                WHERE Id = @Id;",
                                connection, transaction))
                            {
                                parentCommand.Parameters.Add(
                                    "@Id", SqlDbType.UniqueIdentifier).Value = id;

                                affected = await parentCommand.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            return affected > 0;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionItems", "DELETE", 0, ip,
                    "DeleteSectionItemAsync");
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
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionItemAction", "GET", 0, ip,
                    "GetSectionsAsync");
                throw;
            }

            return sections;
        }

        public async Task<List<SectionItemVM>> GetSectionItemsBySectionIdAsync(
            Guid sectionId, string ip)
        {
            var items = new List<SectionItemVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, SectionId, ParentSectionItemId,
                           Title, Heading, SubHeading, Description,
                           Value, Label, LinkUrl, AdditionalData,
                           DisplayOrder, Status, CreatedAt, UpdatedAt
                    FROM SectionItems
                    WHERE SectionId = @SectionId
                      AND ParentSectionItemId IS NULL
                    ORDER BY DisplayOrder, CreatedAt;", connection))
                {
                    command.Parameters.Add(
                        "@SectionId", SqlDbType.UniqueIdentifier).Value =
                        sectionId;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            items.Add(MapSectionItem(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/PageContent", "GET", 0, ip,
                    "GetSectionItemsBySectionIdAsync");
                throw;
            }

            return items;
        }

        private static SectionItemVM MapSectionItem(SqlDataReader reader)
        {
            int parentOrdinal =
                reader.GetOrdinal("ParentSectionItemId");

            return new SectionItemVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                SectionId = reader.GetGuid(reader.GetOrdinal("SectionId")),

                ParentSectionItemId = reader.IsDBNull(parentOrdinal)
                    ? (Guid?)null
                    : reader.GetGuid(parentOrdinal),

                Title = GetString(reader, "Title"),
                Heading = GetString(reader, "Heading"),
                SubHeading = GetString(reader, "SubHeading"),
                Description = GetString(reader, "Description"),
                Value = GetString(reader, "Value"),
                Label = GetString(reader, "Label"),
                LinkUrl = GetString(reader, "LinkUrl"),
                AdditionalData = GetString(reader, "AdditionalData"),
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
                        reader.GetOrdinal("UpdatedAt"))
            };
        }

        private static string GetString(
            SqlDataReader reader, string column)
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



//Without self reference working Service

//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.SqlClient;
//using System.Threading.Tasks;
//using DTOs;

//namespace Services
//{
//    public class SectionItemService
//    {
//        private readonly string _connectionString;
//        private readonly LogErrorService _logErrorService;

//        public SectionItemService(
//            string connectionString,
//            LogErrorService logErrorService)
//        {
//            _connectionString = connectionString;
//            _logErrorService = logErrorService;
//        }

//        public async Task<List<SectionItemVM>> GetSectionItemsAsync(string ip)
//        {
//            var items = new List<SectionItemVM>();

//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                using (var command = new SqlCommand(@"
//                    SELECT Id, SectionId, Title, Heading, SubHeading,
//                           Description, Value, Label, LinkUrl,
//                           AdditionalData, DisplayOrder, Status,
//                           CreatedAt, UpdatedAt
//                    FROM SectionItems
//                    ORDER BY DisplayOrder, CreatedAt DESC;", connection))
//                {
//                    await connection.OpenAsync();

//                    using (var reader = await command.ExecuteReaderAsync())
//                    {
//                        while (await reader.ReadAsync())
//                        {
//                            items.Add(MapSectionItem(reader));
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logErrorService.LogError(
//                    ex, "Admin/SectionItems", "GET", 0, ip,
//                    "GetSectionItemsAsync");

//                throw;
//            }

//            return items;
//        }

//        public async Task<SectionItemVM> GetSectionItemByIdAsync(
//            Guid id, string ip)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                using (var command = new SqlCommand(@"
//                    SELECT Id, SectionId, Title, Heading, SubHeading,
//                           Description, Value, Label, LinkUrl,
//                           AdditionalData, DisplayOrder, Status,
//                           CreatedAt, UpdatedAt
//                    FROM SectionItems
//                    WHERE Id = @Id;", connection))
//                {
//                    command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

//                    await connection.OpenAsync();

//                    using (var reader = await command.ExecuteReaderAsync())
//                    {
//                        if (await reader.ReadAsync())
//                        {
//                            return MapSectionItem(reader);
//                        }
//                    }
//                }

//                return null;
//            }
//            catch (Exception ex)
//            {
//                _logErrorService.LogError(
//                    ex, "Admin/SectionItemAction", "GET", 0,
//                    ip, "GetSectionItemByIdAsync");

//                throw;
//            }
//        }

//        public async Task<Guid?> SaveSectionItemAsync(
//            SectionItemVM model, string ip)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                {
//                    await connection.OpenAsync();

//                    using (var command = new SqlCommand())
//                    {
//                        command.Connection = connection;

//                        bool isNew = model.Id == Guid.Empty;

//                        if (isNew)
//                        {
//                            command.CommandText = @"
//                        INSERT INTO SectionItems
//                        (
//                            SectionId, Title, Heading, SubHeading,
//                            Description, Value, Label, LinkUrl,
//                            AdditionalData, DisplayOrder, Status
//                        )
//                        OUTPUT INSERTED.Id
//                        VALUES
//                        (
//                            @SectionId, @Title, @Heading, @SubHeading,
//                            @Description, @Value, @Label, @LinkUrl,
//                            @AdditionalData, @DisplayOrder, @Status
//                        );";
//                        }
//                        else
//                        {
//                            command.CommandText = @"
//                        UPDATE SectionItems
//                        SET SectionId = @SectionId,
//                            Title = @Title,
//                            Heading = @Heading,
//                            SubHeading = @SubHeading,
//                            Description = @Description,
//                            Value = @Value,
//                            Label = @Label,
//                            LinkUrl = @LinkUrl,
//                            AdditionalData = @AdditionalData,
//                            DisplayOrder = @DisplayOrder,
//                            Status = @Status,
//                            UpdatedAt = SYSUTCDATETIME()
//                        WHERE Id = @Id;";

//                            command.Parameters.Add(
//                                "@Id", SqlDbType.UniqueIdentifier).Value = model.Id;
//                        }

//                        command.Parameters.Add(
//                            "@SectionId", SqlDbType.UniqueIdentifier).Value = model.SectionId;

//                        AddTextParameter(command, "@Title", model.Title, 200);
//                        AddTextParameter(command, "@Heading", model.Heading, 200);
//                        AddTextParameter(command, "@SubHeading", model.SubHeading, 200);
//                        AddTextParameter(command, "@Description", model.Description, -1);
//                        AddTextParameter(command, "@Value", model.Value, 500);
//                        AddTextParameter(command, "@Label", model.Label, 200);
//                        AddTextParameter(command, "@LinkUrl", model.LinkUrl, 1000);
//                        AddTextParameter(command, "@AdditionalData", model.AdditionalData, -1);

//                        command.Parameters.Add(
//                            "@DisplayOrder", SqlDbType.Int).Value = model.DisplayOrder;

//                        command.Parameters.Add(
//                            "@Status", SqlDbType.Bit).Value = model.Status;

//                        if (isNew)
//                        {
//                            object result = await command.ExecuteScalarAsync();

//                            if (result == null || result == DBNull.Value)
//                                return null;

//                            return (Guid)result;
//                        }

//                        int affected = await command.ExecuteNonQueryAsync();

//                        return affected > 0 ? model.Id : (Guid?)null;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logErrorService.LogError(
//                    ex, "Admin/SectionItemAction", "POST", 0,
//                    ip, "SaveSectionItemAsync");

//                throw;
//            }
//        }

//        public async Task<bool> DeleteSectionItemAsync(Guid id, string ip)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                using (var command = new SqlCommand(
//                    "DELETE FROM SectionItems WHERE Id = @Id;", connection))
//                {
//                    command.Parameters.Add(
//                        "@Id", SqlDbType.UniqueIdentifier).Value = id;

//                    await connection.OpenAsync();

//                    int affected = await command.ExecuteNonQueryAsync();

//                    return affected > 0;
//                }
//            }
//            catch (Exception ex)
//            {
//                _logErrorService.LogError(
//                    ex, "Admin/SectionItems", "DELETE", 0, ip,
//                    "DeleteSectionItemAsync");

//                throw;
//            }
//        }

//        public async Task<List<SectionVM>> GetSectionsAsync(string ip)
//        {
//            var sections = new List<SectionVM>();

//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                using (var command = new SqlCommand(@"
//                    SELECT Id, Title
//                    FROM Sections
//                    WHERE Status = 1
//                    ORDER BY Title;", connection))
//                {
//                    await connection.OpenAsync();

//                    using (var reader = await command.ExecuteReaderAsync())
//                    {
//                        while (await reader.ReadAsync())
//                        {
//                            sections.Add(new SectionVM
//                            {
//                                Id = reader.GetGuid(0),
//                                Title = reader.GetString(1)
//                            });
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logErrorService.LogError(
//                    ex, "Admin/SectionItemAction", "GET", 0, ip,
//                    "GetSectionsAsync");

//                throw;
//            }

//            return sections;
//        }

//        private static SectionItemVM MapSectionItem(SqlDataReader reader)
//        {
//            return new SectionItemVM
//            {
//                Id = reader.GetGuid(reader.GetOrdinal("Id")),
//                SectionId = reader.GetGuid(reader.GetOrdinal("SectionId")),
//                Title = GetString(reader, "Title"),
//                Heading = GetString(reader, "Heading"),
//                SubHeading = GetString(reader, "SubHeading"),
//                Description = GetString(reader, "Description"),
//                Value = GetString(reader, "Value"),
//                Label = GetString(reader, "Label"),
//                LinkUrl = GetString(reader, "LinkUrl"),
//                AdditionalData = GetString(reader, "AdditionalData"),
//                DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
//                Status = reader.GetBoolean(reader.GetOrdinal("Status")),
//                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
//                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
//                    ? (DateTime?)null
//                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
//            };
//        }

//        private static string GetString(SqlDataReader reader, string column)
//        {
//            int ordinal = reader.GetOrdinal(column);

//            return reader.IsDBNull(ordinal)
//                ? null
//                : reader.GetString(ordinal);
//        }

//        private static void AddTextParameter(
//            SqlCommand command, string name, string value, int size)
//        {
//            var parameter = command.Parameters.Add(
//                name, SqlDbType.NVarChar, size);

//            parameter.Value = (object)value ?? DBNull.Value;
//        }


//        public async Task<List<SectionItemVM>> GetSectionItemsBySectionIdAsync(
//            Guid sectionId, string ip)
//        {
//            var items = new List<SectionItemVM>();

//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                using (var command = new SqlCommand(@"
//            SELECT Id, SectionId, Title, Heading, SubHeading,
//                   Description, Value, Label, LinkUrl,
//                   AdditionalData, DisplayOrder, Status,
//                   CreatedAt, UpdatedAt
//            FROM SectionItems
//            WHERE SectionId = @SectionId
//            ORDER BY DisplayOrder, CreatedAt;", connection))
//                {
//                    command.Parameters.Add(
//                        "@SectionId", SqlDbType.UniqueIdentifier).Value = sectionId;

//                    await connection.OpenAsync();

//                    using (var reader = await command.ExecuteReaderAsync())
//                    {
//                        while (await reader.ReadAsync())
//                        {
//                            items.Add(MapSectionItem(reader));
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logErrorService.LogError(
//                    ex,
//                    "Admin/PageContent",
//                    "GET",
//                    0,
//                    ip,
//                    "GetSectionItemsBySectionIdAsync");

//                throw;
//            }

//            return items;
//        }

//    }
//}
