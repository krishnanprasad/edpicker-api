using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using edpicker_api.Models.Dto.SyllabusTracker;

namespace edpicker_api.Services.Interface
{
    public interface ISyllabusTrackerRepository
    {
        Task<int> CreateSchoolAsync(SchoolCreateDto request);
        Task SetSchoolSettingsAsync(int schoolId, SchoolSettingsDto request);
        Task<TeacherDto> CreateTeacherAsync(int schoolId, TeacherDto request);
        Task RemoveTeacherAsync(int schoolId, int teacherId);
        Task<ClassDto> AddClassAsync(int schoolId, ClassDto request);
        Task<SubjectDto> AddSubjectAsync(int schoolId, int classId, SubjectDto request);
        Task<ChapterDto> AddChapterAsync(int schoolId, int subjectId, ChapterDto request);
        Task<TopicDto> AddTopicAsync(int schoolId, int chapterId, TopicDto request);
        Task UpdateTopicProgressAsync(int schoolId, int topicId, ProgressUpdateDto request);
        Task<ProgressDto> GetClassProgressAsync(int schoolId, int classId);
        Task<DashboardDto> GetDashboardAsync(int schoolId, DashboardFilterDto filter);
        Task<IEnumerable<AuditLogDto>> GetLogsAsync(int schoolId, LogsFilterDto filter);
    }
}
