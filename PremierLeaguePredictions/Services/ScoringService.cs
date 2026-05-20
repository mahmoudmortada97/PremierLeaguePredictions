using Newtonsoft.Json.Linq;
using PremierLeaguePredictions.Models;

namespace PremierLeaguePredictions.Services
{
    public class ScoringService
    {
        private readonly HttpClient _httpClient;
        private readonly string _formId;
        private readonly string _apiKey;

        public ScoringService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("JotForm");
            _formId = configuration["JotForm:FormId"]
                ?? throw new InvalidOperationException("JotForm:FormId is not configured.");
            _apiKey = configuration["JotForm:ApiKey"]
                ?? throw new InvalidOperationException("JotForm:ApiKey is not configured.");
        }

        public async Task<ScoringResultsResponse> CalculateScoresAsync(Dictionary<string, int> realOrder)
        {
            var userRankings = await FetchUserRankingsAsync();

            var results = new List<UserRankingDTO>();
            var standings = new List<Standings>();

            foreach (var userRanking in userRankings)
            {
                int score = CalculateScore(realOrder, userRanking.Rankings);

                results.Add(new UserRankingDTO
                {
                    UserEmail = userRanking.UserEmail,
                    UserName = userRanking.UserName,
                    UserScore = score,
                    Rankings = userRanking.Rankings
                });

                standings.Add(new Standings
                {
                    UserEmail = userRanking.UserEmail,
                    UserName = userRanking.UserName,
                    Score = score
                });
            }

            var rankedStandings = standings
                .OrderByDescending(u => u.Score)
                .Select((u, index) => new Standings
                {
                    UserEmail = u.UserEmail,
                    UserName = u.UserName,
                    Score = u.Score,
                    Position = index + 1
                })
                .ToList();

            return new ScoringResultsResponse
            {
                RealOrder = realOrder,
                Results = results,
                Standings = rankedStandings
            };
        }

        public static int CalculateScore(
            Dictionary<string, int> realOrder,
            Dictionary<string, int> userPredictedOrder)
        {
            int score = 0;

            foreach (var team in realOrder.Keys)
            {
                if (!userPredictedOrder.TryGetValue(team, out int predictedRank)) continue;

                int diff = Math.Abs(realOrder[team] - predictedRank);

                score += diff switch
                {
                    0 => 10,
                    1 => 5,
                    2 => 1,
                    _ => 0
                };
            }

            return score;
        }

        private async Task<List<UserRankingDTO>> FetchUserRankingsAsync()
        {
            var response = await _httpClient.GetStringAsync(
                $"form/{_formId}/submissions?apiKey={_apiKey}");

            var json = JObject.Parse(response);
            var userRankings = new List<UserRankingDTO>();

            foreach (var submission in json["content"]!)
            {
                var userRanking = new UserRankingDTO
                {
                    UserName = submission["answers"]?["13"]?["prettyFormat"]?.ToString(),
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
                            var teamName = parts[1].Trim().Replace("&amp;", "&");
                            userRanking.Rankings[teamName] = rank;
                        }
                    }
                }

                userRankings.Add(userRanking);
            }

            return userRankings;
        }
    }
}