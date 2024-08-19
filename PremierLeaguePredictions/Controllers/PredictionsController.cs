using Microsoft.AspNetCore.Mvc;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictionsController"/> class.
        /// </summary>
        /// <param name="jotFormService">The service to fetch user rankings from JotForm.</param>
        /// <param name="scoringService">The service to calculate scores based on rankings.</param>
        public PredictionsController(JotFormService jotFormService, ScoringService scoringService, IConfiguration configuration)
        {
            _jotFormService = jotFormService;
            _scoringService = scoringService;
            _configuration = configuration;
        }

        /// <summary>
        /// Gets all user predictions from JotForm.
        /// </summary>
        /// <returns>A list of user predictions.</returns>
        [HttpGet]
        public async Task<IActionResult> GetPredictions()
        {
            var responses = await _jotFormService.FetchUserRankingsAsync();
            return Ok(responses);
        }

        /// <summary>
        /// Calculate the scores for each user based on the real team order.
        /// </summary>
        /// <remarks>
        /// ## Overview
        /// This endpoint calculates the scores for each user's predicted team rankings against the real team rankings.
        ///
        /// The scoring criteria are:
        /// - 10 points for an exact match of the ranking.
        /// - 5 points for a one-step deviation (e.g., if a team is ranked 2nd in the real order and 3rd in the prediction).
        /// - 1 point for a two-step deviation.
        /// - 0 points for more than two steps deviation.
        ///
        /// ### Example of Real Team Order
        /// ```json
        /// {
        ///     "Chelsea 🔵": 1,
        ///     "Crystal Palace 🦅🔵🔴": 2,
        ///     "Brighton & Hove Albion 🐦": 3,
        ///     "Aston Villa 🟣🔵": 4,
        ///     "Bournemouth 🍒": 5,
        ///     "Brentford 🟠⚪": 6,
        ///     "Everton 🔵": 7,
        ///     "Fulham ⚪⚫": 8,
        ///     "Ipswich Town 🔵": 9,
        ///     "Arsenal 🔴⚪": 10,
        ///     "Leicester City 🦊": 11,
        ///     "Liverpool 🔴": 12,
        ///     "Manchester City 🔵": 13,
        ///     "Manchester United 🔴": 14,
        ///     "Newcastle United ⚫⚪": 15,
        ///     "Nottingham Forest 🌳": 16,
        ///     "Southampton ⚓": 17,
        ///     "Tottenham Hotspur ⚪🔵": 18,
        ///     "West Ham United 🔴⚒": 19,
        ///     "Wolverhampton Wanderers 🐺": 20
        /// }
        /// ```
        /// 
        /// ### Example Request
        /// ```json
        /// {
        ///     "Arsenal 🔴⚪": 1,
        ///     "Aston Villa 🟣🔵": 2,
        ///     "Bournemouth 🍒": 3,
        ///     "Brentford 🟠⚪": 4,
        ///     "Brighton & Hove Albion 🐦": 5,
        ///     "Chelsea 🔵": 6,
        ///     "Crystal Palace 🦅🔵🔴": 7,
        ///     "Everton 🔵": 8,
        ///     "Fulham ⚪⚫": 9,
        ///     "Ipswich Town 🔵": 10,
        ///     "Leicester City 🦊": 11,
        ///     "Liverpool 🔴": 12,
        ///     "Manchester City 🔵": 13,
        ///     "West Ham United 🔴⚒": 14,
        ///     "Manchester United 🔴": 15,
        ///     "Newcastle United ⚫⚪": 16,
        ///     "Nottingham Forest 🌳": 17,
        ///     "Southampton ⚓": 18,
        ///     "Tottenham Hotspur ⚪🔵": 19,
        ///     "Wolverhampton Wanderers 🐺": 20
        /// }
        /// ```
        /// 
        /// ### Response Types
        /// - **200 OK**: Returns a list of scoring results for each user, including user name, email, score, their predicted order, and the real order.
        /// - **400 Bad Request**: If the real order is not provided or is empty.
        /// - **500 Internal Server Error**: For any unexpected errors that occur during processing.
        /// </remarks>
        /// <param name="realOrder">A dictionary representing the real order of teams with team names as keys and ranks as values.</param>
        /// <returns>A list of scoring results including user name, email, score, their predicted order, and the real order.</returns>
        /// <response code="200">Returns the scoring results for each user.</response>
        /// <response code="400">Validation errors if the provided real order is invalid.</response>
        /// <response code="500">Internal server error if an unexpected issue occurs.</response>
        [HttpPost]
        public async Task<IActionResult> CalculateScores([FromBody] Dictionary<string, int> realOrder)
        {
            if (realOrder == null || realOrder.Count == 0)
            {
                return BadRequest("Real order must be provided and cannot be empty.");
            }

            // Fetch the URL for user rankings service
            var userRankingsUrl = $"https://api.jotform.com/form/{_configuration.GetSection("JotForm:FormId").Value}/submissions?apiKey={_configuration.GetSection("JotForm:ApiKey").Value}";

            // Instantiate the ScoringService with URLs for real order and user rankings
            var scoringService = new ScoringService("https://api.example.com/realOrder", userRankingsUrl);

            try
            {
                var results = await scoringService.CalculateScoresAsync(realOrder);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
