//using Microsoft.AspNetCore.Mvc;
//using PremierLeaguePredictions.Models;
//using PremierLeaguePredictions.Services;

//namespace PremierLeaguePredictions.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class EmailController : ControllerBase
//    {
//        private readonly EmailService _emailService;

//        public EmailController(EmailService emailService)
//        {
//            _emailService = emailService;
//        }

//        [HttpPost]
//        [Route("Send")]
//        public async Task<IActionResult> SendEmailToAllUsers([FromBody] EmailDTO emails)
//        {
//            if (emails == null)
//            {
//                return BadRequest("EmailDTO cannot be null.");
//            }

//            try
//            {
//                var leaderboard = emails.UserRankings;

//                await _emailService.BuildLeaderboardAsync(leaderboard);
//                await _emailService.BuildFinalOrderAsync(emails.FinalOrder);
//                await _emailService.SendEmailsAsync(emails);

//                return Ok();
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, $"Internal server error: {ex.Message}");
//            }
//        }


//    }
//}
