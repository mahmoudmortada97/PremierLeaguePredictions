using Newtonsoft.Json.Linq;
using PremierLeaguePredictions.Models;
using RestSharp;

namespace PremierLeaguePredictions.Services
{
    public class ScoringService
    {
        private readonly string _realOrderUrl;
        private readonly string _userRankingsUrl;

        // Constructor for initializing both URLs
        public ScoringService(string realOrderUrl, string userRankingsUrl)
        {
            _realOrderUrl = realOrderUrl;
            _userRankingsUrl = userRankingsUrl;
        }

        // Constructor for initializing only user rankings URL (assumes realOrder will be fetched or set elsewhere)
        public ScoringService(string userRankingsUrl)
        {
            _userRankingsUrl = userRankingsUrl;
        }

        // Calculate scores for all users
        public async Task<ScoringResultsResponse> CalculateScoresAsync(Dictionary<string, int> realOrder = null)
        {
            try
            {
                // Fetch user rankings and real order
                var userRankings = await FetchUserRankingsAsync();
                var actualRealOrder = realOrder != null && realOrder.Count > 0
                    ? realOrder
                    : await FetchRealOrderAsync();

                var results = new List<UserRankingDTO>();
                var userScores = new List<Standings>();

                // Calculate score for each user
                foreach (var userRanking in userRankings)
                {
                    var score = CalculateScore(actualRealOrder, userRanking.Rankings);

                    results.Add(new UserRankingDTO
                    {
                        UserEmail = userRanking.UserEmail,
                        UserName = userRanking.UserName,
                        UserScore = score,
                        Rankings = userRanking.Rankings
                    });

                    userScores.Add(new Standings
                    {
                        UserEmail = userRanking.UserEmail,
                        UserName = userRanking.UserName,
                        Score = score
                    });
                }

                // Rank users based on their scores
                var sortedScores = userScores
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
                    RealOrder = actualRealOrder,
                    Results = results,
                    Standings = sortedScores
                };
            }
            catch (Exception ex)
            {
                // Log the exception as needed
                throw new Exception("An error occurred while calculating scores", ex);
            }
        }

        // Calculate score based on real order and predicted order
        public static int CalculateScore(Dictionary<string, int> realOrder, Dictionary<string, int> userPredictedOrder)
        {
            int score = 0;

            foreach (var team in realOrder.Keys)
            {
                if (userPredictedOrder.TryGetValue(team, out int predictedRank))
                {
                    int realRank = realOrder[team];
                    int rankDifference = Math.Abs(realRank - predictedRank);

                    if (rankDifference == 0)
                    {
                        score += 10;
                    }
                    else if (rankDifference == 1)
                    {
                        score += 5;
                    }
                    else if (rankDifference == 2)
                    {
                        score += 1;
                    }
                }
            }

            return score;
        }

        // Fetch the real order from an external source
        private async Task<Dictionary<string, int>> FetchRealOrderAsync()
        {
            var client = new RestClient(_realOrderUrl);
            var request = new RestRequest();
            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var json = JObject.Parse(response.Content);
                var realOrder = json["realOrder"].ToObject<Dictionary<string, int>>();
                return realOrder;
            }

            throw new Exception("Failed to fetch real order");
        }

        // Fetch user rankings from an external source
        private async Task<List<UserRankingDTO>> FetchUserRankingsAsync()
        {
            var client = new RestClient(_userRankingsUrl);
            var request = new RestRequest();
            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var json = JObject.Parse(response.Content);
                var userRankings = new List<UserRankingDTO>();

                foreach (var submission in json["content"])
                {
                    var userRanking = new UserRankingDTO
                    {
                        UserName = submission["answers"]["13"]["prettyFormat"]?.ToString(),
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
                                var teamName = parts[1].Trim();
                                var rank = int.Parse(parts[0].Trim());
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
