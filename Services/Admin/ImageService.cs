
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using DTOs;

namespace Services
{
    public class ImageService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public ImageService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        public async Task<List<ImageVM>> GetImagesAsync(string ip)
        {
            var images = new List<ImageVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
    SELECT
        i.Id,
        i.SectionId,
        i.SectionItemId,
        s.Title AS SectionTitle,
        si.Title AS SectionItemTitle,
        i.ImageUrl,
        i.AltText,
        i.Caption,
        i.ImageType,
        i.DisplayOrder,
        i.Status,
        i.CreatedAt,
        i.UpdatedAt
    FROM dbo.Images i
    LEFT JOIN dbo.Sections s
        ON i.SectionId = s.Id
    LEFT JOIN dbo.SectionItems si
        ON i.SectionItemId = si.Id
    ORDER BY i.CreatedAt DESC;",
                    connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            images.Add(MapImage(reader));
                        }
                    }
                }

                return images;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Images", "GET", 0, ip, "GetImagesAsync");

                throw;
            }
        }

        public async Task<ImageVM> GetImageByIdAsync(Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, SectionId, SectionItemId, ImageUrl,
                           AltText, Caption, ImageType, DisplayOrder,
                           Status, CreatedAt, UpdatedAt
                    FROM Images
                    WHERE Id = @Id;", connection))
                {
                    command.Parameters.Add(
                        "@Id", SqlDbType.UniqueIdentifier).Value = id;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapImage(reader);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/ImageAction", "GET", 0, ip,
                    "GetImageByIdAsync");

                throw;
            }
        }

        public async Task<bool> SaveImageAsync(ImageVM model, string ip)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            // Each image must belong to exactly one parent.
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
                            INSERT INTO Images
                            (
                                SectionId, SectionItemId, ImageUrl,
                                AltText, Caption, ImageType,
                                DisplayOrder, Status
                            )
                            VALUES
                            (
                                @SectionId, @SectionItemId, @ImageUrl,
                                @AltText, @Caption, @ImageType,
                                @DisplayOrder, @Status
                            );";
                    }
                    else
                    {
                        command.CommandText = @"
                            UPDATE Images
                            SET SectionId = @SectionId,
                                SectionItemId = @SectionItemId,
                                ImageUrl = @ImageUrl,
                                AltText = @AltText,
                                Caption = @Caption,
                                ImageType = @ImageType,
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

                    AddTextParameter(command, "@ImageUrl", model.ImageUrl, 1000);
                    AddTextParameter(command, "@AltText", model.AltText, 500);
                    AddTextParameter(command, "@Caption", model.Caption, 500);
                    AddTextParameter(command, "@ImageType", model.ImageType, 100);

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
                    ex, "Admin/ImageAction", "POST", 0, ip,
                    "SaveImageAsync");

                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(
                    "DELETE FROM Images WHERE Id = @Id;", connection))
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
                    ex, "Admin/Images", "DELETE", 0, ip,
                    "DeleteImageAsync");

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
                    ex, "Admin/ImageAction", "GET", 0, ip,
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
                    ex, "Admin/ImageAction", "GET", 0, ip,
                    "GetSectionItemsAsync");

                throw;
            }
        }

        private static ImageVM MapImage(SqlDataReader reader)
        {
            return new ImageVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                SectionId = GetGuid(reader, "SectionId"),
                SectionItemId = GetGuid(reader, "SectionItemId"),
                ImageUrl = GetString(reader, "ImageUrl"),
                AltText = GetString(reader, "AltText"),
                Caption = GetString(reader, "Caption"),
                ImageType = GetString(reader, "ImageType"),
                DisplayOrder = reader.GetInt32(
                    reader.GetOrdinal("DisplayOrder")),
                Status = reader.GetBoolean(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                    ? (DateTime?)null
                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                SectionTitle = GetStringIfExists(reader, "SectionTitle"),
                SectionItemTitle = GetStringIfExists(reader, "SectionItemTitle"),
            };
        }

        private static string GetStringIfExists(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(
                    columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return reader.IsDBNull(i) ? null : reader.GetString(i);
                }
            }

            return null;
        }
        public async Task<bool> IsImageUrlReferencedAsync(
            string imageUrl,
            Guid excludedImageId,
            string ip)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return false;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
            SELECT CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM Images
                    WHERE ImageUrl = @ImageUrl
                      AND Id <> @ExcludedImageId
                )
                THEN 1
                ELSE 0
            END;", connection))
                {
                    command.Parameters.Add(
                        "@ImageUrl", SqlDbType.NVarChar, 1000).Value = imageUrl;

                    command.Parameters.Add(
                        "@ExcludedImageId", SqlDbType.UniqueIdentifier)
                        .Value = excludedImageId;

                    await connection.OpenAsync();

                    int result = Convert.ToInt32(
                        await command.ExecuteScalarAsync());

                    return result == 1;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/ImageAction",
                    "GET",
                    0,
                    ip,
                    "IsImageUrlReferencedAsync");

                throw;
            }
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


        public async Task<List<ImageVM>> GetImagesBySectionItemIdAsync(Guid sectionItemId, string ip)
        {
            var images = new List<ImageVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
            SELECT Id, SectionId, SectionItemId, ImageUrl,
                   AltText, Caption, ImageType, DisplayOrder,
                   Status, CreatedAt, UpdatedAt
            FROM Images
            WHERE SectionItemId = @SectionItemId
            ORDER BY DisplayOrder, CreatedAt DESC;", connection))
                {
                    command.Parameters.Add(
                        "@SectionItemId", SqlDbType.UniqueIdentifier)
                        .Value = sectionItemId;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            images.Add(MapImage(reader));
                        }
                    }
                }

                return images;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/SectionItems",
                    "GET",
                    0,
                    ip,
                    "GetImagesBySectionItemIdAsync");

                throw;
            }
        }


        public async Task<List<ImageVM>> GetImagesBySectionIdAsync(
            Guid sectionId, string ip)
        {
            var images = new List<ImageVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
            SELECT Id, SectionId, SectionItemId, ImageUrl,
                   AltText, Caption, ImageType, DisplayOrder,
                   Status, CreatedAt, UpdatedAt
            FROM Images
            WHERE SectionId = @SectionId
            ORDER BY DisplayOrder, CreatedAt DESC;", connection))
                {
                    command.Parameters.Add(
                        "@SectionId", SqlDbType.UniqueIdentifier)
                        .Value = sectionId;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            images.Add(MapImage(reader));
                        }
                    }
                }

                return images;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/Sections",
                    "GET",
                    0,
                    ip,
                    "GetImagesBySectionIdAsync");

                throw;
            }
        }

    }
}




