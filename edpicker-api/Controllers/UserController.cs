using System.Text.Json;
using edpicker_api.Models;
using edpicker_api.Models.Dto;
using edpicker_api.Models.Methods;
using edpicker_api.Models.Results;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace edpicker_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJwtTokenService _jwtTokenService;

        public UserController(IUserRepository userRepository, IHttpClientFactory httpClientFactory, IJwtTokenService jwtTokenService)
        {
            _userRepository = userRepository;
            _httpClientFactory = httpClientFactory;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleTokenRequest tokenRequest)
        {
            if (string.IsNullOrEmpty(tokenRequest.IdToken))
                return BadRequest("Token is required");

            // Validate and decode the token using Google API
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={tokenRequest.IdToken}");

            if (!response.IsSuccessStatusCode)
                return Unauthorized("Invalid Google token");

            var payload = JsonSerializer.Deserialize<GoogleTokenPayload>(await response.Content.ReadAsStringAsync());

            if (payload == null || string.IsNullOrEmpty(payload.Sub))
                return Unauthorized("Invalid token payload");

            // Check if user exists or create a new one
            var user = new User
            {
                GoogleId = payload.Sub,
                FullName = payload.Name,
                Email = payload.Email,
                EmailVerified = payload.EmailVerified == "true",
                Phone = "", // Optional, collect later
                IsActive = true,
            };
            
            var User = await _userRepository.CreateOrUpdateUserAsync(user);
            var Jwttoken = _jwtTokenService.GenerateToken(User);
            return Ok(new { success = true, name = user.FullName, token = Jwttoken });
        }
        [HttpGet("applications")]
        [Authorize]
        public async Task<IActionResult> GetUserApplications()
        {
            try {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");

                if (userIdClaim == null)
                    return Unauthorized();

                int userId = int.Parse(userIdClaim.Value);
                string createdBy = User.Identity.Name ?? "unknown";
                var results = await _userRepository.GetUserJobApplicationsAsync(userId);
            return Ok(results);
            }
catch(Exception E)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }
        [Authorize]
        [HttpDelete("withdrawl")]
        public async Task<IActionResult> ApplyForJob(int jobId)
        {
            // Extract UserId from JWT claims (assuming claim type "userId")
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");

            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            string createdBy = User.Identity.Name ?? "unknown";

            // Extract FullName from claims and ensure it is a string
            var fullNameClaim = User.Claims.FirstOrDefault(c => c.Type == "FullName");
            string fullName = fullNameClaim?.Value ?? "unknown";

            ApplyForJobResult _ApplyForJobResult = await _userRepository.WithdrawlOfJobAsync(userId, jobId, "", fullName);

            if (_ApplyForJobResult.ResultCode == 1)
                return Ok(new { success = true, _ApplyForJobResult.Message });
            else
                return BadRequest(new { success = false, _ApplyForJobResult.Message });
        }

    }
}
