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
        private const string LayoutDataCacheKey = "HillViewLayoutData";

        public WebInfoService(string connectionString, LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        public LayoutDataVM GetLayoutData(string ip)
        {
            var cachedData = Cache.Get(LayoutDataCacheKey) as LayoutDataVM;

            if (cachedData != null)
            {
                return cachedData;
            }

            var data = new LayoutDataVM();

            const string query = @"
        SELECT Id, Name, Type, Description, SVG_Value,
               GroupName, DisplayOrder
        FROM dbo.[Key]
        WHERE Type IN (
            'BrandName', 'BrandLogo', 'BrandInfo',
            'FooterLink', 'SocialMedia'
        )
        ORDER BY DisplayOrder, Id;

        SELECT Title, Slug
        FROM dbo.Pages
        WHERE Status = 1 AND IsNav = 1
        ORDER BY DisplayOrder, Title;
    ";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        // First result set: branding, footer and social links
                        while (reader.Read())
                        {
                            string name = reader["Name"] == DBNull.Value
                                ? ""
                                : reader["Name"].ToString();

                            string type = reader["Type"] == DBNull.Value
                                ? ""
                                : reader["Type"].ToString();

                            string description =
                                reader["Description"] == DBNull.Value
                                    ? null
                                    : reader["Description"].ToString();

                            string svg = reader["SVG_Value"] == DBNull.Value
                                ? null
                                : reader["SVG_Value"].ToString();

                            string groupName = reader["GroupName"] == DBNull.Value
                                ? null
                                : reader["GroupName"].ToString();

                            int displayOrder =
                                reader["DisplayOrder"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["DisplayOrder"]);

                            switch (type)
                            {
                                case "BrandName":
                                    data.BrandName = name;
                                    break;

                                case "BrandLogo":
                                    data.Logo = name;
                                    break;

                                case "BrandInfo":
                                    switch (name.Trim().ToLowerInvariant())
                                    {
                                        case "phone":
                                            data.Phone = description;
                                            break;

                                        case "toll free":
                                            data.TollFree = description;
                                            break;

                                        case "email":
                                            data.Email = description;
                                            break;

                                        case "address":
                                            data.Address = description;
                                            break;

                                        case "business hours":
                                            data.BusinessHours = description;
                                            break;

                                        case "copyright":
                                            data.Copyright = description;
                                            break;
                                    }
                                    break;

                                case "FooterLink":
                                    data.FooterLinks.Add(new LayoutFooterLinkVM
                                    {
                                        Name = name,
                                        Url = description,
                                        GroupName = groupName,
                                        DisplayOrder = displayOrder
                                    });
                                    break;

                                case "SocialMedia":
                                    data.SocialLinks.Add(new LayoutSocialLinkVM
                                    {
                                        Name = name,
                                        Url = description,
                                        Icon = svg,
                                        DisplayOrder = displayOrder
                                    });
                                    break;
                            }
                        }

                        // Second result set: navigation pages
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                data.Navigation.Add(new LayoutNavVM
                                {
                                    Title = reader["Title"].ToString(),
                                    Slug = reader["Slug"].ToString()
                                });
                            }
                        }
                    }
                }

                // Keep footer and social links in display order
                data.FooterLinks = data.FooterLinks
                    .OrderBy(x => x.GroupName)
                    .ThenBy(x => x.DisplayOrder)
                    .ToList();

                data.SocialLinks = data.SocialLinks
                    .OrderBy(x => x.DisplayOrder)
                    .ToList();

                Cache.Set(LayoutDataCacheKey, data, DateTimeOffset.UtcNow.AddMinutes(30));
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Shared/Layout",
                    "GET",
                    0,
                    ip,
                    "GetLayoutDataAsync");
            }

            return data;
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
                                    GroupName = dr["GroupName"] == DBNull.Value ? null : dr["GroupName"].ToString(),
                                    DisplayOrder = dr["DisplayOrder"] == DBNull.Value ? 0 : Convert.ToInt32(dr["DisplayOrder"]),
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
                        data.GroupName = reader["GroupName"] != DBNull.Value ? reader["GroupName"].ToString() : null;
                        data.DisplayOrder = reader["DisplayOrder"] != DBNull.Value ? Convert.ToInt32(reader["DisplayOrder"]) : 0;
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
                    command.Parameters.Add("@GroupName", SqlDbType.NVarChar, 100).Value = (object)key.GroupName ?? DBNull.Value;
                    command.Parameters.Add("@DisplayOrder", SqlDbType.Int).Value = key.DisplayOrder;

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
                if (result.Success)
                {
                    ClearLayoutDataCache();

                    if (key.Type == "BrandLogo")
                    {
                        ClearBrandLogoCache();
                    }
                }
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
        public void ClearLayoutDataCache()
        {
            Cache.Remove(LayoutDataCacheKey);
        }
    }
}
