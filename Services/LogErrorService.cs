using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class LogErrorService
    {
        private readonly string _connectionString;

        public LogErrorService(string connectionString)
        {
            _connectionString = connectionString;
        }
        public void LogError(Exception ex, string url = null, string method = null, int? userId = null, string ipAddress = null, string procedureName = null)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                ipAddress = "Unknown";
            }
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("InsertErrorLog", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ErrorMessage", ex.Message);
                        cmd.Parameters.AddWithValue("@ErrorSource", ex.Source ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@StackTrace", ex.StackTrace ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ErrorType", ex.GetType().ToString());
                        cmd.Parameters.AddWithValue("@Url", url ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Method", method ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserId", userId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IPAddress", ipAddress ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ProcedureName", procedureName ?? (object)DBNull.Value);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void LogErrorInTryBlock(string error, string url = null, string method = null, int? userId = null, string ipAddress = null, string procedureName = null)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                ipAddress = "Unknown";
            }
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("InsertErrorLog", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ErrorMessage", error);
                        cmd.Parameters.AddWithValue("@ErrorSource", "Inside the try block");
                        cmd.Parameters.AddWithValue("@StackTrace", "Inside the try block");
                        cmd.Parameters.AddWithValue("@ErrorType", "Inside the try block");
                        cmd.Parameters.AddWithValue("@Url", url ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Method", method ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserId", userId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IPAddress", ipAddress ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ProcedureName", procedureName ?? (object)DBNull.Value);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}