//using System;
//using System.Data;
//using System.Data.SqlClient;
//using System.Threading.Tasks;

//namespace Services
//{
//    public class ImageService
//    {
//        private readonly string _connectionString;
//        private readonly LogErrorService _logErrorService;

//        public ImageService(
//            string connectionString,
//            LogErrorService logErrorService)
//        {
//            _connectionString = connectionString;
//            _logErrorService = logErrorService;
//        }

//        public async Task<Guid> UpInsertImageAsync(
//            string image,
//            string typeId,
//            string userHostAdd,
//            string id = "")
//        {
//            Guid? imageId = null;

//            if (!string.IsNullOrWhiteSpace(id))
//            {
//                if (!Guid.TryParse(id, out Guid parsedId))
//                    throw new ArgumentException("Invalid image ID.");

//                imageId = parsedId;
//            }

//            using (var connection =
//                   new SqlConnection(_connectionString))
//            using (var command =
//                   new SqlCommand("dbo.sp_UpInsertImage", connection))
//            {
//                command.CommandType = CommandType.StoredProcedure;

//                command.Parameters.Add(
//                    "@Id", SqlDbType.UniqueIdentifier).Value =
//                    (object)imageId ?? DBNull.Value;

//                command.Parameters.Add(
//                    "@Url", SqlDbType.NVarChar, 250).Value = image;

//                command.Parameters.Add(
//                    "@TypeId", SqlDbType.NVarChar, 250).Value = typeId;

//                command.Parameters.Add(
//                    "@UserHostAdd", SqlDbType.VarChar, 45).Value =
//                    string.IsNullOrWhiteSpace(userHostAdd)
//                        ? (object)DBNull.Value
//                        : userHostAdd;

//                var resultParameter = command.Parameters.Add(
//                    "@Result", SqlDbType.UniqueIdentifier);

//                resultParameter.Direction = ParameterDirection.Output;

//                await connection.OpenAsync();
//                await command.ExecuteNonQueryAsync();

//                return (Guid)resultParameter.Value;
//            }
//        }

//        public async Task<string> GetImageUrlAsync(string id)
//        {
//            if (!Guid.TryParse(id, out Guid imageId))
//                return null;

//            using (var connection = new SqlConnection(_connectionString))
//            using (var command = new SqlCommand(
//                "SELECT Url FROM dbo.Images WHERE Id = @Id", connection))
//            {
//                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
//                    .Value = imageId;

//                await connection.OpenAsync();

//                var result = await command.ExecuteScalarAsync();

//                return result == null || result == DBNull.Value
//                    ? null
//                    : Convert.ToString(result);
//            }
//        }

//        public async Task<DTOs.Image> GetBrandLogoAsync()
//        {
//            using (var connection = new SqlConnection(_connectionString))
//            using (var command = new SqlCommand("dbo.GetBrandLogo", connection))
//            {
//                command.CommandType = CommandType.StoredProcedure;

//                await connection.OpenAsync();

//                using (var reader = await command.ExecuteReaderAsync())
//                {
//                    if (!await reader.ReadAsync())
//                        return null;

//                    return new DTOs.Image
//                    {
//                        Id = reader["Id"].ToString(),
//                        Url = reader["Url"].ToString(),
//                        DisplayOrder = Convert.ToInt32(reader["DisplayOrder"])
//                    };
//                }
//            }
//        }
//    }
//}
