namespace PremierLeaguePredictions.Models
{
    public class EmailDTO
    {
        //public List<UserScore> UserScores { get; set; } = new();
        public List<UserRankingDTO> UserRankings { get; set; } = new();
        public Dictionary<string, int> FinalOrder { get; set; }



    }
}
