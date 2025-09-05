// Models/EdPickerQuestionPaperDbContext.cs
using edpicker_api.Models.Dto; // Make sure this namespace exists and contains your DTOs
using edpicker_api.Models.QuestionPaper.Dto;
using Microsoft.EntityFrameworkCore;

namespace edpicker_api
{
    public class EdPickerQuestionPaperDbContext : DbContext
    {
        public EdPickerQuestionPaperDbContext(DbContextOptions<EdPickerQuestionPaperDbContext> options) : base(options)
        {
        }

        // Define your DbSet properties here, for example:
        public DbSet<SchoolClassDto> SchoolClasses { get; set; }
        public DbSet<SchoolDto> Schools { get; set; }
        public DbSet<SchoolSubjectDto> SchoolSubjects { get; set; }
        public DbSet<TopicDto> Topics { get; set; }

        public DbSet<SubjectChapterDto> SubjectChapters { get; set; }
        public DbSet<ChapterKnowledgeDto> ChapterKnowledges { get; set; } // Add this line


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SchoolDto>().HasKey(s => s.SchoolId);
            modelBuilder.Entity<SchoolSubjectDto>().HasNoKey().ToView("vw_SchoolSubjectByClass");
            modelBuilder.Entity<TopicDto>().HasNoKey();
            modelBuilder.Entity<SubjectChapterDto>().HasNoKey().ToView("vw_SubjectChapter");
            modelBuilder.Entity<ChapterKnowledgeDto>().HasNoKey(); // Also configure ChapterKnowledgeDto

            // Configure your entities here, if needed
            base.OnModelCreating(modelBuilder);
        }
    }
}