using Microsoft.AspNetCore.Mvc;
using PremierLeaguePredictions.Filters;
using PremierLeaguePredictions.Models;
using PremierLeaguePredictions.Services;

namespace PremierLeaguePredictions.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PredictionsController : ControllerBase
    {
        private readonly JotFormService _jotFormService;
        private readonly ScoringService _scoringService;
        private readonly EmailService _emailService;
        private readonly ILogger<PredictionsController> _logger;

        public PredictionsController(
            JotFormService jotFormService,
            ScoringService scoringService,
            EmailService emailService,
            ILogger<PredictionsController> logger)
        {
            _jotFormService = jotFormService;
            _scoringService = scoringService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet("GetPredictions")]
        public async Task<IActionResult> GetPredictions()
        {
            var responses = await _jotFormService.FetchUserRankingsAsync();
            return Ok(responses);
        }

        /// <summary>
        /// Calculates scores for all participants and sends result emails.
        /// Pass dryRun=true to calculate scores without sending emails.
        /// </summary>
        [HttpPost("CalculateScores")]
        [ApiKey]
        public async Task<IActionResult> CalculateScores(
            [FromBody] Dictionary<string, int> realOrder,
            [FromQuery] bool dryRun = false)
        {
            if (realOrder == null || realOrder.Count == 0)
                return BadRequest("Real order must be provided and cannot be empty.");

            try
            {
                var results = await _scoringService.CalculateScoresAsync(realOrder);

                if (results?.Results == null)
                    return StatusCode(500, "Failed to calculate scores.");

                if (!dryRun)
                {
                    var emails = new EmailDTO
                    {
                        FinalOrder = results.RealOrder,
                        UserRankings = results.Results
                    };

                    await _emailService.SendEmailsAsync(emails);
                }

                return Ok(new { DryRun = dryRun, Results = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Score calculation failed.");
                return StatusCode(500, "An error occurred processing your request.");
            }
        }
    }
}