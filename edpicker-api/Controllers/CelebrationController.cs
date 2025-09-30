using System.Security.Claims;
using edpicker_api.Models.Celebration.Dtos;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace edpicker_api.Controllers;

[Authorize]
[ApiController]
[Route("api/celebration")]
public class CelebrationController : ControllerBase
{
    private readonly ICelebrationService _celebrationService;
    private readonly ILogger<CelebrationController> _logger;

    public CelebrationController(ICelebrationService celebrationService, ILogger<CelebrationController> logger)
    {
        _celebrationService = celebrationService;
        _logger = logger;
    }

    [HttpPost("profiles/upload")]
    public async Task<IActionResult> UploadProfiles([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            var adminId = GetCurrentAdminId();
            var result = await _celebrationService.UploadProfilesAsync(adminId, file, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload celebration profiles");
            return MapError(ex, "Failed to upload profiles");
        }
    }

    [HttpPost("profiles/students")]
    public async Task<IActionResult> UpsertStudent([FromBody] StudentUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var adminId = GetCurrentAdminId();
            var result = await _celebrationService.UpsertStudentAsync(adminId, request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert student");
            return MapError(ex, "Failed to save student");
        }
    }

    [HttpPost("profiles/teachers")]
    public async Task<IActionResult> UpsertTeacher([FromBody] TeacherUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var adminId = GetCurrentAdminId();
            var result = await _celebrationService.UpsertTeacherAsync(adminId, request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert teacher");
            return MapError(ex, "Failed to save teacher");
        }
    }

    [HttpGet("dashboard/today")]
    public async Task<IActionResult> GetTodayDashboard(CancellationToken cancellationToken)
    {
        try
        {
            var adminId = GetCurrentAdminId();
            var result = await _celebrationService.GetTodayDashboardAsync(adminId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve celebration dashboard");
            return MapError(ex, "Failed to load dashboard");
        }
    }

    [HttpGet("dashboard/history")]
    public async Task<IActionResult> GetHistory([FromQuery] string date, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            return BadRequest(new
            {
                success = false,
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "Invalid date format. Use YYYY-MM-DD"
                }
            });
        }

        try
        {
            var adminId = GetCurrentAdminId();
            var result = await _celebrationService.GetHistoryAsync(adminId, parsedDate, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve celebration history for {Date}", date);
            return MapError(ex, "Failed to load history");
        }
    }

    [HttpPost("config/school")]
    public async Task<IActionResult> SetSchoolName([FromBody] SchoolConfigRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var adminId = GetCurrentAdminId();
            var result = await _celebrationService.SetSchoolNameAsync(adminId, request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set school name");
            return MapError(ex, "Failed to update school name");
        }
    }

    [HttpPost("sms/test")]
    public async Task<IActionResult> SendTestSms([FromBody] SmsTestRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var adminId = GetCurrentAdminId();
            var result = await _celebrationService.SendTestSmsAsync(adminId, request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test SMS");
            return MapError(ex, "Failed to send SMS");
        }
    }

    [AllowAnonymous]
    [HttpPost("webhook/optout")]
    public async Task<IActionResult> HandleOptOut([FromBody] OptOutRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var response = await _celebrationService.HandleOptOutAsync(request, cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process opt-out webhook");
            return MapError(ex, "Failed to process opt-out");
        }
    }

    private int GetCurrentAdminId()
    {
        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userId, out var adminId))
        {
            return adminId;
        }

        throw new UnauthorizedAccessException("Unable to determine admin id");
    }

    private ObjectResult MapError(Exception exception, string message)
    {
        var statusCode = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(statusCode, new
        {
            success = false,
            error = new
            {
                code = statusCode == StatusCodes.Status400BadRequest ? "VALIDATION_ERROR" : "SERVER_ERROR",
                message,
                details = new[] { exception.Message }
            }
        });
    }
}
