using edpicker_api.Models.Dto;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace edpicker_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchoolAccountController : ControllerBase
    {
        private readonly ISchoolAccountRepository _repo;
        private readonly IJwtTokenService _jwtTokenService;
        public SchoolAccountController(ISchoolAccountRepository repo, IJwtTokenService jwtTokenService)
        {
            _repo = repo;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SchoolLoginDto dto)
        {
            var school = await _repo.LoginAsync(dto.SchoolEmail, dto.SchoolPassword);
            if (school == null)
                return Unauthorized("Invalid email or password.");

            var token = _jwtTokenService.GenerateToken(school);

            return Ok(new
            {
                token,
                schoolId = school.SchoolId,
                schoolName = school.SchoolName,
            });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] SchoolAccountDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var (success, schoolId, schoolAccountId, error) = await _repo.CreateAsync(dto);
                if (!success)
                    return BadRequest(error ?? "Could not create account.");

                return Ok(new
                {
                    Message = "Account created successfully.",
                    SchoolId = schoolId,
                    SchoolAccountId = schoolAccountId
                });
              
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [Authorize]
        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("Passwords do not match.");
            if (!_repo.IsPasswordValid(dto.NewPassword))
                return BadRequest("Password must be alphanumeric and at least 8 characters.");
            var result = await _repo.UpdatePasswordAsync(dto.SchoolEmail, dto.OldPassword, dto.NewPassword);
            if (!result)
                return BadRequest("Old password is incorrect.");
            return Ok("Password updated successfully.");
        }
        [Authorize]
        [HttpPost("update-details")]
        public async Task<IActionResult> UpdateDetails([FromBody] UpdateDetailsDto dto)
        {
            var result = await _repo.UpdateDetailsAsync(dto);
            if (!result)
                return BadRequest("Update failed.");
            return Ok("School details updated.");
        }
        [Authorize]
        [HttpPost("add-job")]
        public async Task<IActionResult> AddJob([FromBody] CreateJobRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Get createdBy from JWT or fallback
                string createdBy = User.Identity?.Name ?? "unknown";

                // Extract schoolId from JWT claims and convert to int
                var schoolIdClaim = User.Claims.FirstOrDefault(c => c.Type == "schoolId");
                if (schoolIdClaim == null || !int.TryParse(schoolIdClaim.Value, out int schoolId))
                    return Unauthorized("Invalid or missing schoolId in token.");

                // Optionally, set SchoolId in the request if needed
                request.SchoolId = schoolId;

                var jobId = await _repo.AddJobAsync(request, createdBy);
                return Ok(new { success = true, jobId });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", details = e.Message });
            }
        }
        [HttpGet("job-list")]
        public async Task<IActionResult> GetJobList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)  
        {
            // JWT extraction (assume schoolId claim exists)
            var schoolIdClaim = User.Claims.FirstOrDefault(c => c.Type == "schoolId");
            if (schoolIdClaim == null || !int.TryParse(schoolIdClaim.Value, out int schoolId))
                return Unauthorized("Invalid or missing schoolId in token.");

            if (schoolId <= 0)
                return BadRequest("Invalid SchoolId.");

            try
            {
                var jobs = await _repo.GetJobsBySchoolIdAsync(schoolId, pageNumber, pageSize);
                if (jobs == null || jobs.Count == 0)
                    return NotFound("No jobs found for this school.");

                return Ok(jobs);
            }
            catch (SqlException ex) when (ex.Number == 51030) // SchoolId not found
            {
                return BadRequest("Invalid SchoolId.");
            }
            catch (Exception ex)
            {
                // Optionally log ex
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("job-applications")]
        public async Task<IActionResult> GetJobApplicationsBySchoolAndJob(
        [FromQuery] int jobId)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Get createdBy from JWT or fallback
                string createdBy = User.Identity?.Name ?? "unknown";

                // Extract schoolId from JWT claims and convert to int
                var schoolIdClaim = User.Claims.FirstOrDefault(c => c.Type == "schoolId");
                if (schoolIdClaim == null || !int.TryParse(schoolIdClaim.Value, out int schoolId))
                    return Unauthorized("Invalid or missing schoolId in token.");

                if (schoolId <= 0 || jobId <= 0)
                    return BadRequest("Invalid SchoolId or JobId.");

                var result = await _repo.GetJobApplicationsBySchoolAndJobAsync(schoolId, jobId);

                // Edge cases
                if (result.Applications.Count == 0)
                    return NotFound("No applications found for this job/school.");
                if (result.StatusCounts.Count == 0)
                    return Ok(new { Applications = result.Applications, StatusCounts = new List<School_ApplicationStatusCountDto>() });

                return Ok(result);
            }
            catch (SqlException ex) when (ex.Number == 51010 || ex.Number == 51011)
            {
                // custom error codes from your SP
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Unexpected error
                // Log the error as needed
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPut("update-profile")]
        public async Task<IActionResult> Update([FromBody] School_UpdateSchoolAccountDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string createdBy = User.Identity?.Name ?? "unknown";

            // Extract schoolId from JWT claims and convert to int
            var schoolIdClaim = User.Claims.FirstOrDefault(c => c.Type == "schoolId");
            if (schoolIdClaim == null || !int.TryParse(schoolIdClaim.Value, out int schoolId))
                return Unauthorized("Invalid or missing schoolId in token.");

            if (schoolId <= 0 )
                return BadRequest("Invalid SchoolId");

            try
            {
                var updated = await _repo.UpdateSchoolAccountAsync(schoolId, dto);
                if (!updated)
                    return NotFound("No account found for the given SchoolId.");
                return Ok(new { success = true, message = "School account updated successfully" });
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 53000) // Custom thrown error code from SQL
            {
                return NotFound(sqlEx.Message);
            }
            catch (Exception ex)
            {
                // Optionally: log ex.ToString()
                return StatusCode(500, "An error occurred while updating the school account.");
            }
        }
        [HttpGet("get-profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var schoolIdClaim = User.Claims.FirstOrDefault(c => c.Type == "schoolId");
                if (schoolIdClaim == null || !int.TryParse(schoolIdClaim.Value, out int schoolId))
                    return Unauthorized("Invalid or missing schoolId in token.");

                var result = await _repo.GetProfileBySchoolIdAsync(schoolId);

                if (result == null)
                    return NotFound("School profile not found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] School_ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // extract schoolId from JWT claim
            var claim = User.Claims.FirstOrDefault(c => c.Type == "schoolId");
            if (claim == null || !int.TryParse(claim.Value, out int schoolId))
                return Unauthorized("Invalid or missing schoolId in token.");

            try
            {
                var res = await _repo.ChangePasswordAsync(schoolId, dto);

                return res.ResultCode switch
                {
                    1 => Ok(res.Message),
                    -1 => BadRequest(res.Message),    // old password mismatch
                    -2 => NotFound(res.Message),     // account not found
                    _ => StatusCode(500, res.Message)
                };
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
