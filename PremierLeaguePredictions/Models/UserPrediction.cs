namespace PremierLeaguePredictions.Models
{
    public class UserPrediction
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string[] Rankings { get; set; } // Changed to array of strings
    }
}
