using Hangfire;
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
        private string _finalRankingHtml;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void BuildLeaderboard(List<PersonEmailDTO> leaderboard)
        {
            //Todo  : Fix The Table View in Email
            var leaderboardHtml = new StringBuilder();
            int rank = 1;
            foreach (var user in leaderboard.OrderByDescending(u => u.UserScore))
            {
                string medalClass = rank == 1 ? "gold" :
                    rank == 2 ? "silver" :
                    rank == 3 ? "#cd7f32" : "transparent";  // Bronze color and default transparent

                string color = $"style='background-color:{medalClass};text-align: center;font-size: large;'";
                string textStyle = "style='font-weight: bold; font-size: larger;'";
                string rowId = $"'{user.UserEmail}'";


                leaderboardHtml.AppendLine($"<tr id={rowId}><td {color}>{rank}</td><td {textStyle}>{user.UserName}</td><td {textStyle}>{user.UserScore}</td></tr>");

                rank++;
            }

            _leaderboardHtml = leaderboardHtml.ToString();
        }


        public void BuildFinalOrder(Dictionary<string, int> finalRanking)
        {

            _finalRankingHtml = BuildRankingHtml(finalRanking);
        }


        public async Task SendEmails(EmailDTO emails)
        {
            if (emails == null)
                throw new ArgumentNullException(nameof(emails));

            if (string.IsNullOrEmpty(_leaderboardHtml))
                throw new InvalidOperationException("Leaderboard HTML is not built. Call BuildLeaderboard first.");

            if (string.IsNullOrEmpty(_finalRankingHtml))
                throw new InvalidOperationException("Final Ranking HTML is not built. Call Final Ranking first.");

            var results = new StringBuilder();

            foreach (var emailDto in emails.PersonEmails)
            {
                try
                {
                    string emailBody = BuildEmail(emailDto.UserName, emailDto.UserScore, emailDto.UserPredictedOrder);
                    //await HighlightUserRow($"{emailDto.UserEmail}", _leaderboardHtml);

                    // Send email
                    BackgroundJob.Enqueue(() => Send(emailDto.UserEmail, emailBody));

                }
                catch (Exception ex)
                {
                }
            }
            await Task.CompletedTask;

        }

        private string BuildEmail(string userName, int score, Dictionary<string, int> userPredictionRanking)
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

                var userPredictionRankingHtml = BuildRankingHtml(userPredictionRanking);

                // Replace username and score placeholders
                html = html.Replace("*|UserName|*", userName);
                html = html.Replace("*|Score|*", score.ToString());

                // Replace the leaderboard placeholder with pre-built HTML
                html = html.Replace("*|Leaderboard|*", _leaderboardHtml);
                html = html.Replace("*|FinalRanking|*", _finalRankingHtml);
                html = html.Replace("*|UserOrderRanking|*", userPredictionRankingHtml);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return html;
        }

        public async Task Send(string to, string body)
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
            }
            catch (SmtpFailedRecipientException ex)
            {
                Console.WriteLine($"Failed to deliver email to . Exception: {ex.Message}");
            }
            catch (Exception ex)
            {

            }
        }



        private string BuildRankingHtml(Dictionary<string, int> Ranking)
        {
            var RankingHtml = new StringBuilder();
            foreach (var rankingItem in Ranking.OrderBy(r => r.Value))
            {
                RankingHtml.AppendLine($"<tr><td>{rankingItem.Value}</td><td>{rankingItem.Key}</td></tr>");
            }
            return RankingHtml.ToString();
        }
        /// TODO : Next Update
        //private async Task HighlightUserRow(string userId, string leaderboard)
        //{
        //    // Initialize the StringBuilder with the current HTML
        //    var updatedHtml = new System.Text.StringBuilder(leaderboard);

        //    // Define the styles
        //    var existingStyle = "style='background-color: gray;'";
        //    var newStyle = "style='background-color: gray;'";

        //    // Remove existing gray background styles
        //    updatedHtml.Replace(existingStyle, string.Empty);

        //    // Define the row start tag and the new style to add
        //    string rowStartTag = $"<tr id='{userId}'";
        //    string styleToAdd = " " + newStyle;

        //    // Find the row containing the userId
        //    int rowStartIndex = updatedHtml.ToString().IndexOf(rowStartTag);

        //    if (rowStartIndex != -1)
        //    {
        //        // Find the closing of the opening <tr> tag
        //        int tagEndIndex = updatedHtml.ToString().IndexOf(">", rowStartIndex);

        //        // Insert the new style attribute before the closing of the opening <tr> tag
        //        updatedHtml.Insert(tagEndIndex, styleToAdd);
        //    }

        //    // Update the _leaderboardHtml with the modified HTML
        //    _leaderboardHtml = updatedHtml.ToString();

        //    await Task.CompletedTask;
        //}


    }
}
