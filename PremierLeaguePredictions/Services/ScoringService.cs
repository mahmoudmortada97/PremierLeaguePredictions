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

            var scoredUsers = userRankings
                .Select(userRanking =>
                {
                    var breakdown = CalculateBreakdown(realOrder, userRanking.Rankings);

                    return new
                    {
                        UserEmail = userRanking.UserEmail,
                        UserName = userRanking.UserName,
                        Rankings = userRanking.Rankings,
                        Score = breakdown.Score,
                        ExactCount = breakdown.ExactCount,
                        OffByOneCount = breakdown.OffByOneCount,
                        OffByTwoCount = breakdown.OffByTwoCount,
                        RandomTieBreaker = Random.Shared.Next()
                    };
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.ExactCount)
                .ThenByDescending(x => x.OffByOneCount)
                .ThenByDescending(x => x.OffByTwoCount)
                .ThenByDescending(x => x.RandomTieBreaker)
                .ToList();

            var results = scoredUsers
                .Select(x => new UserRankingDTO
                {
                    UserEmail = x.UserEmail,
                    UserName = x.UserName,
                    UserScore = x.Score,
                    Rankings = x.Rankings
                })
                .ToList();

            var rankedStandings = scoredUsers
                .Select((x, index) => new Standings
                {
                    UserEmail = x.UserEmail,
                    UserName = x.UserName,
                    Score = x.Score,
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
            return CalculateBreakdown(realOrder, userPredictedOrder).Score;
        }

        private static ScoreBreakdown CalculateBreakdown(
            Dictionary<string, int> realOrder,
            Dictionary<string, int> userPredictedOrder)
        {
            int score = 0;
            int exactCount = 0;
            int offByOneCount = 0;
            int offByTwoCount = 0;

            foreach (var team in realOrder.Keys)
            {
                if (!userPredictedOrder.TryGetValue(team, out int predictedRank))
                    continue;

                int diff = Math.Abs(realOrder[team] - predictedRank);

                switch (diff)
                {
                    case 0:
                        score += 10;
                        exactCount++;
                        break;
                    case 1:
                        score += 5;
                        offByOneCount++;
                        break;
                    case 2:
                        score += 1;
                        offByTwoCount++;
                        break;
                }
            }

            return new ScoreBreakdown
            {
                Score = score,
                ExactCount = exactCount,
                OffByOneCount = offByOneCount,
                OffByTwoCount = offByTwoCount
            };
        }

        private async Task<List<UserRankingDTO>> FetchUserRankingsAsync()
        {
            var response = await _httpClient.GetStringAsync(
                $"form/{_formId}/submissions?apiKey={_apiKey}&limit=1000");

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
                    foreach (var ranking in teamRankingAnswer.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = ranking.Split(':', 2);
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

        private sealed class ScoreBreakdown
        {
            public int Score { get; set; }
            public int ExactCount { get; set; }
            public int OffByOneCount { get; set; }
            public int OffByTwoCount { get; set; }
        }
    }
}