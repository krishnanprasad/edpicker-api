using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using edpicker_api.Services.Interface;
using edpicker_api.Models.Dto.SyllabusTracker;

namespace edpicker_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyllabusTrackerController : ControllerBase
    {
        private readonly ISyllabusTrackerRepository _repository;

        public SyllabusTrackerController(ISyllabusTrackerRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("syllabus")]
        public async Task<IActionResult> CreateSchool([FromBody] SchoolCreateDto request)
        {
            var id = await _repository.CreateSchoolAsync(request);
            return Ok(new { SchoolId = id });
        }

        [HttpPut("syllabus/{schoolId}/settings")]
        public async Task<IActionResult> SetSettings(int schoolId, [FromBody] SchoolSettingsDto request)
        {
            await _repository.SetSchoolSettingsAsync(schoolId, request);
            return NoContent();
        }

        [HttpPost("syllabus/{schoolId}/teachers")]
        public async Task<IActionResult> CreateTeacher(int schoolId, [FromBody] TeacherDto request)
        {
            var teacher = await _repository.CreateTeacherAsync(schoolId, request);
            return Ok(teacher);
        }

        [HttpDelete("syllabus/{schoolId}/teachers/{teacherId}")]
        public async Task<IActionResult> RemoveTeacher(int schoolId, int teacherId)
        {
            await _repository.RemoveTeacherAsync(schoolId, teacherId);
            return NoContent();
        }

        [HttpPost("syllabus/{schoolId}/AddClasses")]
        public async Task<IActionResult> AddClass(int schoolId, [FromBody] ClassDto request)
        {
            var cls = await _repository.AddClassAsync(schoolId, request);
            return Ok(cls);
        }

        [HttpPost("syllabus/{schoolId}/classes/{classId}/subjects")]
        public async Task<IActionResult> AddSubject(int schoolId, int classId, [FromBody] SubjectDto request)
        {
            var subject = await _repository.AddSubjectAsync(schoolId, classId, request);
            return Ok(subject);
        }

        [HttpPost("syllabus/{schoolId}/subjects/{subjectId}/chapters")]
        public async Task<IActionResult> AddChapter(int schoolId, int subjectId, [FromBody] ChapterDto request)
        {
            var chapter = await _repository.AddChapterAsync(schoolId, subjectId, request);
            return Ok(chapter);
        }

        [HttpPost("syllabus/{schoolId}/chapters/{chapterId}/topics")]
        public async Task<IActionResult> AddTopic(int schoolId, int chapterId, [FromBody] TopicDto request)
        {
            var topic = await _repository.AddTopicAsync(schoolId, chapterId, request);
            return Ok(topic);
        }

        [HttpPost("syllabus/{schoolId}/topics/{topicId}/progress")]
        public async Task<IActionResult> UpdateTopicProgress(int schoolId, int topicId, [FromBody] ProgressUpdateDto request)
        {
            await _repository.UpdateTopicProgressAsync(schoolId, topicId, request);
            return NoContent();
        }

        [HttpGet("syllabus/{schoolId}/classes/{classId}/progress")]
        public async Task<IActionResult> GetClassProgress(int schoolId, int classId)
        {
            var progress = await _repository.GetClassProgressAsync(schoolId, classId);
            return Ok(progress);
        }

        [HttpGet("syllabus/{schoolId}/dashboard")]
        public async Task<IActionResult> GetDashboard(int schoolId, [FromQuery] DashboardFilterDto filter)
        {
            var dashboard = await _repository.GetDashboardAsync(schoolId, filter);
            return Ok(dashboard);
        }

        [HttpGet("syllabus/{schoolId}/logs")]
        public async Task<IActionResult> GetLogs(int schoolId, [FromQuery] LogsFilterDto filter)
        {
            var logs = await _repository.GetLogsAsync(schoolId, filter);
            return Ok(logs);
        }
    }
}
