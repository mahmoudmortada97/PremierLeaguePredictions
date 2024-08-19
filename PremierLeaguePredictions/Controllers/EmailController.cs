using Microsoft.AspNetCore.Mvc;
using PremierLeaguePredictions.Models;
using PremierLeaguePredictions.Services;

namespace PremierLeaguePredictions.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly EmailService _emailService;

        public EmailController(EmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost]
        [Route("Send")]
        public async Task<IActionResult> SendEmailToAllUsers([FromBody] EmailDTO emails)
        {
            if (emails == null)
            {
                return BadRequest("EmailDTO cannot be null.");
            }

            try
            {
                // Ensure the leaderboard HTML is built before sending emails
                // You should have some logic to fetch the leaderboard data
                var leaderboard = emails.UserScores; // Implement this method to get actual leaderboard data
                _emailService.BuildLeaderboard(leaderboard);

                // Send emails
                var result = await Task.Run(() => _emailService.SendEmails(emails));

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }
}
