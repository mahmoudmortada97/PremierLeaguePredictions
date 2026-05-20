using Hangfire;
using PremierLeaguePredictions.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace PremierLeaguePredictions.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly ScoringService _scoringService;
        private readonly HashSet<string> _processedEmails = new(StringComparer.OrdinalIgnoreCase);

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            ScoringService scoringService)
        {
            _configuration = configuration;
            _logger = logger;
            _scoringService = scoringService;
        }

        public async Task SendEmailsAsync(EmailDTO emails)
        {
            if (emails == null) throw new ArgumentNullException(nameof(emails));

            string leaderboardHtml = BuildLeaderboardHtml(emails.UserRankings);
            string finalRankingHtml = BuildRankingHtml(emails.FinalOrder);

            foreach (var emailDto in emails.UserRankings)
            {
                if (!_processedEmails.Add(emailDto.UserEmail))
                {
                    _logger.LogWarning("Skipping duplicate send for {Email}", emailDto.UserEmail);
                    continue;
                }

                try
                {
                    string body = BuildEmail(
                        emailDto.UserName,
                        emailDto.UserScore,
                        emailDto.Rankings,
                        leaderboardHtml,
                        finalRankingHtml);

                    BackgroundJob.Enqueue<EmailService>(svc => svc.Send(emailDto.UserEmail, body));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to enqueue email for {Email}", emailDto.UserEmail);
                }
            }

            await Task.CompletedTask;
        }

        private string BuildEmail(
            string userName,
            int score,
            Dictionary<string, int> userPredictionRanking,
            string leaderboardHtml,
            string finalRankingHtml)
        {
            string? templatePath = _configuration.GetValue<string>("Email:EmailTemplate");

            if (string.IsNullOrEmpty(templatePath))
                throw new InvalidOperationException("Email template path is not configured.");

            string html = File.ReadAllText(templatePath);

            string userRankingHtml = BuildRankingHtml(userPredictionRanking);

            return html
                .Replace("*|UserName|*", userName)
                .Replace("*|Score|*", score.ToString())
                .Replace("*|Leaderboard|*", leaderboardHtml)
                .Replace("*|FinalRanking|*", finalRankingHtml)
                .Replace("*|UserOrderRanking|*", userRankingHtml);
        }

        public async Task Send(string to, string body)
        {
            try
            {
                string fromAddress = _configuration.GetValue<string>("Email:From") ?? string.Empty;
                string fromDisplayName = _configuration.GetValue<string>("Email:DisplayName") ?? string.Empty;

                var addressFrom = new MailAddress(fromAddress, fromDisplayName);
                var addressTo = new MailAddress(to);

                using var message = new MailMessage();
                message.BodyEncoding = Encoding.UTF8;
                message.From = addressFrom;
                message.To.Add(addressTo);
                message.Subject = "Premier League Predictions 2026";
                message.IsBodyHtml = true;
                message.Body = body;

                using var smtp = new SmtpClient();
                smtp.Port = _configuration.GetValue<int>("Email:Port");
                smtp.Host = _configuration.GetValue<string>("Email:Server") ?? string.Empty;
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Credentials = new NetworkCredential(
                    fromAddress,
                    _configuration.GetValue<string>("Email:Password"));

                await smtp.SendMailAsync(message);
            }
            catch (SmtpFailedRecipientException ex)
            {
                _logger.LogError(ex, "Failed to deliver email to {Recipient}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending email to {Recipient}", to);
                throw;
            }
        }

        private string BuildLeaderboardHtml(List<UserRankingDTO> leaderboard)
        {
            var sb = new StringBuilder();
            int rank = 1;

            foreach (var user in leaderboard.OrderByDescending(u => u.UserScore))
            {
                string bgColor = rank switch
                {
                    1 => "gold",
                    2 => "silver",
                    3 => "#cd7f32",
                    _ => "transparent"
                };

                sb.AppendLine(
                    $"<tr id='{user.UserEmail}'>" +
                    $"<td style='background-color:{bgColor};text-align:center;font-size:large'>{rank}</td>" +
                    $"<td style='font-weight:bold;font-size:larger'>{user.UserName}</td>" +
                    $"<td style='font-weight:bold;font-size:larger'>{user.UserScore}</td>" +
                    $"</tr>");

                rank++;
            }

            return sb.ToString();
        }

        private string BuildRankingHtml(Dictionary<string, int> ranking)
        {
            var sb = new StringBuilder();

            foreach (var item in ranking.OrderBy(r => r.Value))
                sb.AppendLine($"<tr><td>{item.Value}</td><td>{item.Key}</td></tr>");

            return sb.ToString();
        }
    }
}