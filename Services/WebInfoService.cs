using DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Caching;

namespace Services
{
    public class WebInfoService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;
        private static readonly ObjectCache Cache = MemoryCache.Default;
        private const string BrandLogoCacheKey = "BrandLogo";

        public WebInfoService(string connectionString, LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        public async Task<List<Key>> GetWebInfoAsync(string ip)
        {
            List<Key> keysList = new List<Key>();
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("GetKeys", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        con.Open();

                        using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                        {
                            while (await dr.ReadAsync())
                            {
                                keysList.Add(new Key
                                {
                                    Id = Convert.ToInt32(dr["Id"]),
                                    Name = dr["Name"].ToString(),
                                    Type = dr["Type"].ToString(),
                                    SVG = dr["SVG_Value"].ToString(),
                                    Description = dr["Description"].ToString(),
                                });
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, "/Helper/GetConatctDetails", "Get", 0, ip, "GetKeys");
            }
            return keysList;
        }
        public async Task<Key> GetWebInfoByIdAsync(int id, string ip)
        {
            Key data = new Key();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var command = new SqlCommand("GetWebInfoById", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", id);
                    connection.Open();
                    var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        data.Id = reader["Id"] != DBNull.Value ? (int)reader["Id"] : 0;
                        data.Name = reader["Name"] != DBNull.Value ? reader["Name"].ToString() : null;
                        data.SVG = reader["SVG_Value"] != DBNull.Value ? reader["SVG_Value"].ToString() : null;
                        data.Type = reader["Type"] != DBNull.Value ? reader["Type"].ToString() : null;
                        data.Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, "Admin/WebInfoAction", "GET", 0, ip, "GetWebInfoById");
            }
            return data;
        }
        public async Task<ResponseVM> UpInsertWebInfoAsync(Key key, string userHostAdd)
        {
            ResponseVM result = new ResponseVM();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var command = new SqlCommand("sp_UpInsertWebInfo", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", key.Id);
                    command.Parameters.AddWithValue("@Name", key.Name);
                    command.Parameters.AddWithValue("@Type", key.Type);
                    command.Parameters.AddWithValue("@SVG", !string.IsNullOrEmpty(key.SVG) ? key.SVG : "");
                    command.Parameters.AddWithValue("@Description", key.Description ?? "");

                    var resultParam = new SqlParameter("@Result", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(resultParam);

                    connection.Open();
                    await command.ExecuteReaderAsync();
                    int resultId = (int)resultParam.Value;
                    if (resultId > 0)
                    {
                        result.Success = true;
                    }
                    else
                    {
                        result.Success = false;
                        _logErrorService.LogErrorInTryBlock("Not fetched from store procedure", "Admin/WebInfoAction", "POST", 0, userHostAdd, "sp_UpInsertWebInfo");
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, "Admin/WebInfoAction", "POST", 0, userHostAdd, "sp_UpInsertWebInfo");
            }

            if (key.Type == "BrandLogo")
            {
                ClearBrandLogoCache();
            }

            return result;
        }

        public async Task<string> GetBrandLogoAsync()
        {
            // 1. Check memory cache first
            var cachedLogo = Cache.Get(BrandLogoCacheKey) as string;

            if (cachedLogo != null)
                return cachedLogo;

            // 2. If not cached, query the database
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(
                "SELECT Name FROM dbo.[Key] WHERE Type = 'BrandLogo'",
                connection))
            {
                await connection.OpenAsync();

                var result = await command.ExecuteScalarAsync();

                var logo = result == null || result == DBNull.Value
                    ? null
                    : Convert.ToString(result);

                // 3. Cache the result for 30 minutes
                if (logo != null)
                {
                    Cache.Set(
                        BrandLogoCacheKey,
                        logo,
                        DateTimeOffset.UtcNow.AddMinutes(30));
                }

                return logo;
            }
        }

        public void ClearBrandLogoCache()
        {
            Cache.Remove(BrandLogoCacheKey);
        }
    }
}
