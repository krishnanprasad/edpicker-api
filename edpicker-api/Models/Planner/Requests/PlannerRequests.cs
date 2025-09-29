using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Planner.Requests
{
    public class TopicCompletionUpdateRequest
    {
        [Required]
        public int InstanceId { get; set; }

        [Required]
        public bool IsCompleted { get; set; }
    }

    public class AssignTemplateRequest
    {
        [Required]
        public int ClassId { get; set; }

        [Required]
        public int SectionId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        public int TemplateId { get; set; }

        [Required]
        public int AcademicYearId { get; set; }
    }

    public class CreateCustomTemplateRequest
    {
        [Required]
        public int SourceTemplateId { get; set; }

        [Required]
        [MaxLength(200)]
        public string NewTemplateName { get; set; } = string.Empty;

        public TemplateCustomizationRequest Customizations { get; set; } = new();
    }

    public class TemplateCustomizationRequest
    {
        public List<UnitCustomization> Units { get; set; } = new();
    }

    public class UnitCustomization
    {
        public string UnitName { get; set; } = string.Empty;
        public int UnitOrder { get; set; }
        public List<ChapterCustomization> Chapters { get; set; } = new();
    }

    public class ChapterCustomization
    {
        public string ChapterName { get; set; } = string.Empty;
        public int ChapterOrder { get; set; }
        public List<TopicCustomization> Topics { get; set; } = new();
    }

    public class TopicCustomization
    {
        public string TopicName { get; set; } = string.Empty;
        public int TopicOrder { get; set; }
    }

    public class AcademicYearCreateRequest
    {
        [Required]
        [MaxLength(50)]
        public string YearName { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }

    public class AcademicYearFreezeRequest
    {
        [Required]
        public int AcademicYearId { get; set; }
    }
}
