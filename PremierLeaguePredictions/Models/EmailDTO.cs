namespace PremierLeaguePredictions.Models
{
    public class EmailDTO
    {
        //public List<UserScore> UserScores { get; set; } = new();
        public List<PersonEmailDTO> PersonEmails { get; set; } = new();
        public Dictionary<string, int> FinalOrder { get; set; }



    }
}
