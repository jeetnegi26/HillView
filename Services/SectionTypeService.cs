
using DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Services
{
    public class SectionTypeService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public SectionTypeService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
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
                    ORDER BY DisplayOrder, Title;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            sectionTypes.Add(MapSectionType(reader));
                        }
                    }
                }

                return sectionTypes;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionTypes", "GET", 0, ip,
                    "GetSectionTypes");

                throw;
            }
        }

        public async Task<SectionTypeVM> GetSectionTypeByIdAsync(
            int id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, Name, Title, Description, Status, DisplayOrder
                    FROM dbo.SectionTypes
                    WHERE Id = @Id;", connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapSectionType(reader);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionTypeAction", "GET", id, ip,
                    "GetSectionTypeById");

                throw;
            }
        }

        public async Task<bool> SaveSectionTypeAsync(
            SectionTypeVM model, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string sql;

                    if (model.Id <= 0)
                    {
                        sql = @"
                            INSERT INTO dbo.SectionTypes
                                (Name, Title, Description, Status, DisplayOrder)
                            VALUES
                                (@Name, @Title, @Description, @Status, @DisplayOrder);";
                    }
                    else
                    {
                        sql = @"
                            UPDATE dbo.SectionTypes
                            SET Name = @Name,
                                Title = @Title,
                                Description = @Description,
                                Status = @Status,
                                DisplayOrder = @DisplayOrder
                            WHERE Id = @Id;";
                    }

                    using (var command = new SqlCommand(sql, connection))
                    {
                        if (model.Id > 0)
                        {
                            command.Parameters.Add("@Id", SqlDbType.Int)
                                .Value = model.Id;
                        }

                        command.Parameters.Add("@Name", SqlDbType.VarChar, 50)
                            .Value = model.Name.Trim();

                        command.Parameters.Add("@Title", SqlDbType.NVarChar, 100)
                            .Value = model.Title.Trim();

                        command.Parameters.Add("@Description", SqlDbType.NVarChar, 500)
                            .Value = string.IsNullOrWhiteSpace(model.Description)
                                ? (object)DBNull.Value
                                : model.Description.Trim();

                        command.Parameters.Add("@Status", SqlDbType.Bit)
                            .Value = model.Status;

                        command.Parameters.Add("@DisplayOrder", SqlDbType.Int)
                            .Value = model.DisplayOrder;

                        await connection.OpenAsync();

                        int affectedRows = await command.ExecuteNonQueryAsync();

                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionTypeAction", "POST",
                    model.Id, ip, "SaveSectionType");

                throw;
            }
        }

        public async Task<bool> DeleteSectionTypeAsync(int id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    DELETE FROM dbo.SectionTypes
                    WHERE Id = @Id;", connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                    await connection.OpenAsync();

                    int affectedRows = await command.ExecuteNonQueryAsync();

                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionTypes", "DELETE", id, ip,
                    "DeleteSectionType");

                throw;
            }
        }

        private static SectionTypeVM MapSectionType(SqlDataReader reader)
        {
            return new SectionTypeVM
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"] == DBNull.Value
                    ? string.Empty
                    : Convert.ToString(reader["Name"]),
                Title = reader["Title"] == DBNull.Value
                    ? string.Empty
                    : Convert.ToString(reader["Title"]),
                Description = reader["Description"] == DBNull.Value
                    ? string.Empty
                    : Convert.ToString(reader["Description"]),
                Status = reader["Status"] != DBNull.Value
                    && Convert.ToBoolean(reader["Status"]),
                DisplayOrder = reader["DisplayOrder"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(reader["DisplayOrder"])
            };
        }
    }
}
