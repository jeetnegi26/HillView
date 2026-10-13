
using DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class AdminMenuService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public AdminMenuService(
            string connectionString,
            LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }

        public async Task<List<AdminMenuGroupVM>> GetMenuGroupsAsync(string ip)
        {
            var groups = new List<AdminMenuGroupVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, Title, DisplayOrder, Status
                    FROM AdminMenuGroups
                    WHERE Status = 1
                    ORDER BY DisplayOrder, Title;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            groups.Add(new AdminMenuGroupVM
                            {
                                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                                Status = reader.GetBoolean(reader.GetOrdinal("Status"))
                            });
                        }
                    }
                }

                return groups;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Sidebar", "GET", 0, ip,
                    "GetMenuGroupsAsync");

                throw;
            }
        }

        public async Task<List<AdminMenuItemVM>> GetMenuItemsAsync(string ip)
        {
            var items = new List<AdminMenuItemVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"
                    SELECT Id, GroupId, ParentMenuItemId, Title, Icon,
                           Controller, Action, Url, DisplayOrder, Status
                    FROM AdminMenuItems
                    WHERE Status = 1
                    ORDER BY DisplayOrder, Title;", connection))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(new AdminMenuItemVM
                            {
                                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                GroupId = GetNullableGuid(reader, "GroupId"),
                                ParentMenuItemId = GetNullableGuid(reader, "ParentMenuItemId"),
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                Icon = GetNullableString(reader, "Icon"),
                                Controller = GetNullableString(reader, "Controller"),
                                Action = GetNullableString(reader, "Action"),
                                Url = GetNullableString(reader, "Url"),
                                DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                                Status = reader.GetBoolean(reader.GetOrdinal("Status"))
                            });
                        }
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Sidebar", "GET", 0, ip,
                    "GetMenuItemsAsync");

                throw;
            }
        }

        private static Guid? GetNullableGuid(
            SqlDataReader reader, string column)
        {
            int ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(ordinal)
                ? (Guid?)null
                : reader.GetGuid(ordinal);
        }


        public async Task<List<AdminMenuItemVM>> GetMenuTreeAsync(string ip)
        {
            var items = await GetMenuItemsAsync(ip);

            var itemsById = new Dictionary<Guid, AdminMenuItemVM>();

            foreach (var item in items)
            {
                item.Children.Clear();
                itemsById[item.Id] = item;
            }

            var rootItems = new List<AdminMenuItemVM>();

            foreach (var item in items)
            {
                if (item.ParentMenuItemId.HasValue &&
                    itemsById.TryGetValue(item.ParentMenuItemId.Value, out var parent))
                {
                    parent.Children.Add(item);
                }
                else
                {
                    rootItems.Add(item);
                }
            }

            SortMenuTree(rootItems);

            return rootItems;
        }

        private static void SortMenuTree(List<AdminMenuItemVM> items)
        {
            items.Sort((a, b) =>
            {
                int orderComparison = a.DisplayOrder.CompareTo(b.DisplayOrder);

                return orderComparison != 0
                    ? orderComparison
                    : string.Compare(
                        a.Title,
                        b.Title,
                        StringComparison.OrdinalIgnoreCase);
            });

            foreach (var item in items)
            {
                SortMenuTree(item.Children);
            }
        }


        public AdminSidebarVM GetSidebarMenu(string ip)
        {
            var sidebar = new AdminSidebarVM();
            var items = new List<AdminMenuItemVM>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // 1. Load active menu groups
                    using (var command = new SqlCommand(@"
                SELECT Id, Title, DisplayOrder, Status
                FROM dbo.AdminMenuGroups
                WHERE Status = 1
                ORDER BY DisplayOrder, Title;", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sidebar.Groups.Add(new AdminMenuGroupVM
                                {
                                    Id = reader.GetGuid(0),
                                    Title = reader.GetString(1),
                                    DisplayOrder = reader.GetInt32(2),
                                    Status = reader.GetBoolean(3)
                                });
                            }
                        }
                    }

                    // 2. Load active menu items
                    using (var command = new SqlCommand(@"
                SELECT Id, GroupId, ParentMenuItemId,
                       Title, Icon, Controller, Action, Url,
                       DisplayOrder, Status
                FROM dbo.AdminMenuItems
                WHERE Status = 1
                ORDER BY DisplayOrder, Title;", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(new AdminMenuItemVM
                                {
                                    Id = reader.GetGuid(0),
                                    GroupId = reader.IsDBNull(1)
                                        ? (Guid?)null : reader.GetGuid(1),
                                    ParentMenuItemId = reader.IsDBNull(2)
                                        ? (Guid?)null : reader.GetGuid(2),
                                    Title = reader.GetString(3),
                                    Icon = reader.IsDBNull(4)
                                        ? null : reader.GetString(4),
                                    Controller = reader.IsDBNull(5)
                                        ? null : reader.GetString(5),
                                    Action = reader.IsDBNull(6)
                                        ? null : reader.GetString(6),
                                    Url = reader.IsDBNull(7)
                                        ? null : reader.GetString(7),
                                    DisplayOrder = reader.GetInt32(8),
                                    Status = reader.GetBoolean(9)
                                });
                            }
                        }
                    }
                }

                // Build the hierarchy after reading the database results.
                BuildSidebarHierarchy(sidebar, items);

                return sidebar;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/Sidebar",
                    "GET",
                    0,
                    ip,
                    "GetSidebarMenu");

                throw;
            }
        }

        private static void BuildSidebarHierarchy(
            AdminSidebarVM sidebar,
            List<AdminMenuItemVM> items)
        {
            var groupsById = sidebar.Groups.ToDictionary(g => g.Id);
            var itemsById = items.ToDictionary(i => i.Id);

            // Attach each item to its parent.
            foreach (var item in items)
            {
                if (item.ParentMenuItemId.HasValue &&
                    itemsById.TryGetValue(item.ParentMenuItemId.Value, out var parent))
                {
                    parent.Children.Add(item);
                }
            }

            // Attach root items to their groups or the standalone collection.
            foreach (var item in items)
            {
                if (item.ParentMenuItemId.HasValue)
                {
                    continue;
                }

                if (item.GroupId.HasValue &&
                    groupsById.TryGetValue(item.GroupId.Value, out var group))
                {
                    group.Items.Add(item);
                }
                else if (!item.GroupId.HasValue)
                {
                    sidebar.StandaloneItems.Add(item);
                }
            }

            SortMenuTree(sidebar.StandaloneItems);

            foreach (var group in sidebar.Groups)
            {
                SortMenuTree(group.Items);
            }

            sidebar.Groups = sidebar.Groups
                .OrderBy(g => g.DisplayOrder)
                .ToList();
        }


        private static string GetNullableString(
            SqlDataReader reader, string column)
        {
            int ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetString(ordinal);
        }
    }
}
