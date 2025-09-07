using System;
using System.Collections.Generic;

namespace edpicker_api.Models.Dto.SyllabusTracker
{
    public class SchoolCreateDto
    {
        public string Name { get; set; }
    }

    public class SchoolSettingsDto
    {
        public DateTime StartDate { get; set; }
        public int TotalWorkingDays { get; set; }
    }

    public class TeacherDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class ClassDto
    {
        public string Name { get; set; }
    }

    public class SubjectDto
    {
        public string Name { get; set; }
        public int? TeacherUserId { get; set; }
    }

    public class ChapterDto
    {
        public string Name { get; set; }
    }

    public class TopicDto
    {
        public string Name { get; set; }
    }

    public class ProgressUpdateDto
    {
        public int Percentage { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class ProgressDto
    {
        public int Id { get; set; }
        public int Percentage { get; set; }
    }

    public class DashboardFilterDto
    {
        public int? ClassId { get; set; }
        public int? TeacherUserId { get; set; }
        public int? SubjectId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class DashboardDto
    {
        public List<ProgressDto> Items { get; set; } = new();
    }

    public class LogsFilterDto
    {
        public int? TeacherUserId { get; set; }
        public int? ClassId { get; set; }
        public int? SubjectId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class AuditLogDto
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }
}

