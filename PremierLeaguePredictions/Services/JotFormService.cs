using Newtonsoft.Json.Linq;
using PremierLeaguePredictions.Models;
using RestSharp;

namespace PremierLeaguePredictions.Services
{
    public class JotFormService
    {
        private readonly string _formId;
        private readonly string _apiKey;

        public JotFormService(string formId, string apiKey)
        {
            _formId = formId;
            _apiKey = apiKey;
        }

        public async Task<Dictionary<string, int>> FetchRealRankingsAsync()
        {
            var url = $"https://api.jotform.com/form/{_formId}/submissions?apiKey={_apiKey}";
            var client = new RestClient(url);

            var request = new RestRequest
            {
                Method = Method.Get
            };

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var json = JObject.Parse(response.Content);
                var rankings = new Dictionary<string, int>();

                foreach (var submission in json["content"])
                {
                    var answers = submission["answers"];
                    var teamRankingAnswer = answers["12"]["answer"]?.ToString();

                    if (!string.IsNullOrEmpty(teamRankingAnswer))
                    {
                        var rankingsList = teamRankingAnswer.Split('\n');
                        foreach (var ranking in rankingsList)
                        {
                            var parts = ranking.Split(':');
                            if (parts.Length == 2)
                            {
                                var teamName = parts[1].Trim();
                                var rank = int.Parse(parts[0].Trim());
                                rankings[teamName] = rank;
                            }
                        }
                    }
                }

                return rankings;
            }

            throw new Exception("Failed to fetch real rankings");
        }

        public async Task<List<UserRanking>> FetchUserRankingsAsync()
        {
            var client = new RestClient($"https://api.jotform.com/form/{_formId}/submissions?apiKey={_apiKey}");
            var request = new RestRequest();
            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var json = JObject.Parse(response.Content);
                var userRankings = new List<UserRanking>();

                foreach (var submission in json["content"])
                {
                    var userNameJson = submission["answers"]["13"]["answer"]?.ToString();
                    string userName = string.Empty;

                    if (!string.IsNullOrEmpty(userNameJson))
                    {
                        // Parse the JSON string to extract the first and last names
                        var userNameObject = JObject.Parse(userNameJson);
                        var firstName = userNameObject["first"]?.ToString();
                        var lastName = userNameObject["last"]?.ToString();
                        userName = $"{firstName} {lastName}".Trim();
                    }

                    var userRanking = new UserRanking
                    {
                        UserName = userName,
                        UserEmail = submission["answers"]["3"]["answer"]?.ToString(),
                        Rankings = new Dictionary<string, int>()
                    };

                    var teamRankingAnswer = submission["answers"]["12"]["answer"]?.ToString();

                    if (!string.IsNullOrEmpty(teamRankingAnswer))
                    {
                        var rankingsList = teamRankingAnswer.Split('\n');
                        foreach (var ranking in rankingsList)
                        {
                            var parts = ranking.Split(':');
                            if (parts.Length == 2)
                            {
                                var rank = int.Parse(parts[0].Trim());
                                var teamName = parts[1].Trim();
                                userRanking.Rankings[teamName] = rank;
                            }
                        }
                    }

                    userRankings.Add(userRanking);
                }

                return userRankings;
            }

            throw new Exception("Failed to fetch user rankings");
        }
    }
}
