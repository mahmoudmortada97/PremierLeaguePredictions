namespace PremierLeaguePredictions.Models
{
    public class UserRanking
    {
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public Dictionary<string, int> Rankings { get; set; }
    }
}
