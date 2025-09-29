using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using edpicker_api.Models.Planner.Dto;
using edpicker_api.Models.Planner.Entities;
using edpicker_api.Models.Planner.Requests;
using edpicker_api.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace edpicker_api.Services
{
    public class PlannerRepository : IPlannerRepository
    {
        private readonly EdPickerDbContext _context;
        private readonly ILogger<PlannerRepository> _logger;

        public PlannerRepository(EdPickerDbContext context, ILogger<PlannerRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ClassSummaryDto>> GetClassesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.CurriculumClasses
                .AsNoTracking()
                .OrderBy(c => c.ClassName)
                .Select(c => new ClassSummaryDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<SectionSummaryDto>> GetSectionsByClassAsync(int classId, CancellationToken cancellationToken = default)
        {
            return await _context.Sections
                .AsNoTracking()
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.SectionName)
                .Select(s => new SectionSummaryDto
                {
                    SectionId = s.SectionId,
                    SectionName = s.SectionName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<SubjectSummaryDto>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default)
        {
            return await _context.Subjects
                .AsNoTracking()
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.SubjectName)
                .Select(s => new SubjectSummaryDto
                {
                    SubjectId = s.SubjectId,
                    SubjectName = s.SubjectName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<TemplateSummaryDto>> GetTemplatesAsync(int classId, int subjectId, CancellationToken cancellationToken = default)
        {
            return await _context.Templates
                .AsNoTracking()
                .Where(t => t.ClassId == classId && t.SubjectId == subjectId)
                .OrderBy(t => t.TemplateName)
                .Select(t => new TemplateSummaryDto
                {
                    TemplateId = t.TemplateId,
                    TemplateName = t.TemplateName,
                    TemplateType = t.TemplateType,
                    IsArchived = t.IsArchived,
                    CreatedDate = t.CreatedDate
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<TemplateSummaryDto>> GetArchivedTemplatesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Templates
                .AsNoTracking()
                .Where(t => t.IsArchived)
                .OrderByDescending(t => t.CreatedDate)
                .Select(t => new TemplateSummaryDto
                {
                    TemplateId = t.TemplateId,
                    TemplateName = t.TemplateName,
                    TemplateType = t.TemplateType,
                    IsArchived = t.IsArchived,
                    CreatedDate = t.CreatedDate
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<CurriculumInstanceDto?> GetCurriculumInstanceAsync(int instanceId, CancellationToken cancellationToken = default)
        {
            return await _context.CurriculumInstances
                .AsNoTracking()
                .Where(ci => ci.InstanceId == instanceId)
                .Select(ci => new CurriculumInstanceDto
                {
                    InstanceId = ci.InstanceId,
                    AcademicYearId = ci.AcademicYearId,
                    ClassId = ci.ClassId,
                    SectionId = ci.SectionId,
                    SubjectId = ci.SubjectId,
                    TemplateId = ci.TemplateId,
                    AssignedDate = ci.AssignedDate,
                    TemplateName = ci.Template.TemplateName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<CurriculumProgressDto?> GetProgressAsync(int instanceId, CancellationToken cancellationToken = default)
        {
            var instance = await _context.CurriculumInstances
                .Include(ci => ci.Template)
                .FirstOrDefaultAsync(ci => ci.InstanceId == instanceId, cancellationToken);

            if (instance == null)
            {
                return null;
            }

            var query = _context.Topics
                .AsNoTracking()
                .Where(t => !t.IsArchived && !t.Chapter.IsArchived && !t.Chapter.Unit.IsArchived && t.Chapter.Unit.TemplateId == instance.TemplateId);

            var totalTopics = await query.CountAsync(cancellationToken);

            var completedQuery = _context.TopicCompletions
                .AsNoTracking()
                .Where(tc => tc.InstanceId == instanceId && tc.IsCompleted && !tc.Topic.IsArchived && !tc.Topic.Chapter.IsArchived && !tc.Topic.Chapter.Unit.IsArchived);

            var completedTopics = await completedQuery.CountAsync(cancellationToken);

            var lastUpdated = await completedQuery
                .OrderByDescending(tc => tc.UpdatedDate)
                .Select(tc => (DateTime?)tc.UpdatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            var percentage = totalTopics == 0 ? 0 : Math.Round((decimal)completedTopics / totalTopics * 100, 2);

            return new CurriculumProgressDto
            {
                InstanceId = instance.InstanceId,
                TotalTopics = totalTopics,
                CompletedTopics = completedTopics,
                Percentage = percentage,
                TemplateName = instance.Template.TemplateName,
                LastUpdated = lastUpdated
            };
        }

        public async Task<CurriculumStructureDto?> GetCurriculumAsync(int instanceId, CancellationToken cancellationToken = default)
        {
            var instance = await _context.CurriculumInstances
                .AsNoTracking()
                .FirstOrDefaultAsync(ci => ci.InstanceId == instanceId, cancellationToken);

            if (instance == null)
            {
                return null;
            }

            var completions = await _context.TopicCompletions
                .AsNoTracking()
                .Where(tc => tc.InstanceId == instanceId)
                .ToDictionaryAsync(tc => tc.TopicId, tc => tc, cancellationToken);

            var units = await _context.Units
                .AsNoTracking()
                .Where(u => u.TemplateId == instance.TemplateId && !u.IsArchived)
                .OrderBy(u => u.UnitOrder)
                .Select(u => new
                {
                    Unit = u,
                    Chapters = u.Chapters
                        .Where(c => !c.IsArchived)
                        .OrderBy(c => c.ChapterOrder)
                        .Select(c => new
                        {
                            Chapter = c,
                            Topics = c.Topics
                                .Where(t => !t.IsArchived)
                                .OrderBy(t => t.TopicOrder)
                                .ToList()
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            var result = new CurriculumStructureDto
            {
                InstanceId = instanceId
            };

            foreach (var unitData in units)
            {
                var unitDto = new CurriculumUnitDto
                {
                    UnitId = unitData.Unit.UnitId,
                    UnitName = unitData.Unit.UnitName,
                    UnitOrder = unitData.Unit.UnitOrder
                };

                foreach (var chapterData in unitData.Chapters)
                {
                    var chapterDto = new CurriculumChapterDto
                    {
                        ChapterId = chapterData.Chapter.ChapterId,
                        ChapterName = chapterData.Chapter.ChapterName,
                        ChapterOrder = chapterData.Chapter.ChapterOrder
                    };

                    foreach (var topic in chapterData.Topics)
                    {
                        completions.TryGetValue(topic.TopicId, out var completion);
                        chapterDto.Topics.Add(new CurriculumTopicDto
                        {
                            TopicId = topic.TopicId,
                            TopicName = topic.TopicName,
                            TopicOrder = topic.TopicOrder,
                            IsCompleted = completion?.IsCompleted ?? false,
                            CompletedDate = completion?.CompletedDate
                        });
                    }

                    unitDto.Chapters.Add(chapterDto);
                }

                result.Units.Add(unitDto);
            }

            return result;
        }

        public async Task<TopicCompletionStatusDto> UpdateTopicCompletionAsync(int topicId, int instanceId, bool isCompleted, int? userId, string? ipAddress, CancellationToken cancellationToken = default)
        {
            var topic = await _context.Topics
                .Include(t => t.Chapter)
                .ThenInclude(c => c.Unit)
                .FirstOrDefaultAsync(t => t.TopicId == topicId, cancellationToken);

            if (topic == null)
            {
                throw new KeyNotFoundException($"Topic {topicId} not found");
            }

            var instance = await _context.CurriculumInstances
                .FirstOrDefaultAsync(ci => ci.InstanceId == instanceId, cancellationToken);

            if (instance == null)
            {
                throw new KeyNotFoundException($"Curriculum instance {instanceId} not found");
            }

            if (topic.Chapter.Unit.TemplateId != instance.TemplateId)
            {
                throw new InvalidOperationException("Topic does not belong to the specified curriculum instance");
            }

            var completion = await _context.TopicCompletions
                .FirstOrDefaultAsync(tc => tc.InstanceId == instanceId && tc.TopicId == topicId, cancellationToken);

            var now = DateTime.UtcNow;

            if (completion != null)
            {
                if (completion.IsCompleted == isCompleted)
                {
                    _logger.LogInformation("Topic completion skipped (debounced). TopicId: {TopicId}, InstanceId: {InstanceId}", topicId, instanceId);
                    return new TopicCompletionStatusDto
                    {
                        TopicId = topicId,
                        InstanceId = instanceId,
                        IsCompleted = completion.IsCompleted,
                        CompletedDate = completion.CompletedDate,
                        UpdatedDate = completion.UpdatedDate
                    };
                }

                var oldValue = JsonSerializer.Serialize(completion);

                completion.IsCompleted = isCompleted;
                completion.CompletedDate = isCompleted ? now : null;
                completion.UpdatedDate = now;
                completion.UpdatedBy = userId;

                await _context.SaveChangesAsync(cancellationToken);

                await LogAuditAsync("TopicCompletion", completion.CompletionId, "UPDATE", oldValue, JsonSerializer.Serialize(completion), userId, ipAddress, cancellationToken);

                return new TopicCompletionStatusDto
                {
                    TopicId = topicId,
                    InstanceId = instanceId,
                    IsCompleted = completion.IsCompleted,
                    CompletedDate = completion.CompletedDate,
                    UpdatedDate = completion.UpdatedDate
                };
            }
            else
            {
                var newCompletion = new TopicCompletion
                {
                    InstanceId = instanceId,
                    TopicId = topicId,
                    IsCompleted = isCompleted,
                    CompletedDate = isCompleted ? now : null,
                    UpdatedDate = now,
                    UpdatedBy = userId
                };

                _context.TopicCompletions.Add(newCompletion);
                await _context.SaveChangesAsync(cancellationToken);

                await LogAuditAsync("TopicCompletion", newCompletion.CompletionId, "INSERT", null, JsonSerializer.Serialize(newCompletion), userId, ipAddress, cancellationToken);

                return new TopicCompletionStatusDto
                {
                    TopicId = topicId,
                    InstanceId = instanceId,
                    IsCompleted = newCompletion.IsCompleted,
                    CompletedDate = newCompletion.CompletedDate,
                    UpdatedDate = newCompletion.UpdatedDate
                };
            }
        }

        public async Task<IReadOnlyList<SectionProgressComparisonDto>> GetMultiSectionComparisonAsync(int classId, int subjectId, CancellationToken cancellationToken = default)
        {
            var sections = await _context.Sections
                .AsNoTracking()
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.SectionName)
                .ToListAsync(cancellationToken);

            var activeYear = await _context.AcademicYears
                .AsNoTracking()
                .OrderByDescending(y => y.IsActive)
                .ThenByDescending(y => y.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            var instances = await _context.CurriculumInstances
                .AsNoTracking()
                .Where(ci => ci.ClassId == classId && ci.SubjectId == subjectId && (activeYear == null || ci.AcademicYearId == activeYear.AcademicYearId))
                .ToListAsync(cancellationToken);

            var instanceIds = instances.Select(ci => ci.InstanceId).ToList();
            var templateIds = instances.Select(ci => ci.TemplateId).Distinct().ToList();

            var templateNames = await _context.Templates
                .AsNoTracking()
                .Where(t => templateIds.Contains(t.TemplateId))
                .ToDictionaryAsync(t => t.TemplateId, t => t.TemplateName, cancellationToken);

            var completions = await _context.TopicCompletions
                .AsNoTracking()
                .Where(tc => instanceIds.Contains(tc.InstanceId) && tc.IsCompleted)
                .GroupBy(tc => tc.InstanceId)
                .Select(g => new { InstanceId = g.Key, Count = g.Count(), LastUpdated = g.Max(tc => tc.UpdatedDate) })
                .ToListAsync(cancellationToken);

            var completionLookup = completions.ToDictionary(c => c.InstanceId, c => c);

            var templateTopicCounts = await _context.Topics
                .AsNoTracking()
                .Where(t => templateIds.Contains(t.Chapter.Unit.TemplateId) && !t.IsArchived && !t.Chapter.IsArchived && !t.Chapter.Unit.IsArchived)
                .GroupBy(t => t.Chapter.Unit.TemplateId)
                .Select(g => new { TemplateId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var topicCountLookup = templateTopicCounts.ToDictionary(t => t.TemplateId, t => t.Count);

            var result = new List<SectionProgressComparisonDto>();

            foreach (var section in sections)
            {
                var instance = instances.FirstOrDefault(ci => ci.SectionId == section.SectionId);

                if (instance == null)
                {
                    result.Add(new SectionProgressComparisonDto
                    {
                        SectionId = section.SectionId,
                        SectionName = section.SectionName,
                        TemplateName = string.Empty,
                        InstanceId = null,
                        TotalTopics = 0,
                        CompletedTopics = 0,
                        Percentage = 0,
                        LastUpdated = null
                    });
                    continue;
                }

                completionLookup.TryGetValue(instance.InstanceId, out var completionData);
                topicCountLookup.TryGetValue(instance.TemplateId, out var totalTopics);
                templateNames.TryGetValue(instance.TemplateId, out var templateName);

                var completedCount = completionData?.Count ?? 0;
                var totalCount = totalTopics;
                var percentage = totalCount.GetValueOrDefault() == 0 ? 0 : Math.Round((decimal)completedCount / totalCount.Value * 100, 2);

                result.Add(new SectionProgressComparisonDto
                {
                    SectionId = section.SectionId,
                    SectionName = section.SectionName,
                    TemplateName = templateName ?? string.Empty,
                    InstanceId = instance.InstanceId,
                    TotalTopics = totalCount ?? 0,
                    CompletedTopics = completedCount,
                    Percentage = percentage,
                    LastUpdated = completionData?.LastUpdated
                });
            }

            return result;
        }

        public async Task<PaginatedTopicsResponseDto> GetPaginatedTopicsAsync(int instanceId, int pageNumber, int pageSize, string? searchTerm, string filterStatus, CancellationToken cancellationToken = default)
        {
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 20;
            }

            pageSize = Math.Min(pageSize, 100);

            var instance = await _context.CurriculumInstances
                .AsNoTracking()
                .FirstOrDefaultAsync(ci => ci.InstanceId == instanceId, cancellationToken);

            if (instance == null)
            {
                throw new KeyNotFoundException($"Curriculum instance {instanceId} not found");
            }

            var topicsQuery = _context.Topics
                .AsNoTracking()
                .Where(t => t.Chapter.Unit.TemplateId == instance.TemplateId && !t.IsArchived && !t.Chapter.IsArchived && !t.Chapter.Unit.IsArchived);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                topicsQuery = topicsQuery.Where(t => t.TopicName.Contains(searchTerm));
            }

            filterStatus = filterStatus?.ToLowerInvariant() ?? "all";

            if (filterStatus == "completed")
            {
                topicsQuery = topicsQuery.Where(t => _context.TopicCompletions.Any(tc => tc.InstanceId == instanceId && tc.TopicId == t.TopicId && tc.IsCompleted));
            }
            else if (filterStatus == "incomplete")
            {
                topicsQuery = topicsQuery.Where(t => !_context.TopicCompletions.Any(tc => tc.InstanceId == instanceId && tc.TopicId == t.TopicId && tc.IsCompleted));
            }

            var totalCount = await topicsQuery.CountAsync(cancellationToken);
            var completionMap = await _context.TopicCompletions
                .AsNoTracking()
                .Where(tc => tc.InstanceId == instanceId)
                .ToDictionaryAsync(tc => tc.TopicId, tc => tc, cancellationToken);

            var topicEntities = await topicsQuery
                .OrderBy(t => t.TopicOrder)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    t.TopicId,
                    t.TopicName,
                    t.TopicOrder
                })
                .ToListAsync(cancellationToken);

            var topics = topicEntities
                .Select(t =>
                {
                    completionMap.TryGetValue(t.TopicId, out var completion);
                    return new CurriculumTopicDto
                    {
                        TopicId = t.TopicId,
                        TopicName = t.TopicName,
                        TopicOrder = t.TopicOrder,
                        IsCompleted = completion?.IsCompleted ?? false,
                        CompletedDate = completion?.CompletedDate
                    };
                })
                .ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PaginatedTopicsResponseDto
            {
                Topics = topics,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }

        public async Task<int> AssignTemplateAsync(AssignTemplateRequest request, int? userId, string? ipAddress, CancellationToken cancellationToken = default)
        {
            var template = await _context.Templates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TemplateId == request.TemplateId, cancellationToken);

            if (template == null)
            {
                throw new KeyNotFoundException($"Template {request.TemplateId} not found");
            }

            if (template.ClassId != request.ClassId || template.SubjectId != request.SubjectId)
            {
                throw new InvalidOperationException("Template is not compatible with the selected class or subject");
            }

            var existing = await _context.CurriculumInstances
                .FirstOrDefaultAsync(ci => ci.AcademicYearId == request.AcademicYearId && ci.ClassId == request.ClassId && ci.SectionId == request.SectionId && ci.SubjectId == request.SubjectId, cancellationToken);

            if (existing != null)
            {
                throw new InvalidOperationException("Template already assigned to this class-section-subject for the academic year");
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var instance = new CurriculumInstance
                {
                    AcademicYearId = request.AcademicYearId,
                    ClassId = request.ClassId,
                    SectionId = request.SectionId,
                    SubjectId = request.SubjectId,
                    TemplateId = request.TemplateId,
                    AssignedDate = DateTime.UtcNow
                };

                _context.CurriculumInstances.Add(instance);
                await _context.SaveChangesAsync(cancellationToken);

                await LogAuditAsync("CurriculumInstance", instance.InstanceId, "INSERT", null, JsonSerializer.Serialize(instance), userId, ipAddress, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return instance.InstanceId;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<TemplatePreviewDto?> GetTemplatePreviewAsync(int templateId, CancellationToken cancellationToken = default)
        {
            var template = await _context.Templates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TemplateId == templateId, cancellationToken);

            if (template == null)
            {
                return null;
            }

            var units = await _context.Units
                .AsNoTracking()
                .Where(u => u.TemplateId == templateId && !u.IsArchived)
                .OrderBy(u => u.UnitOrder)
                .ToListAsync(cancellationToken);

            var chapters = await _context.Chapters
                .AsNoTracking()
                .Where(c => units.Select(u => u.UnitId).Contains(c.UnitId) && !c.IsArchived)
                .OrderBy(c => c.ChapterOrder)
                .ToListAsync(cancellationToken);

            var topics = await _context.Topics
                .AsNoTracking()
                .Where(t => chapters.Select(c => c.ChapterId).Contains(t.ChapterId) && !t.IsArchived)
                .OrderBy(t => t.TopicOrder)
                .ToListAsync(cancellationToken);

            return new TemplatePreviewDto
            {
                TemplateId = template.TemplateId,
                TemplateName = template.TemplateName,
                UnitCount = units.Count,
                ChapterCount = chapters.Count,
                TopicCount = topics.Count,
                UnitNames = units.Select(u => u.UnitName).ToList(),
                ChapterNames = chapters.Select(c => c.ChapterName).ToList(),
                TopicNames = topics.Select(t => t.TopicName).Take(100).ToList()
            };
        }

        public async Task<int> CreateCustomTemplateAsync(CreateCustomTemplateRequest request, int? userId, string? ipAddress, CancellationToken cancellationToken = default)
        {
            var sourceTemplate = await _context.Templates
                .Include(t => t.Units)
                    .ThenInclude(u => u.Chapters)
                        .ThenInclude(c => c.Topics)
                .FirstOrDefaultAsync(t => t.TemplateId == request.SourceTemplateId, cancellationToken);

            if (sourceTemplate == null)
            {
                throw new KeyNotFoundException($"Source template {request.SourceTemplateId} not found");
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var newTemplate = new Template
                {
                    ClassId = sourceTemplate.ClassId,
                    SubjectId = sourceTemplate.SubjectId,
                    TemplateName = request.NewTemplateName,
                    TemplateType = "Custom",
                    SourceTemplateId = sourceTemplate.TemplateId,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userId,
                    IsArchived = false
                };

                _context.Templates.Add(newTemplate);
                await _context.SaveChangesAsync(cancellationToken);

                if (request.Customizations?.Units != null && request.Customizations.Units.Any())
                {
                    foreach (var unitCustomization in request.Customizations.Units.OrderBy(u => u.UnitOrder))
                    {
                        var unit = new Unit
                        {
                            TemplateId = newTemplate.TemplateId,
                            UnitName = unitCustomization.UnitName,
                            UnitOrder = unitCustomization.UnitOrder,
                            CreatedDate = DateTime.UtcNow,
                            IsArchived = false
                        };

                        _context.Units.Add(unit);
                        await _context.SaveChangesAsync(cancellationToken);

                        foreach (var chapterCustomization in unitCustomization.Chapters.OrderBy(c => c.ChapterOrder))
                        {
                            var chapter = new Chapter
                            {
                                UnitId = unit.UnitId,
                                ChapterName = chapterCustomization.ChapterName,
                                ChapterOrder = chapterCustomization.ChapterOrder,
                                CreatedDate = DateTime.UtcNow,
                                IsArchived = false
                            };

                            _context.Chapters.Add(chapter);
                            await _context.SaveChangesAsync(cancellationToken);

                            foreach (var topicCustomization in chapterCustomization.Topics.OrderBy(t => t.TopicOrder))
                            {
                                var topic = new Topic
                                {
                                    ChapterId = chapter.ChapterId,
                                    TopicName = topicCustomization.TopicName,
                                    TopicOrder = topicCustomization.TopicOrder,
                                    CreatedDate = DateTime.UtcNow,
                                    IsArchived = false
                                };

                                _context.Topics.Add(topic);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var unit in sourceTemplate.Units.OrderBy(u => u.UnitOrder))
                    {
                        var clonedUnit = new Unit
                        {
                            TemplateId = newTemplate.TemplateId,
                            UnitName = unit.UnitName,
                            UnitOrder = unit.UnitOrder,
                            CreatedDate = DateTime.UtcNow,
                            IsArchived = false
                        };

                        _context.Units.Add(clonedUnit);
                        await _context.SaveChangesAsync(cancellationToken);

                        foreach (var chapter in unit.Chapters.OrderBy(c => c.ChapterOrder))
                        {
                            var clonedChapter = new Chapter
                            {
                                UnitId = clonedUnit.UnitId,
                                ChapterName = chapter.ChapterName,
                                ChapterOrder = chapter.ChapterOrder,
                                CreatedDate = DateTime.UtcNow,
                                IsArchived = false
                            };

                            _context.Chapters.Add(clonedChapter);
                            await _context.SaveChangesAsync(cancellationToken);

                            foreach (var topic in chapter.Topics.OrderBy(t => t.TopicOrder))
                            {
                                var clonedTopic = new Topic
                                {
                                    ChapterId = clonedChapter.ChapterId,
                                    TopicName = topic.TopicName,
                                    TopicOrder = topic.TopicOrder,
                                    CreatedDate = DateTime.UtcNow,
                                    IsArchived = false
                                };

                                _context.Topics.Add(clonedTopic);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                await LogAuditAsync("Template", newTemplate.TemplateId, "INSERT", null, JsonSerializer.Serialize(newTemplate), userId, ipAddress, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return newTemplate.TemplateId;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> ArchiveTemplateAsync(int templateId, int? userId, string? ipAddress, CancellationToken cancellationToken = default)
        {
            var template = await _context.Templates
                .Include(t => t.Units)
                    .ThenInclude(u => u.Chapters)
                        .ThenInclude(c => c.Topics)
                .FirstOrDefaultAsync(t => t.TemplateId == templateId, cancellationToken);

            if (template == null)
            {
                throw new KeyNotFoundException($"Template {templateId} not found");
            }

            var inUse = await _context.CurriculumInstances
                .AnyAsync(ci => ci.TemplateId == templateId, cancellationToken);

            if (inUse)
            {
                throw new InvalidOperationException("Cannot archive template while it is assigned to active curriculum instances");
            }

            if (template.IsArchived)
            {
                return false;
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                template.IsArchived = true;

                foreach (var unit in template.Units)
                {
                    unit.IsArchived = true;
                    foreach (var chapter in unit.Chapters)
                    {
                        chapter.IsArchived = true;
                        foreach (var topic in chapter.Topics)
                        {
                            topic.IsArchived = true;
                        }
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                await LogAuditAsync("Template", template.TemplateId, "ARCHIVE", null, JsonSerializer.Serialize(template), userId, ipAddress, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> RestoreTemplateAsync(int templateId, int? userId, string? ipAddress, CancellationToken cancellationToken = default)
        {
            var template = await _context.Templates
                .Include(t => t.Units)
                    .ThenInclude(u => u.Chapters)
                        .ThenInclude(c => c.Topics)
                .FirstOrDefaultAsync(t => t.TemplateId == templateId, cancellationToken);

            if (template == null)
            {
                throw new KeyNotFoundException($"Template {templateId} not found");
            }

            if (!template.IsArchived)
            {
                return false;
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                template.IsArchived = false;

                foreach (var unit in template.Units)
                {
                    unit.IsArchived = false;
                    foreach (var chapter in unit.Chapters)
                    {
                        chapter.IsArchived = false;
                        foreach (var topic in chapter.Topics)
                        {
                            topic.IsArchived = false;
                        }
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                await LogAuditAsync("Template", template.TemplateId, "RESTORE", null, JsonSerializer.Serialize(template), userId, ipAddress, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<IReadOnlyList<DataIntegrityIssueDto>> ValidateDataIntegrityAsync(CancellationToken cancellationToken = default)
        {
            var issues = new List<DataIntegrityIssueDto>();

            var orphanedTopics = await _context.Topics
                .AsNoTracking()
                .Where(t => !_context.Chapters.Any(c => c.ChapterId == t.ChapterId))
                .Select(t => t.TopicId)
                .ToListAsync(cancellationToken);

            if (orphanedTopics.Any())
            {
                issues.Add(new DataIntegrityIssueDto
                {
                    IssueType = "OrphanedTopics",
                    Description = $"Topics without chapters: {string.Join(", ", orphanedTopics.Take(20))}"
                });
            }

            var orphanedChapters = await _context.Chapters
                .AsNoTracking()
                .Where(c => !_context.Units.Any(u => u.UnitId == c.UnitId))
                .Select(c => c.ChapterId)
                .ToListAsync(cancellationToken);

            if (orphanedChapters.Any())
            {
                issues.Add(new DataIntegrityIssueDto
                {
                    IssueType = "OrphanedChapters",
                    Description = $"Chapters without units: {string.Join(", ", orphanedChapters.Take(20))}"
                });
            }

            var orphanedUnits = await _context.Units
                .AsNoTracking()
                .Where(u => !_context.Templates.Any(t => t.TemplateId == u.TemplateId))
                .Select(u => u.UnitId)
                .ToListAsync(cancellationToken);

            if (orphanedUnits.Any())
            {
                issues.Add(new DataIntegrityIssueDto
                {
                    IssueType = "OrphanedUnits",
                    Description = $"Units without templates: {string.Join(", ", orphanedUnits.Take(20))}"
                });
            }

            var invalidCompletions = await _context.TopicCompletions
                .AsNoTracking()
                .Where(tc => !_context.Topics.Any(t => t.TopicId == tc.TopicId))
                .Select(tc => tc.CompletionId)
                .ToListAsync(cancellationToken);

            if (invalidCompletions.Any())
            {
                issues.Add(new DataIntegrityIssueDto
                {
                    IssueType = "InvalidCompletions",
                    Description = $"Completion records referencing missing topics: {string.Join(", ", invalidCompletions.Take(20))}"
                });
            }

            return issues;
        }

        public async Task<int> FixOrphanedRecordsAsync(CancellationToken cancellationToken = default)
        {
            var orphanedCompletionIds = await _context.TopicCompletions
                .Where(tc => !_context.Topics.Any(t => t.TopicId == tc.TopicId))
                .Select(tc => tc.CompletionId)
                .ToListAsync(cancellationToken);

            if (!orphanedCompletionIds.Any())
            {
                return 0;
            }

            var completions = await _context.TopicCompletions
                .Where(tc => orphanedCompletionIds.Contains(tc.CompletionId))
                .ToListAsync(cancellationToken);

            _context.TopicCompletions.RemoveRange(completions);
            await _context.SaveChangesAsync(cancellationToken);

            return completions.Count;
        }

        public async Task<AcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default)
        {
            return await _context.AcademicYears
                .AsNoTracking()
                .Where(y => y.IsActive)
                .OrderByDescending(y => y.StartDate)
                .Select(y => new AcademicYearDto
                {
                    AcademicYearId = y.AcademicYearId,
                    YearName = y.YearName,
                    StartDate = y.StartDate,
                    EndDate = y.EndDate,
                    IsActive = y.IsActive,
                    IsFrozen = y.IsFrozen
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> CreateAcademicYearAsync(string yearName, DateTime startDate, DateTime endDate, int? userId, string? ipAddress, CancellationToken cancellationToken = default)
        {
            if (endDate <= startDate)
            {
                throw new InvalidOperationException("Academic year end date must be after start date");
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingActiveYears = await _context.AcademicYears.Where(y => y.IsActive).ToListAsync(cancellationToken);
                foreach (var year in existingActiveYears)
                {
                    year.IsActive = false;
                }

                var newYear = new AcademicYear
                {
                    YearName = yearName,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsActive = true,
                    IsFrozen = false
                };

                _context.AcademicYears.Add(newYear);
                await _context.SaveChangesAsync(cancellationToken);

                await LogAuditAsync("AcademicYear", newYear.AcademicYearId, "INSERT", null, JsonSerializer.Serialize(newYear), userId, ipAddress, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return newYear.AcademicYearId;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> FreezeAcademicYearAsync(int academicYearId, int? userId, string? ipAddress, CancellationToken cancellationToken = default)
        {
            var year = await _context.AcademicYears.FirstOrDefaultAsync(y => y.AcademicYearId == academicYearId, cancellationToken);

            if (year == null)
            {
                throw new KeyNotFoundException($"Academic year {academicYearId} not found");
            }

            if (year.IsFrozen)
            {
                return false;
            }

            year.IsFrozen = true;
            await _context.SaveChangesAsync(cancellationToken);

            await LogAuditAsync("AcademicYear", year.AcademicYearId, "FREEZE", null, JsonSerializer.Serialize(year), userId, ipAddress, cancellationToken);

            return true;
        }

        public async Task<byte[]> ExportProgressAsync(int instanceId, CancellationToken cancellationToken = default)
        {
            var curriculum = await GetCurriculumAsync(instanceId, cancellationToken);
            if (curriculum == null)
            {
                throw new KeyNotFoundException($"Curriculum instance {instanceId} not found");
            }

            var lines = new List<string> { "Unit,Chapter,Topic,IsCompleted,CompletedDate" };
            foreach (var unit in curriculum.Units)
            {
                foreach (var chapter in unit.Chapters)
                {
                    foreach (var topic in chapter.Topics)
                    {
                        lines.Add($"{Escape(unit.UnitName)},{Escape(chapter.ChapterName)},{Escape(topic.TopicName)},{topic.IsCompleted},{topic.CompletedDate:O}");
                    }
                }
            }

            return Encoding.UTF8.GetBytes(string.Join("\n", lines));
        }

        public async Task<byte[]> ExportAuditTrailAsync(int instanceId, CancellationToken cancellationToken = default)
        {
            var completionIds = await _context.TopicCompletions
                .Where(tc => tc.InstanceId == instanceId)
                .Select(tc => tc.CompletionId)
                .ToListAsync(cancellationToken);

            if (!completionIds.Any())
            {
                return Array.Empty<byte>();
            }

            var completionIdsLong = completionIds.Select(id => (long)id).ToList();

            var logs = await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.TableName == "TopicCompletion" && completionIdsLong.Contains(a.RecordId))
                .OrderBy(a => a.UpdatedDate)
                .ToListAsync(cancellationToken);

            var lines = new List<string> { "AuditId,TableName,RecordId,Action,UpdatedDate,UpdatedBy,IpAddress" };
            lines.AddRange(logs.Select(l => $"{l.AuditId},{Escape(l.TableName)},{l.RecordId},{Escape(l.Action)},{l.UpdatedDate:O},{l.UpdatedBy},{Escape(l.IpAddress ?? string.Empty)}"));

            return Encoding.UTF8.GetBytes(string.Join("\n", lines));
        }

        private static string Escape(string value)
        {
            if (value.Contains(',') || value.Contains('"'))
            {
                var escaped = value.Replace("\"", "\"\"");
                return $"\"{escaped}\"";
            }

            return value;
        }

        private async Task LogAuditAsync(string tableName, long recordId, string action, string? oldValue, string? newValue, int? userId, string? ipAddress, CancellationToken cancellationToken)
        {
            var log = new AuditLog
            {
                TableName = tableName,
                RecordId = recordId,
                Action = action,
                OldValue = oldValue,
                NewValue = newValue,
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = userId,
                IpAddress = ipAddress
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
