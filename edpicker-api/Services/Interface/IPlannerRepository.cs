using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using edpicker_api.Models.Planner.Dto;
using edpicker_api.Models.Planner.Requests;

namespace edpicker_api.Services.Interface
{
    public interface IPlannerRepository
    {
        Task<IReadOnlyList<ClassSummaryDto>> GetClassesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SectionSummaryDto>> GetSectionsByClassAsync(int classId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SubjectSummaryDto>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TemplateSummaryDto>> GetTemplatesAsync(int classId, int subjectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TemplateSummaryDto>> GetArchivedTemplatesAsync(CancellationToken cancellationToken = default);
        Task<CurriculumInstanceDto?> GetCurriculumInstanceAsync(int instanceId, CancellationToken cancellationToken = default);
        Task<CurriculumProgressDto?> GetProgressAsync(int instanceId, CancellationToken cancellationToken = default);
        Task<CurriculumStructureDto?> GetCurriculumAsync(int instanceId, CancellationToken cancellationToken = default);
        Task<TopicCompletionStatusDto> UpdateTopicCompletionAsync(int topicId, int instanceId, bool isCompleted, int? userId, string? ipAddress, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SectionProgressComparisonDto>> GetMultiSectionComparisonAsync(int classId, int subjectId, CancellationToken cancellationToken = default);
        Task<PaginatedTopicsResponseDto> GetPaginatedTopicsAsync(int instanceId, int pageNumber, int pageSize, string? searchTerm, string filterStatus, CancellationToken cancellationToken = default);
        Task<int> AssignTemplateAsync(AssignTemplateRequest request, int? userId, string? ipAddress, CancellationToken cancellationToken = default);
        Task<TemplatePreviewDto?> GetTemplatePreviewAsync(int templateId, CancellationToken cancellationToken = default);
        Task<int> CreateCustomTemplateAsync(CreateCustomTemplateRequest request, int? userId, string? ipAddress, CancellationToken cancellationToken = default);
        Task<bool> ArchiveTemplateAsync(int templateId, int? userId, string? ipAddress, CancellationToken cancellationToken = default);
        Task<bool> RestoreTemplateAsync(int templateId, int? userId, string? ipAddress, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DataIntegrityIssueDto>> ValidateDataIntegrityAsync(CancellationToken cancellationToken = default);
        Task<int> FixOrphanedRecordsAsync(CancellationToken cancellationToken = default);
        Task<AcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default);
        Task<int> CreateAcademicYearAsync(string yearName, DateTime startDate, DateTime endDate, int? userId, string? ipAddress, CancellationToken cancellationToken = default);
        Task<bool> FreezeAcademicYearAsync(int academicYearId, int? userId, string? ipAddress, CancellationToken cancellationToken = default);
        Task<byte[]> ExportProgressAsync(int instanceId, CancellationToken cancellationToken = default);
        Task<byte[]> ExportAuditTrailAsync(int instanceId, CancellationToken cancellationToken = default);
    }
}
