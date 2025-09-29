using System.Security.Claims;
using System.Threading.Tasks;
using edpicker_api.Models.Planner.Requests;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace edpicker_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PlannerController : ControllerBase
    {
        private readonly IPlannerRepository _repository;
        private readonly ILogger<PlannerController> _logger;

        public PlannerController(IPlannerRepository repository, ILogger<PlannerController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            try
            {
                _logger.LogInformation("GET /api/planner/classes called by user {UserId}", GetCurrentUserId());
                var classes = await _repository.GetClassesAsync();
                return Ok(classes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve classes");
                return StatusCode(500, new { Message = "Error retrieving classes" });
            }
        }

        [HttpGet("sections/{classId}")]
        public async Task<IActionResult> GetSections(int classId)
        {
            if (classId <= 0)
            {
                return BadRequest(new { Message = "Invalid classId" });
            }

            try
            {
                _logger.LogInformation("GET /api/planner/sections/{ClassId} called by user {UserId}", classId, GetCurrentUserId());
                var sections = await _repository.GetSectionsByClassAsync(classId);
                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve sections for class {ClassId}", classId);
                return StatusCode(500, new { Message = "Error retrieving sections" });
            }
        }

        [HttpGet("subjects/{classId}")]
        public async Task<IActionResult> GetSubjects(int classId)
        {
            if (classId <= 0)
            {
                return BadRequest(new { Message = "Invalid classId" });
            }

            try
            {
                _logger.LogInformation("GET /api/planner/subjects/{ClassId} called by user {UserId}", classId, GetCurrentUserId());
                var subjects = await _repository.GetSubjectsByClassAsync(classId);
                return Ok(subjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve subjects for class {ClassId}", classId);
                return StatusCode(500, new { Message = "Error retrieving subjects" });
            }
        }

        [HttpGet("templates/{classId}/{subjectId}")]
        public async Task<IActionResult> GetTemplates(int classId, int subjectId)
        {
            try
            {
                _logger.LogInformation("GET /api/planner/templates/{ClassId}/{SubjectId} called by user {UserId}", classId, subjectId, GetCurrentUserId());
                var templates = await _repository.GetTemplatesAsync(classId, subjectId);
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve templates for class {ClassId} subject {SubjectId}", classId, subjectId);
                return StatusCode(500, new { Message = "Error retrieving templates" });
            }
        }

        [HttpGet("progress/{instanceId}")]
        public async Task<IActionResult> GetProgress(int instanceId)
        {
            try
            {
                _logger.LogInformation("GET /api/planner/progress/{InstanceId} called by user {UserId}", instanceId, GetCurrentUserId());
                var progress = await _repository.GetProgressAsync(instanceId);
                if (progress == null)
                {
                    return NotFound(new { Message = "Curriculum instance not found" });
                }

                return Ok(progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve progress for instance {InstanceId}", instanceId);
                return StatusCode(500, new { Message = "Error retrieving progress" });
            }
        }

        [HttpGet("curriculum/{instanceId}")]
        public async Task<IActionResult> GetCurriculum(int instanceId)
        {
            try
            {
                _logger.LogInformation("GET /api/planner/curriculum/{InstanceId} called by user {UserId}", instanceId, GetCurrentUserId());
                var curriculum = await _repository.GetCurriculumAsync(instanceId);
                if (curriculum == null)
                {
                    return NotFound(new { Message = "Curriculum instance not found" });
                }

                return Ok(curriculum);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve curriculum for instance {InstanceId}", instanceId);
                return StatusCode(500, new { Message = "Error retrieving curriculum" });
            }
        }

        [HttpPut("topic/{topicId}/complete")]
        public async Task<IActionResult> UpdateTopicCompletion(int topicId, [FromBody] TopicCompletionUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("PUT /api/planner/topic/{TopicId}/complete called by user {UserId}", topicId, GetCurrentUserId());
                var status = await _repository.UpdateTopicCompletionAsync(topicId, request.InstanceId, request.IsCompleted, GetCurrentUserId(), GetRequestIpAddress());
                return Ok(status);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation while updating topic completion for topic {TopicId} instance {InstanceId}", topicId, request.InstanceId);
                return Conflict(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found while updating topic completion for topic {TopicId} instance {InstanceId}", topicId, request.InstanceId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update topic completion for topic {TopicId} instance {InstanceId}", topicId, request.InstanceId);
                return StatusCode(500, new { Message = "Error updating topic completion" });
            }
        }

        [HttpGet("multi-section/{classId}/{subjectId}")]
        public async Task<IActionResult> GetMultiSectionComparison(int classId, int subjectId)
        {
            try
            {
                _logger.LogInformation("GET /api/planner/multi-section/{ClassId}/{SubjectId} called by user {UserId}", classId, subjectId, GetCurrentUserId());
                var comparison = await _repository.GetMultiSectionComparisonAsync(classId, subjectId);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve multi-section comparison for class {ClassId} subject {SubjectId}", classId, subjectId);
                return StatusCode(500, new { Message = "Error retrieving multi-section comparison" });
            }
        }

        [HttpGet("curriculum/{instanceId}/paginated")]
        public async Task<IActionResult> GetPaginatedCurriculum(int instanceId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? searchTerm = null, [FromQuery] string filterStatus = "all")
        {
            try
            {
                _logger.LogInformation("GET /api/planner/curriculum/{InstanceId}/paginated called by user {UserId}", instanceId, GetCurrentUserId());
                var paginated = await _repository.GetPaginatedTopicsAsync(instanceId, pageNumber, pageSize, searchTerm, filterStatus);
                return Ok(paginated);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Curriculum instance not found for pagination request. InstanceId {InstanceId}", instanceId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve paginated curriculum for instance {InstanceId}", instanceId);
                return StatusCode(500, new { Message = "Error retrieving curriculum topics" });
            }
        }

        [HttpPost("template/assign")]
        public async Task<IActionResult> AssignTemplate([FromBody] AssignTemplateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("POST /api/planner/template/assign called by user {UserId}", GetCurrentUserId());
                var instanceId = await _repository.AssignTemplateAsync(request, GetCurrentUserId(), GetRequestIpAddress());
                return CreatedAtAction(nameof(GetCurriculum), new { instanceId }, new { InstanceId = instanceId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation while assigning template. Request: {@Request}", request);
                return Conflict(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found while assigning template. Request: {@Request}", request);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign template. Request: {@Request}", request);
                return StatusCode(500, new { Message = "Error assigning template" });
            }
        }

        [HttpGet("template/{templateId}/preview")]
        public async Task<IActionResult> GetTemplatePreview(int templateId)
        {
            try
            {
                _logger.LogInformation("GET /api/planner/template/{TemplateId}/preview called by user {UserId}", templateId, GetCurrentUserId());
                var preview = await _repository.GetTemplatePreviewAsync(templateId);
                if (preview == null)
                {
                    return NotFound(new { Message = "Template not found" });
                }

                return Ok(preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve template preview for template {TemplateId}", templateId);
                return StatusCode(500, new { Message = "Error retrieving template preview" });
            }
        }

        [HttpPost("template/create-custom")]
        public async Task<IActionResult> CreateCustomTemplate([FromBody] CreateCustomTemplateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("POST /api/planner/template/create-custom called by user {UserId}", GetCurrentUserId());
                var templateId = await _repository.CreateCustomTemplateAsync(request, GetCurrentUserId(), GetRequestIpAddress());
                return CreatedAtAction(nameof(GetTemplatePreview), new { templateId }, new { TemplateId = templateId });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Source template not found while creating custom template. SourceTemplateId {SourceTemplateId}", request.SourceTemplateId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create custom template. Request: {@Request}", request);
                return StatusCode(500, new { Message = "Error creating custom template" });
            }
        }

        [HttpPost("archive/template/{templateId}")]
        public async Task<IActionResult> ArchiveTemplate(int templateId)
        {
            try
            {
                _logger.LogInformation("POST /api/planner/archive/template/{TemplateId} called by user {UserId}", templateId, GetCurrentUserId());
                var result = await _repository.ArchiveTemplateAsync(templateId, GetCurrentUserId(), GetRequestIpAddress());
                return Ok(new { Success = result });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation while archiving template {TemplateId}", templateId);
                return Conflict(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Template {TemplateId} not found for archive", templateId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive template {TemplateId}", templateId);
                return StatusCode(500, new { Message = "Error archiving template" });
            }
        }

        [HttpGet("archive/templates")]
        public async Task<IActionResult> GetArchivedTemplates()
        {
            try
            {
                _logger.LogInformation("GET /api/planner/archive/templates called by user {UserId}", GetCurrentUserId());
                var templates = await _repository.GetArchivedTemplatesAsync();
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve archived templates");
                return StatusCode(500, new { Message = "Error retrieving archived templates" });
            }
        }

        [HttpPost("archive/restore/{templateId}")]
        public async Task<IActionResult> RestoreTemplate(int templateId)
        {
            try
            {
                _logger.LogInformation("POST /api/planner/archive/restore/{TemplateId} called by user {UserId}", templateId, GetCurrentUserId());
                var result = await _repository.RestoreTemplateAsync(templateId, GetCurrentUserId(), GetRequestIpAddress());
                return Ok(new { Success = result });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Template {TemplateId} not found for restore", templateId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore template {TemplateId}", templateId);
                return StatusCode(500, new { Message = "Error restoring template" });
            }
        }

        [HttpPost("academic-year/create")]
        public async Task<IActionResult> CreateAcademicYear([FromBody] AcademicYearCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("POST /api/planner/academic-year/create called by user {UserId}", GetCurrentUserId());
                var academicYearId = await _repository.CreateAcademicYearAsync(request.YearName, request.StartDate, request.EndDate, GetCurrentUserId(), GetRequestIpAddress());
                return CreatedAtAction(nameof(GetActiveAcademicYear), new { }, new { AcademicYearId = academicYearId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating academic year");
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create academic year");
                return StatusCode(500, new { Message = "Error creating academic year" });
            }
        }

        [HttpPost("academic-year/freeze")]
        public async Task<IActionResult> FreezeAcademicYear([FromBody] AcademicYearFreezeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("POST /api/planner/academic-year/freeze called by user {UserId}", GetCurrentUserId());
                var result = await _repository.FreezeAcademicYearAsync(request.AcademicYearId, GetCurrentUserId(), GetRequestIpAddress());
                return Ok(new { Success = result });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Academic year {AcademicYearId} not found for freeze", request.AcademicYearId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to freeze academic year {AcademicYearId}", request.AcademicYearId);
                return StatusCode(500, new { Message = "Error freezing academic year" });
            }
        }

        [HttpGet("academic-year/active")]
        public async Task<IActionResult> GetActiveAcademicYear()
        {
            try
            {
                _logger.LogInformation("GET /api/planner/academic-year/active called by user {UserId}", GetCurrentUserId());
                var year = await _repository.GetActiveAcademicYearAsync();
                if (year == null)
                {
                    return NotFound(new { Message = "No active academic year" });
                }

                return Ok(year);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active academic year");
                return StatusCode(500, new { Message = "Error retrieving academic year" });
            }
        }

        [HttpGet("export/progress/{instanceId}")]
        public async Task<IActionResult> ExportProgress(int instanceId)
        {
            try
            {
                _logger.LogInformation("GET /api/planner/export/progress/{InstanceId} called by user {UserId}", instanceId, GetCurrentUserId());
                var bytes = await _repository.ExportProgressAsync(instanceId);
                var fileName = $"curriculum-progress-{instanceId}.csv";
                return File(bytes, "text/csv", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Curriculum instance {InstanceId} not found for export", instanceId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export progress for instance {InstanceId}", instanceId);
                return StatusCode(500, new { Message = "Error exporting progress" });
            }
        }

        [HttpGet("export/audit/{instanceId}")]
        public async Task<IActionResult> ExportAudit(int instanceId)
        {
            try
            {
                _logger.LogInformation("GET /api/planner/export/audit/{InstanceId} called by user {UserId}", instanceId, GetCurrentUserId());
                var bytes = await _repository.ExportAuditTrailAsync(instanceId);
                var fileName = $"curriculum-audit-{instanceId}.csv";
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export audit trail for instance {InstanceId}", instanceId);
                return StatusCode(500, new { Message = "Error exporting audit trail" });
            }
        }

        [HttpGet("validate/data-integrity")]
        public async Task<IActionResult> ValidateDataIntegrity()
        {
            try
            {
                _logger.LogInformation("GET /api/planner/validate/data-integrity called by user {UserId}", GetCurrentUserId());
                var issues = await _repository.ValidateDataIntegrityAsync();
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate data integrity");
                return StatusCode(500, new { Message = "Error validating data integrity" });
            }
        }

        [HttpPost("validate/fix-orphans")]
        public async Task<IActionResult> FixOrphans()
        {
            try
            {
                _logger.LogInformation("POST /api/planner/validate/fix-orphans called by user {UserId}", GetCurrentUserId());
                var count = await _repository.FixOrphanedRecordsAsync();
                return Ok(new { FixedRecords = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fix orphaned records");
                return StatusCode(500, new { Message = "Error fixing orphaned records" });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }

        private string? GetRequestIpAddress()
        {
            return HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }
    }
}
