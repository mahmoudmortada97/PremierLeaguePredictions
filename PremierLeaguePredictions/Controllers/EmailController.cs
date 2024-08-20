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
                var leaderboard = emails.PersonEmails;
                _emailService.BuildLeaderboard(leaderboard);
                _emailService.BuildFinalOrder(emails.FinalOrder);

                await _emailService.SendEmails(emails);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }
}
