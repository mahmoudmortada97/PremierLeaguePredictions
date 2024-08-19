namespace PremierLeaguePredictions.Models
{
    public class ScoringResultsResponse
    {
        public Dictionary<string, int> RealOrder { get; set; }
        public List<ScoringResult> Results { get; set; }
        public List<Standings> Standings { get; set; } // New property for standings

    }
}
