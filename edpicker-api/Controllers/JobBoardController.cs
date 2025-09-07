using edpicker_api.Models.Dto;
using edpicker_api.Models.Job;
using edpicker_api.Models.Results;
using edpicker_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace edpicker_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobBoardController : ControllerBase
    {
        private readonly IJobBoardRepository _repo;

        public JobBoardController(IJobBoardRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetJobBoards(
   [FromQuery] int? cityId,
        [FromQuery] int? boardId,
        [FromQuery] bool? isVerified,
        [FromQuery] decimal? minSalary,
        [FromQuery] decimal? maxSalary,
        [FromQuery] int? minExperience,
        [FromQuery] int? maxExperience,
        [FromQuery] decimal? userLatitude = null,
        [FromQuery] decimal? userLongitude = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
        {
            try
            {
                var jobs = await _repo.SearchJobBoardsAsync(
             cityId,
             boardId,
             isVerified,
             minSalary,
             maxSalary,
             minExperience,
             maxExperience,
             userLatitude,
             userLongitude,
             pageNumber,
             pageSize
         );
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetJobBoardDetails(int id)
        {
            try { 
            var job = await _repo.GetJobBoardDetailsByIdAsync(id);
            if (job == null)
                return NotFound();

            return Ok(job);
            }
            catch  (Exception e)
            {
                throw e;
            }
            }

        [Authorize]
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyForJob([FromBody] ApplyForJobRequestDto request)
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

            ApplyForJobResult _ApplyForJobResult = await _repo.ApplyForJobAsync(userId, request.JobId, request.Notes, fullName);

            if (_ApplyForJobResult.ResultCode == 1)
                return Ok(new { success = true, _ApplyForJobResult.Message });
            else if (_ApplyForJobResult.ResultCode == 101)
                return Ok(new { success = false, message = "Already Applied", messageCode = 101 });
            else
                return BadRequest(new { success = false, _ApplyForJobResult.Message });
        }
    }




}
