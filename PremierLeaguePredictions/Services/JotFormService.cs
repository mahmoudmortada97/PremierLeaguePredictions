using Newtonsoft.Json.Linq;
using PremierLeaguePredictions.Models;

namespace PremierLeaguePredictions.Services
{
    public class JotFormService
    {
        private readonly HttpClient _httpClient;
        private readonly string _formId;
        private readonly string _apiKey;

        public JotFormService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("JotForm");
            _formId = configuration["JotForm:FormId"]
                ?? throw new InvalidOperationException("JotForm:FormId is not configured.");
            _apiKey = configuration["JotForm:ApiKey"]
                ?? throw new InvalidOperationException("JotForm:ApiKey is not configured.");
        }

        public async Task<Dictionary<string, int>> FetchRealRankingsAsync()
        {
            var response = await _httpClient.GetStringAsync(BuildSubmissionsUrl());
            var json = JObject.Parse(response);
            var rankings = new Dictionary<string, int>();

            foreach (var submission in json["content"]!)
            {
                var teamRankingAnswer = submission["answers"]?["12"]?["answer"]?.ToString();

                if (string.IsNullOrEmpty(teamRankingAnswer)) continue;

                foreach (var ranking in teamRankingAnswer.Split('\n'))
                {
                    var parts = ranking.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int rank))
                    {
                        var teamName = DecodeTeamName(parts[1]);
                        rankings[teamName] = rank;
                    }
                }
            }

            return rankings;
        }

        public async Task<List<UserRankingDTO>> FetchUserRankingsAsync()
        {
            var response = await _httpClient.GetStringAsync(BuildSubmissionsUrl());
            var json = JObject.Parse(response);
            var userRankings = new List<UserRankingDTO>();

            foreach (var submission in json["content"]!)
            {
                string userName = ParseUserName(submission);

                var userRanking = new UserRankingDTO
                {
                    UserName = userName,
                    UserEmail = submission["answers"]?["3"]?["answer"]?.ToString(),
                    Rankings = new Dictionary<string, int>()
                };

                var teamRankingAnswer = submission["answers"]?["12"]?["answer"]?.ToString();

                if (!string.IsNullOrEmpty(teamRankingAnswer))
                {
                    foreach (var ranking in teamRankingAnswer.Split('\n'))
                    {
                        var parts = ranking.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int rank))
                        {
                            var teamName = DecodeTeamName(parts[1]);
                            userRanking.Rankings[teamName] = rank;
                        }
                    }
                }

                userRankings.Add(userRanking);
            }

            return userRankings;
        }

        private string BuildSubmissionsUrl() =>
            $"form/{_formId}/submissions?apiKey={_apiKey}";

        private static string ParseUserName(JToken submission)
        {
            var userNameJson = submission["answers"]?["13"]?["answer"]?.ToString();

            if (string.IsNullOrEmpty(userNameJson)) return string.Empty;

            try
            {
                var obj = JObject.Parse(userNameJson);
                var first = obj["first"]?.ToString();
                var last = obj["last"]?.ToString();
                return $"{first} {last}".Trim();
            }
            catch
            {
                return userNameJson;
            }
        }

        private static string DecodeTeamName(string raw) =>
            raw.Trim().Replace("&amp;", "&");
    }
}