using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using DTOs;

namespace Services
{
    public class ContactUsService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public ContactUsService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        /// <summary>
        /// status = null  -> all records
        /// status = true  -> only active (Status = 1)
        /// status = false -> only inactive (Status = 0)
        /// </summary>
        public async Task<List<ContactUsVM>> GetContactsAsync(
            bool? status, string ip)
        {
            var contacts = new List<ContactUsVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, FullName, Email, Phone, [Message],
                           Status, UserHostAdd, CreatedAt, UpdatedAt
                    FROM ContactUs
                    WHERE (@Status IS NULL OR Status = @Status)
                    ORDER BY CreatedAt DESC;", connection))
                {
                    command.Parameters.Add(
                        "@Status", SqlDbType.Bit).Value =
                        status.HasValue ? (object)status.Value : DBNull.Value;

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            contacts.Add(MapContact(reader));
                        }
                    }
                }

                return contacts;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/ContactUs", "GET", 0, ip,
                    "GetContactsAsync");

                throw;
            }
        }

        public async Task<bool> SetStatusAsync(Guid id, bool status, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    UPDATE ContactUs
                    SET Status = @Status,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @Id;", connection))
                {
                    command.Parameters.Add(
                        "@Id", SqlDbType.UniqueIdentifier).Value = id;

                    command.Parameters.Add(
                        "@Status", SqlDbType.Bit).Value = status;

                    await connection.OpenAsync();

                    int affected = await command.ExecuteNonQueryAsync();

                    return affected > 0;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/ContactUs", "POST", 0, ip,
                    "SetStatusAsync");

                throw;
            }
        }

        private static ContactUsVM MapContact(SqlDataReader reader)
        {
            return new ContactUsVM
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                FullName = GetString(reader, "FullName"),
                Email = GetString(reader, "Email"),
                Phone = GetString(reader, "Phone"),
                Message = GetString(reader, "Message"),
                Status = reader.GetBoolean(reader.GetOrdinal("Status")),
                UserHostAdd = GetString(reader, "UserHostAdd"),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        private static string GetString(SqlDataReader reader, string column)
        {
            int ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetString(ordinal);
        }
    }
}