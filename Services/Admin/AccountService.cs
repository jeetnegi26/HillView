using DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class AccountService
    {
        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;

        public AccountService(string connectionString, LogErrorService logErrorService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
        }


        public LoginOutputModel AuthenticateUser(LoginModel model, string ip)
        {
            var result = new LoginOutputModel();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    int userId;
                    string userName;
                    string passwordHash;
                    bool isFirstLogin;

                    // Step 1: Find the user by email and check account status.
                    using (var command = new SqlCommand(@"
                SELECT Id, UserName, Password, IsFirstLogin
                FROM dbo.[User]
                WHERE Email = @Email
                  AND Status = 1
                  AND IsLockedOut = 0;",
                        connection))
                    {
                        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255)
                            .Value = model.Email.Trim();

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                result.UserId = 0;
                                result.Role = "Invalid email or password.";
                                return result;
                            }

                            userId = reader.GetInt32(0);
                            userName = reader.IsDBNull(1)
                                ? null
                                : reader.GetString(1);
                            passwordHash = reader.IsDBNull(2)
                                ? null
                                : reader.GetString(2);
                            isFirstLogin = reader.IsDBNull(3)
                                ? true
                                : reader.GetBoolean(3);
                        }
                    }

                    // Step 2: Verify the password against its stored hash.
                    if (!PasswordHashHelper.VerifyPassword(
                            passwordHash, model.Password))
                    {
                        result.UserId = 0;
                        result.Role = "Invalid email or password.";
                        return result;
                    }

                    // Step 3: Retrieve the user's active role assignment.
                    using (var command = new SqlCommand(@"
                SELECT TOP 1 R.Title
                FROM dbo.RoleAssign RA
                INNER JOIN dbo.Role R
                    ON R.Id = RA.RoleId
                WHERE RA.UserId = @UserId
                  AND RA.Status = 1
                  AND R.Status = 1
                ORDER BY RA.Id;",
                        connection))
                    {
                        command.Parameters.Add("@UserId", SqlDbType.Int)
                            .Value = userId;

                        var role = command.ExecuteScalar();

                        if (role == null || role == DBNull.Value)
                        {
                            result.UserId = 0;
                            result.Role = "No active role assigned.";
                            return result;
                        }

                        result.UserId = userId;
                        result.UserName = userName;
                        result.Email = model.Email;
                        result.Role = Convert.ToString(role);
                        result.IsFirstLogin = isFirstLogin;
                    }
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/AuthenticateUser", "POST",
                    0, ip, "AuthenticateUser");

                result.UserId = 0;
                result.Role = "Unable to log in. Please try again.";
            }

            return result;
        }


        public List<RoleViewModel> GetRoles()
        {
            var roles = new List<RoleViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"
        SELECT Id, Title, Status
        FROM dbo.Role
        WHERE Status = 1
        ORDER BY Title;", connection))
            {
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new RoleViewModel
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Status = reader.GetBoolean(2)
                        });
                    }
                }
            }

            return roles;
        }

        public bool AddRole(string title, string userName, string ip)
        {
            if (string.IsNullOrWhiteSpace(title))
                return false;

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"
        IF EXISTS (
            SELECT 1 FROM dbo.Role
            WHERE Title = @Title
        )
            SELECT 0;
        ELSE
        BEGIN
            INSERT INTO dbo.Role
                (Title, CreatedBy, CreatedAt, UpdatedAt,
                 Status, UserHostAdd)
            VALUES
                (@Title, @CreatedBy, GETDATE(), GETDATE(),
                 1, @IP);

            SELECT 1;
        END;", connection))
            {
                command.Parameters.Add("@Title", SqlDbType.NVarChar, 100)
                    .Value = title.Trim();

                command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100)
                    .Value = (object)userName ?? DBNull.Value;

                command.Parameters.Add("@IP", SqlDbType.NVarChar, 100)
                    .Value = (object)ip ?? DBNull.Value;

                connection.Open();

                return Convert.ToInt32(command.ExecuteScalar()) == 1;
            }
        }

        public bool RegisterUser(
    RegisterViewModel model,
    string userName,
    string ip)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Prevent duplicate email addresses.
                        using (var check = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM dbo.[User]
                    WHERE Email = @Email;",
                            connection, transaction))
                        {
                            check.Parameters.Add("@Email", SqlDbType.NVarChar, 255)
                                .Value = model.Email.Trim();

                            if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        // Validate that the selected role is active.
                        using (var checkRole = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM dbo.Role
                    WHERE Id = @RoleId AND Status = 1;",
                            connection, transaction))
                        {
                            checkRole.Parameters.Add("@RoleId", SqlDbType.Int)
                                .Value = model.RoleId.Value;

                            if (Convert.ToInt32(checkRole.ExecuteScalar()) == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        // Hash before saving.
                        string passwordHash =
                            PasswordHashHelper.HashPassword(model.Password);

                        int userId;

                        using (var command = new SqlCommand(@"
                    INSERT INTO dbo.[User]
                    (
                        UserName, Email, Password, IsLockedOut, ProfileImage,
                        CreatedBy, CreatedAt, UpdatedAt,
                        Status, UserHostAdd, IsFirstLogin
                    )
                    VALUES
                    (
                        @UserName, @Email, @Password, 0, NULL,
                        @CreatedBy, GETDATE(), GETDATE(),
                        1, @IP, 1
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);",
                            connection, transaction))
                        {
                            command.Parameters.Add("@UserName", SqlDbType.NVarChar, 100)
                                .Value = model.UserName.Trim();

                            command.Parameters.Add("@Email", SqlDbType.NVarChar, 255)
                                .Value = model.Email.Trim();

                            command.Parameters.Add("@Password", SqlDbType.NVarChar, 255)
                                .Value = passwordHash;

                            command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100)
                                .Value = (object)userName ?? DBNull.Value;

                            command.Parameters.Add("@IP", SqlDbType.NVarChar, 100)
                                .Value = (object)ip ?? DBNull.Value;

                            userId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        using (var command = new SqlCommand(@"
                    INSERT INTO dbo.RoleAssign
                    (
                        UserId, RoleId, CreatedBy,
                        CreatedAt, UpdatedAt, Status, UserHostAdd
                    )
                    VALUES
                    (
                        @UserId, @RoleId, @CreatedBy,
                        GETDATE(), GETDATE(), 1, @IP
                    );",
                            connection, transaction))
                        {
                            command.Parameters.Add("@UserId", SqlDbType.Int)
                                .Value = userId;

                            command.Parameters.Add("@RoleId", SqlDbType.Int)
                                .Value = model.RoleId.Value;

                            command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100)
                                .Value = (object)userName ?? DBNull.Value;

                            command.Parameters.Add("@IP", SqlDbType.NVarChar, 100)
                                .Value = (object)ip ?? DBNull.Value;

                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<RegisteredUserViewModel> GetUsers()
        {
            var users = new List<RegisteredUserViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"
        SELECT
            U.Id,
            U.UserName,
            U.Email,
            R.Title AS RoleTitle,
            U.Status
        FROM dbo.[User] U
        LEFT JOIN dbo.RoleAssign RA
            ON RA.UserId = U.Id AND RA.Status = 1
        LEFT JOIN dbo.Role R
            ON R.Id = RA.RoleId
        ORDER BY U.Id DESC;", connection))
            {
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new RegisteredUserViewModel
                        {
                            Id = reader.GetInt32(0),
                            UserName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            RoleTitle = reader.IsDBNull(3) ? "No role" : reader.GetString(3),
                            Status = reader.GetBoolean(4)
                        });
                    }
                }
            }

            return users;
        }


        public bool ChangeFirstLoginPassword(
            int userId,
            string currentPassword,
            string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string currentHash;

                        using (var command = new SqlCommand(@"
                    SELECT Password
                    FROM dbo.[User]
                    WHERE Id = @UserId
                      AND Status = 1
                      AND IsLockedOut = 0
                      AND IsFirstLogin = 1;",
                            connection, transaction))
                        {
                            command.Parameters.Add("@UserId", SqlDbType.Int)
                                .Value = userId;

                            var value = command.ExecuteScalar();

                            if (value == null || value == DBNull.Value)
                            {
                                transaction.Rollback();
                                return false;
                            }

                            currentHash = Convert.ToString(value);
                        }

                        if (!PasswordHashHelper.VerifyPassword(
                            currentHash, currentPassword))
                        {
                            transaction.Rollback();
                            return false;
                        }

                        string newHash =
                            PasswordHashHelper.HashPassword(newPassword);

                        using (var command = new SqlCommand(@"
                    UPDATE dbo.[User]
                    SET Password = @Password,
                        IsFirstLogin = 0,
                        UpdatedAt = GETDATE()
                    WHERE Id = @UserId
                      AND IsFirstLogin = 1
                      AND Status = 1
                      AND IsLockedOut = 0;",
                            connection, transaction))
                        {
                            command.Parameters.Add("@Password", SqlDbType.NVarChar, 255)
                                .Value = newHash;

                            command.Parameters.Add("@UserId", SqlDbType.Int)
                                .Value = userId;

                            int rows = command.ExecuteNonQuery();

                            if (rows != 1)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool ChangeUserPassword(
    int userId,
    string currentPassword,
    string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string storedPassword;
                        bool isFirstLogin;

                        using (var command = new SqlCommand(@"
                    SELECT Password, IsFirstLogin
                    FROM dbo.[User]
                    WHERE Id = @UserId
                      AND Status = 1
                      AND IsLockedOut = 0;", connection, transaction))
                        {
                            command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                            using (var reader = command.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    transaction.Rollback();
                                    return false;
                                }

                                storedPassword = reader["Password"].ToString();
                                isFirstLogin = Convert.ToBoolean(reader["IsFirstLogin"]);
                            }
                        }

                        // First-login users must use the forced-change flow.
                        if (isFirstLogin)
                        {
                            transaction.Rollback();
                            return false;
                        }

                        if (!PasswordHashHelper.VerifyPassword(
                                storedPassword, currentPassword))
                        {
                            transaction.Rollback();
                            return false;
                        }

                        string hashedPassword =
                            PasswordHashHelper.HashPassword(newPassword);

                        using (var command = new SqlCommand(@"
                    UPDATE dbo.[User]
                    SET Password = @Password,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @UserId
                      AND Status = 1
                      AND IsLockedOut = 0
                      AND IsFirstLogin = 0;", connection, transaction))
                        {
                            command.Parameters.Add("@Password", SqlDbType.NVarChar, 255)
                                .Value = hashedPassword;

                            command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime)
                                .Value = DateTime.UtcNow;

                            command.Parameters.Add("@UserId", SqlDbType.Int)
                                .Value = userId;

                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected != 1)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool ResetUserPassword(int userId, string defaultPassword)
        {
            string hashedPassword = PasswordHashHelper.HashPassword(defaultPassword);

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(@"
            UPDATE dbo.[User]
            SET Password = @Password,
                IsFirstLogin = 1,
                UpdatedAt = @UpdatedAt
            WHERE Id = @UserId
              AND Status = 1;", connection))
                {
                    command.Parameters.Add(
                        "@Password", SqlDbType.NVarChar, 256).Value = hashedPassword;

                    command.Parameters.Add(
                        "@UpdatedAt", SqlDbType.DateTime).Value = DateTime.UtcNow;

                    command.Parameters.Add(
                        "@UserId", SqlDbType.Int).Value = userId;

                    return command.ExecuteNonQuery() == 1;
                }
            }
        }

    }
}
