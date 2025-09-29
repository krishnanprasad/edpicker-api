using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace edpicker_api.Models.Planner.Entities
{
    [Table("AcademicYear")]
    public class AcademicYear
    {
        [Key]
        public int AcademicYearId { get; set; }

        [Required, MaxLength(50)]
        public string YearName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsFrozen { get; set; }

        public ICollection<CurriculumInstance> CurriculumInstances { get; set; } = new List<CurriculumInstance>();
    }

    [Table("Class")]
    public class CurriculumClass
    {
        [Key]
        public int ClassId { get; set; }

        [Required, MaxLength(100)]
        public string ClassName { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public ICollection<Section> Sections { get; set; } = new List<Section>();

        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

        public ICollection<Template> Templates { get; set; } = new List<Template>();
    }

    [Table("Section")]
    public class Section
    {
        [Key]
        public int SectionId { get; set; }

        [ForeignKey(nameof(Class))]
        public int ClassId { get; set; }

        [Required, MaxLength(25)]
        public string SectionName { get; set; } = string.Empty;

        public CurriculumClass Class { get; set; } = null!;

        public ICollection<CurriculumInstance> CurriculumInstances { get; set; } = new List<CurriculumInstance>();
    }

    [Table("Subject")]
    public class Subject
    {
        [Key]
        public int SubjectId { get; set; }

        [ForeignKey(nameof(Class))]
        public int ClassId { get; set; }

        [Required, MaxLength(150)]
        public string SubjectName { get; set; } = string.Empty;

        public CurriculumClass Class { get; set; } = null!;

        public ICollection<Template> Templates { get; set; } = new List<Template>();

        public ICollection<CurriculumInstance> CurriculumInstances { get; set; } = new List<CurriculumInstance>();
    }

    [Table("Template")]
    public class Template
    {
        [Key]
        public int TemplateId { get; set; }

        [ForeignKey(nameof(Class))]
        public int ClassId { get; set; }

        [ForeignKey(nameof(Subject))]
        public int SubjectId { get; set; }

        public int? SourceTemplateId { get; set; }

        public int? CreatedBy { get; set; }

        [Required, MaxLength(200)]
        public string TemplateName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string TemplateType { get; set; } = "System";

        public bool IsArchived { get; set; }

        public DateTime CreatedDate { get; set; }

        public CurriculumClass Class { get; set; } = null!;

        public Subject Subject { get; set; } = null!;

        public Template? SourceTemplate { get; set; }

        public ICollection<Template> DerivedTemplates { get; set; } = new List<Template>();

        public ICollection<Unit> Units { get; set; } = new List<Unit>();

        public ICollection<CurriculumInstance> CurriculumInstances { get; set; } = new List<CurriculumInstance>();
    }

    [Table("Unit")]
    public class Unit
    {
        [Key]
        public int UnitId { get; set; }

        [ForeignKey(nameof(Template))]
        public int TemplateId { get; set; }

        [Required, MaxLength(200)]
        public string UnitName { get; set; } = string.Empty;

        public int UnitOrder { get; set; }

        public bool IsArchived { get; set; }

        public DateTime CreatedDate { get; set; }

        public Template Template { get; set; } = null!;

        public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    }

    [Table("Chapter")]
    public class Chapter
    {
        [Key]
        public int ChapterId { get; set; }

        [ForeignKey(nameof(Unit))]
        public int UnitId { get; set; }

        [Required, MaxLength(200)]
        public string ChapterName { get; set; } = string.Empty;

        public int ChapterOrder { get; set; }

        public bool IsArchived { get; set; }

        public DateTime CreatedDate { get; set; }

        public Unit Unit { get; set; } = null!;

        public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    }

    [Table("Topic")]
    public class Topic
    {
        [Key]
        public int TopicId { get; set; }

        [ForeignKey(nameof(Chapter))]
        public int ChapterId { get; set; }

        [Required, MaxLength(250)]
        public string TopicName { get; set; } = string.Empty;

        public int TopicOrder { get; set; }

        public bool IsArchived { get; set; }

        public DateTime CreatedDate { get; set; }

        public Chapter Chapter { get; set; } = null!;

        public ICollection<TopicCompletion> TopicCompletions { get; set; } = new List<TopicCompletion>();
    }

    [Table("CurriculumInstance")]
    public class CurriculumInstance
    {
        [Key]
        public int InstanceId { get; set; }

        [ForeignKey(nameof(AcademicYear))]
        public int AcademicYearId { get; set; }

        [ForeignKey(nameof(Class))]
        public int ClassId { get; set; }

        [ForeignKey(nameof(Section))]
        public int SectionId { get; set; }

        [ForeignKey(nameof(Subject))]
        public int SubjectId { get; set; }

        [ForeignKey(nameof(Template))]
        public int TemplateId { get; set; }

        public DateTime AssignedDate { get; set; }

        public AcademicYear AcademicYear { get; set; } = null!;

        public CurriculumClass Class { get; set; } = null!;

        public Section Section { get; set; } = null!;

        public Subject Subject { get; set; } = null!;

        public Template Template { get; set; } = null!;

        public ICollection<TopicCompletion> TopicCompletions { get; set; } = new List<TopicCompletion>();
    }

    [Table("TopicCompletion")]
    public class TopicCompletion
    {
        [Key]
        public int CompletionId { get; set; }

        [ForeignKey(nameof(Instance))]
        public int InstanceId { get; set; }

        [ForeignKey(nameof(Topic))]
        public int TopicId { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedDate { get; set; }

        public DateTime UpdatedDate { get; set; }

        public int? UpdatedBy { get; set; }

        public CurriculumInstance Instance { get; set; } = null!;

        public Topic Topic { get; set; } = null!;
    }

    [Table("AuditLog")]
    public class AuditLog
    {
        [Key]
        public long AuditId { get; set; }

        [Required, MaxLength(128)]
        public string TableName { get; set; } = string.Empty;

        public long RecordId { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime UpdatedDate { get; set; }

        public int? UpdatedBy { get; set; }

        [MaxLength(64)]
        public string? IpAddress { get; set; }
    }
}
