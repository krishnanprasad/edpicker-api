using edpicker_api.Models;
using edpicker_api.Models.Celebration.Entities;
using edpicker_api.Models.Dto;
using edpicker_api.Models.Job;
using edpicker_api.Models.Planner.Entities;
using edpicker_api.Models.Results;
using Microsoft.EntityFrameworkCore;

public class EdPickerDbContext : DbContext
{
    public DbSet<JobBoard> JobBoard { get; set; }

    // 1) Rename this so it doesn't shadow the real JobDetails table
    public DbSet<JobBoardDetailsDto> JobBoardDetails { get; set; }

    public DbSet<SchoolAccounts> SchoolAccounts { get; set; }
    public DbSet<User> User { get; set; }
    public DbSet<CityDto> CitiesDto { get; set; }
    public DbSet<SearchJobDto> SearchJobResults { get; set; }
    public DbSet<ApplyForJobResult> ApplyForJobResults { get; set; }
    public DbSet<School_JobApplicationDto> JobApplicationsDto { get; set; }
    public DbSet<School_ApplicationStatusCountDto> ApplicationStatusCounts { get; set; }
    public DbSet<BoardDto> Boards { get; set; }
    public DbSet<SchoolClassDto> SchoolClasses { get; set; }

    public DbSet<AcademicYear> AcademicYears { get; set; }
    public DbSet<CurriculumClass> CurriculumClasses { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<CurriculumInstance> CurriculumInstances { get; set; }
    public DbSet<TopicCompletion> TopicCompletions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    public DbSet<CelebrationStudent> CelebrationStudents { get; set; }
    public DbSet<CelebrationTeacher> CelebrationTeachers { get; set; }
    public DbSet<CelebrationLog> CelebrationLogs { get; set; }
    public DbSet<CelebrationAuditLog> CelebrationAuditLogs { get; set; }
    public DbSet<CelebrationConfiguration> CelebrationConfigurations { get; set; }

    public EdPickerDbContext(DbContextOptions<EdPickerDbContext> options)
      : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Your existing real‐entity config:
        modelBuilder.Entity<JobBoard>()
            .HasOne(j => j.ContactDetails)
            .WithMany(c => c.JobBoards);

        // 2) Configure your DTO as keyless and not mapped to any table/view:
        modelBuilder.Entity<JobBoardDetailsDto>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null);
        });

        // Remove this line — it's wrongly clobbering your real JobDetails entity
        // modelBuilder.Entity<JobDetails>().HasNoKey();

        modelBuilder.Entity<SearchJobDto>()
            .HasNoKey()
            .ToView(null);

        modelBuilder.Entity<CityDto>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null);
        });
        modelBuilder.Entity<ApplyForJobResult>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null);
        });
        modelBuilder.Entity<UserJobApplicationDto>().HasNoKey();
        modelBuilder.Entity<School_JobApplicationDto>().HasNoKey().ToView(null);
        modelBuilder.Entity<School_ApplicationStatusCountDto>().HasNoKey().ToView(null);
        modelBuilder.Entity<School_JobListDto>().HasNoKey().ToView(null);
        modelBuilder.Entity<BoardDto>().HasNoKey().ToView(null);
        modelBuilder.Entity<School_GetProfileDto>().HasNoKey().ToView(null);
        modelBuilder.Entity<School_ChangePasswordResultDto>().HasNoKey().ToView(null);
        modelBuilder.Entity<SchoolClassDto>().HasNoKey().ToView(null);

        ConfigurePlannerModel(modelBuilder);
        ConfigureCelebrationModel(modelBuilder);
    }

    private static void ConfigurePlannerModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CurriculumClass>()
            .HasMany(c => c.Sections)
            .WithOne(s => s.Class)
            .HasForeignKey(s => s.ClassId);

        modelBuilder.Entity<CurriculumClass>()
            .HasMany(c => c.Subjects)
            .WithOne(s => s.Class)
            .HasForeignKey(s => s.ClassId);

        modelBuilder.Entity<Template>()
            .HasIndex(t => new { t.ClassId, t.SubjectId, t.IsArchived })
            .HasDatabaseName("IX_Template_Lookup");

        modelBuilder.Entity<Template>()
            .HasOne(t => t.SourceTemplate)
            .WithMany(t => t.DerivedTemplates)
            .HasForeignKey(t => t.SourceTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Template>()
            .HasMany(t => t.Units)
            .WithOne(u => u.Template)
            .HasForeignKey(u => u.TemplateId);

        modelBuilder.Entity<Unit>()
            .HasIndex(u => new { u.TemplateId, u.IsArchived })
            .HasDatabaseName("IX_Unit_Template");

        modelBuilder.Entity<Unit>()
            .HasMany(u => u.Chapters)
            .WithOne(c => c.Unit)
            .HasForeignKey(c => c.UnitId);

        modelBuilder.Entity<Chapter>()
            .HasIndex(c => new { c.UnitId, c.IsArchived })
            .HasDatabaseName("IX_Chapter_Unit");

        modelBuilder.Entity<Chapter>()
            .HasMany(c => c.Topics)
            .WithOne(t => t.Chapter)
            .HasForeignKey(t => t.ChapterId);

        modelBuilder.Entity<Topic>()
            .HasIndex(t => new { t.ChapterId, t.IsArchived })
            .HasDatabaseName("IX_Topic_Chapter");

        modelBuilder.Entity<CurriculumInstance>()
            .HasIndex(ci => new { ci.AcademicYearId, ci.ClassId, ci.SectionId, ci.SubjectId })
            .HasDatabaseName("IX_CurriculumInstance_Lookup")
            .IsUnique();

        modelBuilder.Entity<CurriculumInstance>()
            .HasMany(ci => ci.TopicCompletions)
            .WithOne(tc => tc.Instance)
            .HasForeignKey(tc => tc.InstanceId);

        modelBuilder.Entity<TopicCompletion>()
            .HasIndex(tc => new { tc.InstanceId, tc.IsCompleted })
            .HasDatabaseName("IX_TopicCompletion_Instance");

        modelBuilder.Entity<TopicCompletion>()
            .HasIndex(tc => new { tc.InstanceId, tc.TopicId })
            .IsUnique();

        modelBuilder.Entity<AcademicYear>()
            .HasMany(y => y.CurriculumInstances)
            .WithOne(ci => ci.AcademicYear)
            .HasForeignKey(ci => ci.AcademicYearId);
    }

    private static void ConfigureCelebrationModel(ModelBuilder modelBuilder)
    {
        const string schema = "celebration";

        modelBuilder.Entity<CelebrationStudent>(entity =>
        {
            entity.ToTable("Students", schema);
            entity.HasIndex(e => new { e.AdminId, e.DateOfBirth }).HasDatabaseName("IX_CelebrationStudents_Admin_Dob");
            entity.HasIndex(e => new { e.AdminId, e.Phone }).HasDatabaseName("IX_CelebrationStudents_Admin_Phone");
        });

        modelBuilder.Entity<CelebrationTeacher>(entity =>
        {
            entity.ToTable("Teachers", schema);
            entity.HasIndex(e => new { e.AdminId, e.DateOfBirth }).HasDatabaseName("IX_CelebrationTeachers_Admin_Dob");
            entity.HasIndex(e => new { e.AdminId, e.Anniversary }).HasDatabaseName("IX_CelebrationTeachers_Admin_Anniv");
            entity.HasIndex(e => new { e.AdminId, e.Phone }).HasDatabaseName("IX_CelebrationTeachers_Admin_Phone");
        });

        modelBuilder.Entity<CelebrationLog>(entity =>
        {
            entity.ToTable("Logs", schema);
            entity.HasIndex(e => new { e.AdminId, e.EventDate }).HasDatabaseName("IX_CelebrationLogs_Admin_Date");
            entity.HasIndex(e => new { e.AdminId, e.RecipientType, e.RecipientId, e.EventDate }).HasDatabaseName("IX_CelebrationLogs_Recipient_Date");
        });

        modelBuilder.Entity<CelebrationAuditLog>(entity =>
        {
            entity.ToTable("AuditTrail", schema);
            entity.HasIndex(e => new { e.AdminId, e.Timestamp }).HasDatabaseName("IX_CelebrationAudit_Admin_Timestamp");
        });

        modelBuilder.Entity<CelebrationConfiguration>(entity =>
        {
            entity.ToTable("Configuration", schema);
        });
    }
}
