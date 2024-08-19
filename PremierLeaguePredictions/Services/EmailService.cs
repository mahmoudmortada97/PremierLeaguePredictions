using PremierLeaguePredictions.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace PremierLeaguePredictions.Services
{
    public class EmailService
    {
        protected readonly IConfiguration _configuration;
        private bool disposedValue;

        private string _leaderboardHtml;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public string SendEmails(EmailDTO emails)
        {
            if (emails == null)
                throw new ArgumentNullException(nameof(emails));

            if (string.IsNullOrEmpty(_leaderboardHtml))
                throw new InvalidOperationException("Leaderboard HTML is not built. Call BuildLeaderboard first.");

            var results = new StringBuilder();

            foreach (var emailDto in emails.PersonEmails)
            {
                try
                {
                    string emailBody = BuildEmail(emailDto.UserName, emailDto.UserScore);

                    // Send email
                    var success = Send(emailDto.UserEmail, emailBody).Result;

                    if (success)
                    {
                        results.AppendLine($"Email sent successfully to {emailDto.UserEmail}");
                    }
                    else
                    {
                        results.AppendLine($"Failed to send email to {emailDto.UserEmail}");
                    }
                }
                catch (Exception ex)
                {
                    results.AppendLine($"Exception while sending email to {emailDto.UserEmail}: {ex.Message}");
                }
            }

            return results.ToString();
        }

        private async Task<bool> Send(string to, string body)
        {
            try
            {
                string fromAddress = _configuration.GetValue<string>("Email:From") ?? string.Empty;
                string fromDisplayName = _configuration.GetValue<string>("Email:DisplayName") ?? string.Empty;

                MailAddress addressFrom = new MailAddress(fromAddress, fromDisplayName);
                MailAddress addressTo = new MailAddress(to);

                using (MailMessage message = new MailMessage())
                {
                    message.BodyEncoding = Encoding.UTF8;
                    message.From = addressFrom;
                    message.To.Add(addressTo);
                    message.Subject = "Premier League Predictions 2025";
                    message.IsBodyHtml = true;
                    message.Body = body;

                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Port = Convert.ToInt32(_configuration.GetValue<string>("Email:Port"));
                        smtp.Host = _configuration.GetValue<string>("Email:Server") ?? string.Empty;
                        smtp.EnableSsl = true;
                        smtp.UseDefaultCredentials = false;
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.Credentials = new NetworkCredential(fromAddress, _configuration.GetValue<string>("Email:Password"));

                        await smtp.SendMailAsync(message);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string BuildEmail(string userName, int score)
        {
            string html = string.Empty;

            try
            {
                string? templateFilePath = _configuration.GetValue<string>("Email:EmailTemplate");

                if (templateFilePath is not null)
                {
                    using (StreamReader reader = File.OpenText(templateFilePath))
                    {
                        html = reader.ReadToEnd();
                    }
                }

                // Replace username and score placeholders
                html = html.Replace("*|UserName|*", userName);
                html = html.Replace("*|Score|*", score.ToString());

                // Replace the leaderboard placeholder with pre-built HTML
                html = html.Replace("*|Leaderboard|*", _leaderboardHtml);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return html;
        }


        public void BuildLeaderboard(List<UserScore> leaderboard)
        {
            //Todo  : Fix The Table View in Email
            var leaderboardHtml = new StringBuilder();
            int rank = 1;
            foreach (var userScore in leaderboard.OrderByDescending(u => u.Score))
            {
                string medalClass = rank == 1 ? "gold" :
                                    rank == 2 ? "silver" :
                                    rank == 3 ? "bronze" : "";

                string medalIcon = rank <= 3 ? $"<i class=\"fas fa-medal {medalClass}\"></i>" : rank.ToString();

                leaderboardHtml.AppendLine($@"
                                    <tr>
                                        <td class=\`medal\`>{medalIcon}</td>
                                        < td >{userScore.UserName}</ td >
                                        < td >{userScore.Score}</ td >
                                    </ tr > ");

                rank++;
            }

            _leaderboardHtml = leaderboardHtml.ToString();
        }

    }
}
