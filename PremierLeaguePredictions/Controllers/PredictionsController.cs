using Microsoft.AspNetCore.Mvc;
using PremierLeaguePredictions.Models;
using PremierLeaguePredictions.Services;

namespace PremierLeaguePredictions.Controllers
{
    /// <summary>
    /// Handles API requests for Premier League predictions and scoring.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PredictionsController : ControllerBase
    {
        private readonly JotFormService _jotFormService;
        private readonly ScoringService _scoringService;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly HttpClient _httpClient;



        public PredictionsController(JotFormService jotFormService, ScoringService scoringService, IConfiguration configuration, EmailService emailService, IHttpClientFactory httpClientFactory)
        {
            _jotFormService = jotFormService;
            _scoringService = scoringService;
            _configuration = configuration;
            _emailService = emailService;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Gets all user predictions from JotForm.
        /// </summary>
        /// <returns>A list of user predictions.</returns>
        [HttpGet("GetPredictions")]
        public async Task<IActionResult> GetPredictions()
        {
            var responses = await _jotFormService.FetchUserRankingsAsync();
            return Ok(responses);
        }


        [HttpPost("CalculateScores")]
        public async Task<IActionResult> CalculateScores([FromBody] Dictionary<string, int> realOrder)
        {
            if (realOrder == null || realOrder.Count == 0)
            {
                return BadRequest("Real order must be provided and cannot be empty.");
            }

            try
            {
                // Fetch the URL for user rankings service
                var userRankingsUrl = $"https://api.jotform.com/form/{_configuration["JotForm:FormId"]}/submissions?apiKey={_configuration["JotForm:ApiKey"]}";

                // Instantiate the ScoringService with URLs for real order and user rankings
                var scoringService = new ScoringService(userRankingsUrl);

                // Calculate scores
                var results = await scoringService.CalculateScoresAsync(realOrder);

                // Fetch user rankings
                var userRankings = await _jotFormService.FetchUserRankingsAsync();

                if (userRankings == null)
                {
                    return StatusCode(500, "Failed to fetch user rankings.");
                }

                var emails = new EmailDTO
                {
                    FinalOrder = results.RealOrder,
                    UserRankings = results.Results
                };

                // Build and send leaderboard email
                await _emailService.BuildLeaderboardAsync(emails.UserRankings);
                await _emailService.BuildFinalOrderAsync(emails.FinalOrder);
                await _emailService.SendEmailsAsync(emails);

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
