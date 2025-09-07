using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using edpicker_api.Models.Dto.SyllabusTracker;
using edpicker_api.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace edpicker_api.Services
{
    public class SyllabusTrackerRepository : ISyllabusTrackerRepository
    {
        private readonly EdPickerDbContext _context;

        public SyllabusTrackerRepository(EdPickerDbContext context)
        {
            _context = context;
        }

        public Task<int> CreateSchoolAsync(SchoolCreateDto request)
        {
            // Placeholder – actual implementation will insert into School table and return new ID
            return Task.FromResult(0);
        }

        public async Task SetSchoolSettingsAsync(int schoolId, SchoolSettingsDto request)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "IF EXISTS (SELECT 1 FROM dbo.ST_SchoolSettings WHERE SchoolId = {0}) " +
                "UPDATE dbo.ST_SchoolSettings SET StartDate = {1}, TotalWorkingDays = {2} WHERE SchoolId = {0} " +
                "ELSE INSERT dbo.ST_SchoolSettings (SchoolId, StartDate, TotalWorkingDays) VALUES ({0}, {1}, {2})",
                schoolId, request.StartDate, request.TotalWorkingDays);
        }

        public Task<TeacherDto> CreateTeacherAsync(int schoolId, TeacherDto request)
        {
            return Task.FromResult(request);
        }

        public Task RemoveTeacherAsync(int schoolId, int teacherId)
        {
            return Task.CompletedTask;
        }

        public async Task<ClassDto> AddClassAsync(int schoolId, ClassDto request)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.ST_Class_Add @SchoolId = {0}, @Name = {1}",
                schoolId, request.Name);
            return request;
        }

        public async Task<SubjectDto> AddSubjectAsync(int schoolId, int classId, SubjectDto request)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.ST_Subject_Add @SchoolId = {0}, @STClassId = {1}, @Name = {2}",
                schoolId, classId, request.Name);
            return request;
        }

        public async Task<ChapterDto> AddChapterAsync(int schoolId, int subjectId, ChapterDto request)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.ST_Chapter_Add @SchoolId = {0}, @STSubjectId = {1}, @Name = {2}",
                schoolId, subjectId, request.Name);
            return request;
        }

        public async Task<TopicDto> AddTopicAsync(int schoolId, int chapterId, TopicDto request)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.ST_Topic_Add @SchoolId = {0}, @STChapterId = {1}, @Name = {2}",
                schoolId, chapterId, request.Name);
            return request;
        }

        public async Task UpdateTopicProgressAsync(int schoolId, int topicId, ProgressUpdateDto request)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.ST_Progress_Add @SchoolId = {0}, @STTopicId = {1}, @Percentage = {2}, @UpdatedBy = {3}",
                schoolId, topicId, request.Percentage, request.UpdatedBy);
        }

        public async Task<ProgressDto> GetClassProgressAsync(int schoolId, int classId)
        {
            var result = await _context.Set<ProgressDto>()
                .FromSqlRaw(
                    "SELECT {1} AS Id, CAST(ISNULL(AVG(sp.SubjectProgress),0) AS INT) AS Percentage " +
                    "FROM dbo.vST_SubjectProgress sp " +
                    "JOIN dbo.ST_Subject s ON s.STSubjectId = sp.STSubjectId " +
                    "WHERE s.SchoolId = {0} AND sp.STClassId = {1}",
                    schoolId, classId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return result ?? new ProgressDto { Id = classId, Percentage = 0 };
        }

        public Task<DashboardDto> GetDashboardAsync(int schoolId, DashboardFilterDto filter)
        {
            return Task.FromResult(new DashboardDto());
        }

        public Task<IEnumerable<AuditLogDto>> GetLogsAsync(int schoolId, LogsFilterDto filter)
        {
            IEnumerable<AuditLogDto> logs = Array.Empty<AuditLogDto>();
            return Task.FromResult(logs);
        }
    }
}
