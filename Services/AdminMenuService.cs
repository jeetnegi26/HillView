
using DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Web.Mvc;

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

        public async Task<DTOs.AdminMenuItemEditVM> GetMenuItemFormDataAsync(
         string ip, Guid? excludeMenuItemId = null)
        {
            var model = new DTOs.AdminMenuItemEditVM();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string groupQuery = @"
                SELECT Id, Title
                FROM dbo.AdminMenuGroups
                WHERE Status = 1
                ORDER BY DisplayOrder, Title;";

                    using (var command = new SqlCommand(groupQuery, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.Groups = model.Groups.Concat(new[]
                            {
                        new SelectListItem
                        {
                            Value = reader["Id"].ToString(),
                            Text = reader["Title"].ToString()
                        }
                    });
                        }
                    }


                    const string itemQuery = @"
    SELECT Id, Title
    FROM dbo.AdminMenuItems
    WHERE Status = 1
      AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
    ORDER BY DisplayOrder, Title;";

                    using (var command = new SqlCommand(itemQuery, connection))
                    {
                        // Add the parameter BEFORE executing the query.
                        command.Parameters.Add("@ExcludeId", SqlDbType.UniqueIdentifier)
                            .Value = (object)excludeMenuItemId ?? DBNull.Value;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var parentItems = new List<SelectListItem>();

                            while (await reader.ReadAsync())
                            {
                                parentItems.Add(new SelectListItem
                                {
                                    Value = reader["Id"].ToString(),
                                    Text = reader["Title"].ToString()
                                });
                            }

                            model.ParentItems = parentItems;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, ip);
                throw;
            }

            return model;
        }


        public async Task<Guid> SaveMenuItemAsync(
            DTOs.AdminMenuItemEditVM model, string ip)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (string.IsNullOrWhiteSpace(model.Title))
                throw new ArgumentException("Menu title is required.");

            if (model.GroupId.HasValue && model.ParentMenuItemId.HasValue)
            {
                throw new ArgumentException(
                    "Select either a menu group or a parent item, not both.");
            }

            await ValidateMenuParentAsync(
                null,
                model.ParentMenuItemId,
                model.GroupId,
                ip);

            var id = Guid.NewGuid();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            if (model.GroupId.HasValue)
                            {
                                const string groupSql = @"
                            SELECT COUNT(1)
                            FROM dbo.AdminMenuGroups
                            WHERE Id = @GroupId AND Status = 1;";

                                using (var command = new SqlCommand(
                                    groupSql, connection, transaction))
                                {
                                    command.Parameters.Add(
                                        "@GroupId", SqlDbType.UniqueIdentifier)
                                        .Value = model.GroupId.Value;

                                    if (Convert.ToInt32(
                                        await command.ExecuteScalarAsync()) == 0)
                                        throw new ArgumentException(
                                            "The selected menu group is invalid.");
                                }
                            }

                            if (model.ParentMenuItemId.HasValue)
                            {
                                const string parentSql = @"
                            SELECT COUNT(1)
                            FROM dbo.AdminMenuItems
                            WHERE Id = @ParentId AND Status = 1;";

                                using (var command = new SqlCommand(
                                    parentSql, connection, transaction))
                                {
                                    command.Parameters.Add(
                                        "@ParentId", SqlDbType.UniqueIdentifier)
                                        .Value = model.ParentMenuItemId.Value;

                                    if (Convert.ToInt32(
                                        await command.ExecuteScalarAsync()) == 0)
                                        throw new ArgumentException(
                                            "The selected parent menu item is invalid.");
                                }
                            }

                            const string insertSql = @"
                        INSERT INTO dbo.AdminMenuItems
                        (
                            Id, GroupId, ParentMenuItemId, Title, Icon,
                            Controller, Action, Url, DisplayOrder, Status,
                            CreatedAt, UpdatedAt
                        )
                        VALUES
                        (
                            @Id, @GroupId, @ParentId, @Title, @Icon,
                            @Controller, @Action, @Url, @DisplayOrder, @Status,
                            SYSUTCDATETIME(), NULL
                        );";

                            using (var command = new SqlCommand(
                                insertSql, connection, transaction))
                            {
                                command.Parameters.Add(
                                    "@Id", SqlDbType.UniqueIdentifier).Value = id;

                                command.Parameters.Add(
                                    "@GroupId", SqlDbType.UniqueIdentifier).Value =
                                    (object)model.GroupId ?? DBNull.Value;

                                command.Parameters.Add(
                                    "@ParentId", SqlDbType.UniqueIdentifier).Value =
                                    (object)model.ParentMenuItemId ?? DBNull.Value;

                                command.Parameters.Add(
                                    "@Title", SqlDbType.NVarChar, 100).Value =
                                    model.Title.Trim();

                                command.Parameters.Add(
                                    "@Icon", SqlDbType.NVarChar, 100).Value =
                                    (object)model.Icon ?? DBNull.Value;

                                command.Parameters.Add(
                                    "@Controller", SqlDbType.NVarChar, 100).Value =
                                    (object)model.Controller ?? DBNull.Value;

                                command.Parameters.Add(
                                    "@Action", SqlDbType.NVarChar, 100).Value =
                                    (object)model.Action ?? DBNull.Value;

                                command.Parameters.Add(
                                    "@Url", SqlDbType.NVarChar, -1).Value =
                                    (object)model.Url ?? DBNull.Value;

                                command.Parameters.Add(
                                    "@DisplayOrder", SqlDbType.Int).Value =
                                    model.DisplayOrder;

                                command.Parameters.Add(
                                    "@Status", SqlDbType.Bit).Value = model.Status;

                                await command.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                return id;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, ip);
                throw;
            }
        }

        public async Task<DTOs.AdminMenuItemEditVM> GetMenuItemByIdAsync(Guid id, string ip)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string query = @"
                SELECT Id, GroupId, ParentMenuItemId, Title, Icon,
                       Controller, Action, Url, DisplayOrder, Status
                FROM dbo.AdminMenuItems
                WHERE Id = @Id;";

                    DTOs.AdminMenuItemEditVM model = null;

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                            .Value = id;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                model = new DTOs.AdminMenuItemEditVM
                                {
                                    Id = (Guid)reader["Id"],
                                    GroupId = reader["GroupId"] == DBNull.Value
                                        ? (Guid?)null : (Guid)reader["GroupId"],
                                    ParentMenuItemId = reader["ParentMenuItemId"] == DBNull.Value
                                        ? (Guid?)null : (Guid)reader["ParentMenuItemId"],
                                    Title = reader["Title"].ToString(),
                                    Icon = reader["Icon"] == DBNull.Value
                                        ? null : reader["Icon"].ToString(),
                                    Controller = reader["Controller"] == DBNull.Value
                                        ? null : reader["Controller"].ToString(),
                                    Action = reader["Action"] == DBNull.Value
                                        ? null : reader["Action"].ToString(),
                                    Url = reader["Url"] == DBNull.Value
                                        ? null : reader["Url"].ToString(),
                                    DisplayOrder = Convert.ToInt32(reader["DisplayOrder"]),
                                    Status = Convert.ToBoolean(reader["Status"])
                                };
                            }
                        }
                    }

                    if (model == null)
                        return null;

                    var formData = await GetMenuItemFormDataAsync(ip, id);
                    var descendantIds = await GetDescendantMenuItemIdsAsync(id, ip);

                    formData.ParentItems = formData.ParentItems
                        .Where(item => !descendantIds.Contains(Guid.Parse(item.Value)))
                        .ToList();

                    model.Groups = formData.Groups;
                    model.ParentItems = formData.ParentItems;

                    return model;
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, ip);
                throw;
            }
        }

        public async Task UpdateMenuItemAsync(
    DTOs.AdminMenuItemEditVM model, string ip)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!model.Id.HasValue)
                throw new ArgumentException("Menu item ID is required.");

            if (string.IsNullOrWhiteSpace(model.Title))
                throw new ArgumentException("Menu title is required.");

            if (model.GroupId.HasValue && model.ParentMenuItemId.HasValue)
            {
                throw new ArgumentException(
                    "Select either a menu group or a parent item, not both.");
            }

            await ValidateMenuParentAsync(
                model.Id,
                model.ParentMenuItemId,
                model.GroupId,
                ip);

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string query = @"
                UPDATE dbo.AdminMenuItems
                SET
                    GroupId = @GroupId,
                    ParentMenuItemId = @ParentId,
                    Title = @Title,
                    Icon = @Icon,
                    Controller = @Controller,
                    Action = @Action,
                    Url = @Url,
                    DisplayOrder = @DisplayOrder,
                    Status = @Status,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @Id;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                            .Value = model.Id.Value;

                        command.Parameters.Add("@GroupId", SqlDbType.UniqueIdentifier)
                            .Value = (object)model.GroupId ?? DBNull.Value;

                        command.Parameters.Add("@ParentId", SqlDbType.UniqueIdentifier)
                            .Value = (object)model.ParentMenuItemId ?? DBNull.Value;

                        command.Parameters.Add("@Title", SqlDbType.NVarChar, 100)
                            .Value = model.Title.Trim();

                        command.Parameters.Add("@Icon", SqlDbType.NVarChar, 100)
                            .Value = (object)model.Icon ?? DBNull.Value;

                        command.Parameters.Add("@Controller", SqlDbType.NVarChar, 100)
                            .Value = (object)model.Controller ?? DBNull.Value;

                        command.Parameters.Add("@Action", SqlDbType.NVarChar, 100)
                            .Value = (object)model.Action ?? DBNull.Value;

                        command.Parameters.Add("@Url", SqlDbType.NVarChar, -1)
                            .Value = (object)model.Url ?? DBNull.Value;

                        command.Parameters.Add("@DisplayOrder", SqlDbType.Int)
                            .Value = model.DisplayOrder;

                        command.Parameters.Add("@Status", SqlDbType.Bit)
                            .Value = model.Status;

                        var affectedRows = await command.ExecuteNonQueryAsync();

                        if (affectedRows == 0)
                            throw new ArgumentException("Menu item was not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, ip);
                throw;
            }
        }
        private async Task<HashSet<Guid>> GetDescendantMenuItemIdsAsync(
    Guid menuItemId, string ip)
        {
            var descendants = new HashSet<Guid>();
            var pending = new Stack<Guid>();
            pending.Push(menuItemId);

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string query = @"
                SELECT Id
                FROM dbo.AdminMenuItems
                WHERE ParentMenuItemId = @ParentId;";

                    while (pending.Count > 0)
                    {
                        var parentId = pending.Pop();

                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.Add("@ParentId",
                                SqlDbType.UniqueIdentifier).Value = parentId;

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var childId = reader.GetGuid(0);

                                    if (descendants.Add(childId))
                                        pending.Push(childId);
                                }
                            }
                        }
                    }
                }

                return descendants;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, ip);
                throw;
            }
        }


        private async Task ValidateMenuParentAsync(
    Guid? menuItemId,
    Guid? parentMenuItemId,
    Guid? groupId,
    string ip)
        {
            if (!parentMenuItemId.HasValue)
                return;

            if (menuItemId.HasValue &&
                menuItemId.Value == parentMenuItemId.Value)
            {
                throw new ArgumentException(
                    "A menu item cannot be its own parent.");
            }

            if (menuItemId.HasValue)
            {
                var descendantIds = await GetDescendantMenuItemIdsAsync(
                    menuItemId.Value, ip);

                if (descendantIds.Contains(parentMenuItemId.Value))
                {
                    throw new ArgumentException(
                        "A menu item cannot be placed under one of its descendants.");
                }
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string query = @"
            SELECT GroupId, Status
            FROM dbo.AdminMenuItems
            WHERE Id = @ParentId;";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@ParentId", SqlDbType.UniqueIdentifier)
                        .Value = parentMenuItemId.Value;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            throw new ArgumentException(
                                "The selected parent menu item does not exist.");
                        }

                        if (!Convert.ToBoolean(reader["Status"]))
                        {
                            throw new ArgumentException(
                                "The selected parent menu item is inactive.");
                        }

                        // A child inherits its parent's location in the sidebar.
                        // Do not allow a child to also select a separate group.
                        if (groupId.HasValue)
                        {
                            throw new ArgumentException(
                                "Select either a menu group or a parent item, not both.");
                        }
                    }
                }
            }
        }


        public async Task DeleteMenuItemAsync(Guid id, string ip)
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
                            // Check whether the item has child menu items.
                            const string childQuery = @"
                        SELECT COUNT(1)
                        FROM dbo.AdminMenuItems
                        WHERE ParentMenuItemId = @Id;";

                            using (var command = new SqlCommand(
                                childQuery, connection, transaction))
                            {
                                command.Parameters.Add("@Id",
                                    SqlDbType.UniqueIdentifier).Value = id;

                                int childCount = Convert.ToInt32(
                                    await command.ExecuteScalarAsync());

                                if (childCount > 0)
                                {
                                    throw new InvalidOperationException(
                                        "This menu item cannot be deleted because it has child items. " +
                                        "Move or delete its child items first.");
                                }
                            }

                            // Delete the selected item.
                            const string deleteQuery = @"
                        DELETE FROM dbo.AdminMenuItems
                        WHERE Id = @Id;";

                            using (var command = new SqlCommand(
                                deleteQuery, connection, transaction))
                            {
                                command.Parameters.Add("@Id",
                                    SqlDbType.UniqueIdentifier).Value = id;

                                int affectedRows = await command.ExecuteNonQueryAsync();

                                if (affectedRows == 0)
                                {
                                    throw new ArgumentException(
                                        "The menu item was not found.");
                                }
                            }

                            transaction.Commit();
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
                _logErrorService.LogError(ex, ipAddress: ip);
                throw;
            }
        }


    }
}
