namespace PremierLeaguePredictions.Models
{
    public class UserRankingDTO
    {
        public int UserScore { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public Dictionary<string, int> Rankings { get; set; }
    }
}
