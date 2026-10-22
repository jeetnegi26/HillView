using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    // Sends HTML mail through the SMTP settings in Web.config
    // (<system.net><mailSettings>). See the setup notes for GoDaddy.
    public class EmailService
    {
        private readonly LogErrorService _logErrorService;

        public EmailService(LogErrorService logErrorService)
        {
            _logErrorService = logErrorService;
        }

        public async Task SendAsync(
            IEnumerable<string> toAddresses,
            string subject,
            string htmlBody,
            string replyTo = null)
        {
            string fromAddress = ConfigurationManager.AppSettings["config:MailFrom"];
            string fromName = ConfigurationManager.AppSettings["config:MailFromName"];

            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                throw new InvalidOperationException(
                    "AppSettings key 'config:MailFrom' is missing.");
            }

            using (var message = new MailMessage())
            {
                // Always send FROM an address on your own domain.
                message.From = new MailAddress(fromAddress, fromName);

                foreach (string to in toAddresses)
                {
                    if (!string.IsNullOrWhiteSpace(to))
                    {
                        message.To.Add(new MailAddress(to.Trim()));
                    }
                }

                if (message.To.Count == 0)
                {
                    throw new InvalidOperationException("No email recipient was provided.");
                }

                // The visitor's address goes in Reply-To, never in From.
                if (!string.IsNullOrWhiteSpace(replyTo))
                {
                    message.ReplyToList.Add(new MailAddress(replyTo.Trim()));
                }

                message.Subject = CleanHeader(subject);
                message.SubjectEncoding = Encoding.UTF8;
                message.BodyEncoding = Encoding.UTF8;
                message.IsBodyHtml = true;
                message.Body = htmlBody;

                // Host and port come from <system.net><mailSettings> in Web.config
                // Host and credentials set in code, for this test only
                System.Net.ServicePointManager.SecurityProtocol |=
                    System.Net.SecurityProtocolType.Tls12;

                using (var client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new System.Net.NetworkCredential(
                        "ed68608@gmail.com", "zajbbxwkzobeuwao");
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    await client.SendMailAsync(message);
                }
            }
        }

        // Removes line breaks so user text can never inject extra mail headers.
        private static string CleanHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            value = value.Replace("\r", " ").Replace("\n", " ").Trim();

            return value.Length > 200 ? value.Substring(0, 200) : value;
        }
    }
}