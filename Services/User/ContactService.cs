using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using DTOs;

namespace Services
{
    public enum ContactSubmitResult
    {
        Saved,
        TooManyRequests
    }

    public class ContactService
    {
        // At most 3 submissions per IP address in any 10 minutes.
        private const int MaxSubmissions = 3;
        private const int WindowMinutes = 10;

        // Email templates live here. App_Data cannot be opened from the browser.
        private const string TemplateFolder = "~/App_Data/EmailTemplates/";

        private readonly string _connectionString;
        private readonly LogErrorService _logErrorService;
        private readonly EmailService _emailService;

        public ContactService(
            string connectionString,
            LogErrorService logErrorService,
            EmailService emailService)
        {
            _connectionString = connectionString;
            _logErrorService = logErrorService;
            _emailService = emailService;
        }

        public async Task<ContactSubmitResult> SubmitAsync(
            ContactVM model, string ip, string pagePath)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            string cleanIp = string.IsNullOrWhiteSpace(ip)
                ? "unknown"
                : Truncate(ip.Trim(), 45);

            try
            {
                if (await CountRecentAsync(cleanIp) >= MaxSubmissions)
                {
                    return ContactSubmitResult.TooManyRequests;
                }

                await SaveAsync(model, cleanIp, pagePath);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Home/SubmitContact", "POST", 0, cleanIp, "SubmitAsync");

                throw;
            }

            // The enquiry is already saved. A mail problem is logged but must
            // not make the visitor think the form failed.
            await SendEmailsAsync(model, cleanIp);

            return ContactSubmitResult.Saved;
        }

