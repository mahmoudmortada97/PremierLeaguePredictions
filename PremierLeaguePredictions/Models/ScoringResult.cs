namespace PremierLeaguePredictions.Models
{
    public class ScoringResult
    {
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public int Score { get; set; }
        public Dictionary<string, int> UserPredictedOrder { get; set; }
    }
}
