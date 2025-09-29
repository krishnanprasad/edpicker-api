using System;
using System.Collections.Generic;

namespace edpicker_api.Models.Planner.Dto
{
    public class ClassSummaryDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }

    public class SectionSummaryDto
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
    }

    public class SubjectSummaryDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
    }

    public class TemplateSummaryDto
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CurriculumProgressDto
    {
        public int InstanceId { get; set; }
        public int TotalTopics { get; set; }
        public int CompletedTopics { get; set; }
        public decimal Percentage { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; }
    }

    public class CurriculumStructureDto
    {
        public int InstanceId { get; set; }
        public List<CurriculumUnitDto> Units { get; set; } = new();
    }

    public class CurriculumUnitDto
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public int UnitOrder { get; set; }
        public List<CurriculumChapterDto> Chapters { get; set; } = new();
    }

    public class CurriculumChapterDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public int ChapterOrder { get; set; }
        public List<CurriculumTopicDto> Topics { get; set; } = new();
    }

    public class CurriculumTopicDto
    {
        public int TopicId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public int TopicOrder { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    public class TopicCompletionStatusDto
    {
        public int TopicId { get; set; }
        public int InstanceId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class SectionProgressComparisonDto
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public int? InstanceId { get; set; }
        public int TotalTopics { get; set; }
        public int CompletedTopics { get; set; }
        public decimal Percentage { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class PaginatedTopicsResponseDto
    {
        public List<CurriculumTopicDto> Topics { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class TemplatePreviewDto
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public int UnitCount { get; set; }
        public int ChapterCount { get; set; }
        public int TopicCount { get; set; }
        public List<string> UnitNames { get; set; } = new();
        public List<string> ChapterNames { get; set; } = new();
        public List<string> TopicNames { get; set; } = new();
    }

    public class DataIntegrityIssueDto
    {
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CurriculumInstanceDto
    {
        public int InstanceId { get; set; }
        public int AcademicYearId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public int TemplateId { get; set; }
        public DateTime AssignedDate { get; set; }
        public string TemplateName { get; set; } = string.Empty;
    }

    public class AcademicYearDto
    {
        public int AcademicYearId { get; set; }
        public string YearName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsFrozen { get; set; }
    }
}