        private async Task<int> CountRecentAsync(string ip)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"
                SELECT COUNT(1)
                FROM ContactUs
                WHERE UserHostAdd = @UserHostAdd
                  AND CreatedAt > @Since;", connection))
            {
                AddParam(command, "@UserHostAdd", SqlDbType.VarChar, 45, ip);

                command.Parameters.Add("@Since", SqlDbType.DateTime2).Value =
                    DateTime.UtcNow.AddMinutes(-WindowMinutes);

                await connection.OpenAsync();

                return (int)await command.ExecuteScalarAsync();
            }
        }

        private async Task SaveAsync(ContactVM model, string ip, string pagePath)
        {
            string fullName = model.FullName.Trim();
            string phone = string.IsNullOrWhiteSpace(model.Phone)
                ? null
                : model.Phone.Trim();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"
                INSERT INTO ContactUs
                (
                    Id, CreatedBy, CreatedAt, UpdatedAt, Status,
                    UserHostAdd, RewriteUrl, FullName, Email, Phone, Message
                )
                VALUES
                (
                    @Id, @CreatedBy, SYSUTCDATETIME(), SYSUTCDATETIME(), 1,
                    @UserHostAdd, @RewriteUrl, @FullName, @Email, @Phone, @Message
                );", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value =
                    Guid.NewGuid();

                AddParam(command, "@CreatedBy", SqlDbType.NVarChar, 250, fullName);
                AddParam(command, "@UserHostAdd", SqlDbType.VarChar, 45, ip);
                AddParam(command, "@RewriteUrl", SqlDbType.NVarChar, 250,
                    Truncate(pagePath, 250));
                AddParam(command, "@FullName", SqlDbType.NVarChar, 250, fullName);
                AddParam(command, "@Email", SqlDbType.NVarChar, 250, model.Email.Trim());
                AddParam(command, "@Phone", SqlDbType.VarChar, 20, phone);
                AddParam(command, "@Message", SqlDbType.NVarChar, -1, model.Message.Trim());

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task SendEmailsAsync(ContactVM model, string ip)
        {
            // Every value placed in a template is HTML-encoded first.
            string name = WebUtility.HtmlEncode(model.FullName.Trim());
            string email = WebUtility.HtmlEncode(model.Email.Trim());
            string phone = string.IsNullOrWhiteSpace(model.Phone)
                ? "-"
                : WebUtility.HtmlEncode(model.Phone.Trim());
            string message = WebUtility.HtmlEncode(model.Message.Trim())
                .Replace("\r\n", "<br/>")
                .Replace("\n", "<br/>");

            // Replies to the confirmation mail go to the first admin address.
            string adminReplyTo = null;

            // 1) Mail to the admin. Reply-To is the visitor, so "Reply" goes to them.
            try
            {
                string adminSetting = ConfigurationManager.AppSettings["config:MailAdminTo"];

                if (string.IsNullOrWhiteSpace(adminSetting))
                {
                    throw new InvalidOperationException(
                        "AppSettings key 'config:MailAdminTo' is missing.");
                }

                string[] adminAddresses = adminSetting.Split(
                    new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (adminAddresses.Length > 0)
                {
                    adminReplyTo = adminAddresses[0].Trim();
                }

                var values = BaseValues();
                values["Preheader"] = "New enquiry from " + name;
                values["FullName"] = name;
                values["Email"] = email;
                values["Phone"] = phone;
                values["Message"] = message;
                values["IpAddress"] = WebUtility.HtmlEncode(ip);
                values["ReceivedAt"] =
                    DateTime.UtcNow.ToString("dd MMM yyyy, HH:mm") + " UTC";

                string adminBody = RenderTemplate("ContactAdminTemplate.html", values);

                await _emailService.SendAsync(
                    adminAddresses,
                    "New contact enquiry from " + model.FullName.Trim(),
                    adminBody,
                    model.Email);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Home/SubmitContact", "POST", 0, ip, "SendAdminEmail");
            }

            // 2) Confirmation mail to the visitor.
            try
            {
                var values = BaseValues();
                values["Preheader"] =
                    "We have received your message and will get back to you shortly.";
                values["FullName"] = name;
                values["Message"] = message;

                string userBody = RenderTemplate("ContactUserTemplate.html", values);

                await _emailService.SendAsync(
                    new[] { model.Email },
                    "We have received your message",
                    userBody,
                    adminReplyTo);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Home/SubmitContact", "POST", 0, ip, "SendUserEmail");
            }
        }

        // Values shared by every template: site name, logo, year.
        private static Dictionary<string, string> BaseValues()
        {
            string siteName =
                ConfigurationManager.AppSettings["config:MailFromName"] ?? "Our team";

            // Optional: full https:// address of your logo image.
            string logoUrl = ConfigurationManager.AppSettings["config:MailLogoUrl"];

            string encodedName = WebUtility.HtmlEncode(siteName);

            string logoHtml = string.IsNullOrWhiteSpace(logoUrl)
                ? "<span style=\"font-family:Arial,Helvetica,sans-serif;font-size:22px;" +
                  "font-weight:bold;color:#ffffff;\">" + encodedName + "</span>"
                : "<img src=\"" + WebUtility.HtmlEncode(logoUrl.Trim()) + "\" alt=\"" +
                  encodedName + "\" height=\"40\" " +
                  "style=\"display:block;border:0;height:40px;\" />";

            return new Dictionary<string, string>
            {
                { "SiteName", encodedName },
                { "Year", DateTime.UtcNow.Year.ToString() },
                { "LogoHtml", logoHtml }
            };
        }

        // Loads an .html template and replaces every {{Token}} with its value.
        private static string RenderTemplate(
            string fileName, IDictionary<string, string> values)
        {
            string path = HostingEnvironment.MapPath(TemplateFolder + fileName);

            if (path == null || !File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Email template not found: " + TemplateFolder + fileName);
            }

            string html = File.ReadAllText(path, Encoding.UTF8);

            foreach (var pair in values)
            {
                html = html.Replace("{{" + pair.Key + "}}", pair.Value ?? string.Empty);
            }

            return html;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private static void AddParam(
            SqlCommand command, string name, SqlDbType type, int size, string value)
        {
            var parameter = command.Parameters.Add(name, type, size);
            parameter.Value = (object)value ?? DBNull.Value;
        }
    }
}