using edpicker_api.Models;
using edpicker_api.Models.Dto;
using edpicker_api.Models.Dto.SyllabusTracker;
using edpicker_api.Models.Job;
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
        modelBuilder.Entity<ProgressDto>().HasNoKey().ToView(null);
    }
}
