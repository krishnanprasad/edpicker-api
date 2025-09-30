using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using edpicker_api.Models.Celebration;
using edpicker_api.Models.Celebration.Dtos;
using edpicker_api.Models.Celebration.Entities;
using edpicker_api.Models.Celebration.Enums;
using edpicker_api.Services.Interface;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace edpicker_api.Services;

public class CelebrationService : ICelebrationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex DigitsOnly = new("[0-9]+", RegexOptions.Compiled);
    private static volatile bool _encodingRegistered;

    private readonly EdPickerDbContext _dbContext;
    private readonly ILogger<CelebrationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CelebrationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public CelebrationService(
        EdPickerDbContext dbContext,
        ILogger<CelebrationService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<CelebrationOptions> options,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _timeZone = ResolveTimeZone(_options.TimeZoneId);
    }

    public async Task<UploadProfilesResult> UploadProfilesAsync(int adminId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is required", nameof(file));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var imported = 0;
        var errors = new List<string>();
        var now = GetCurrentLocalTime();

        List<UploadRecord> records = extension switch
        {
            ".csv" => ParseCsv(file),
            ".xlsx" or ".xls" => ParseExcel(file),
            _ => throw new ArgumentException("Unsupported file format. Use .csv or .xlsx", nameof(file))
        };

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Name))
            {
                errors.Add($"Row {record.RowNumber}: Name is required");
                continue;
            }

            if (!TryNormalizeType(record.Type, out var type))
            {
                errors.Add($"Row {record.RowNumber}: Unknown Type '{record.Type}'");
                continue;
            }

            if (!TryNormalizePhone(record.Phone, out var phone, out var phoneError))
            {
                errors.Add($"Row {record.RowNumber}: {phoneError}");
                continue;
            }

            if (!TryParseDate(record.Dob, out var dob, out var dobError))
            {
                errors.Add($"Row {record.RowNumber}: {dobError}");
                continue;
            }

            var consent = ParseConsent(record.Consent);

            if (type == CelebrationRecipientType.Student)
            {
                var student = new CelebrationStudent
                {
                    AdminId = adminId,
                    Name = record.Name?.Trim() ?? string.Empty,
                    DateOfBirth = dob,
                    Phone = phone,
                    Consent = consent,
                    CreatedAt = now
                };

                _dbContext.CelebrationStudents.Add(student);
                imported++;
            }
            else
            {
                if (!TryParseAnniversary(record.Anniversary, out var anniversary, out var anniversaryError))
                {
                    errors.Add($"Row {record.RowNumber}: {anniversaryError}");
                    continue;
                }

                var teacher = new CelebrationTeacher
                {
                    AdminId = adminId,
                    Name = record.Name?.Trim() ?? string.Empty,
                    DateOfBirth = dob,
                    Anniversary = anniversary,
                    Phone = phone,
                    Consent = consent,
                    CreatedAt = now
                };

                _dbContext.CelebrationTeachers.Add(teacher);
                imported++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditLogAsync(adminId, CelebrationAuditAction.Upload, $"Imported {imported} records", now, cancellationToken);

        return new UploadProfilesResult
        {
            Imported = imported,
            Errors = errors
        };
    }

    public async Task<StudentSummaryDto> UpsertStudentAsync(int adminId, StudentUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!TryNormalizePhone(request.Phone, out var phone, out var phoneError))
        {
            throw new ArgumentException(phoneError ?? "Invalid phone", nameof(request.Phone));
        }

        var now = GetCurrentLocalTime();

        CelebrationStudent entity;
        if (request.StudentId.HasValue)
        {
            entity = await _dbContext.CelebrationStudents
                .FirstOrDefaultAsync(s => s.StudentId == request.StudentId && s.AdminId == adminId, cancellationToken)
                ?? throw new KeyNotFoundException("Student not found");

            entity.Name = request.Name.Trim();
            entity.DateOfBirth = request.DateOfBirth;
            entity.Phone = phone;
            entity.Consent = request.Consent;
            entity.UpdatedAt = now;
        }
        else
        {
            entity = new CelebrationStudent
            {
                AdminId = adminId,
                Name = request.Name.Trim(),
                DateOfBirth = request.DateOfBirth,
                Phone = phone,
                Consent = request.Consent,
                CreatedAt = now
            };

            _dbContext.CelebrationStudents.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditLogAsync(adminId, CelebrationAuditAction.UpsertStudent, $"StudentId={entity.StudentId}", now, cancellationToken);

        return new StudentSummaryDto(entity.StudentId, entity.Name, entity.Phone);
    }

    public async Task<TeacherEventDto> UpsertTeacherAsync(int adminId, TeacherUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!TryNormalizePhone(request.Phone, out var phone, out var phoneError))
        {
            throw new ArgumentException(phoneError ?? "Invalid phone", nameof(request.Phone));
        }

        var now = GetCurrentLocalTime();

        CelebrationTeacher entity;
        if (request.TeacherId.HasValue)
        {
            entity = await _dbContext.CelebrationTeachers
                .FirstOrDefaultAsync(t => t.TeacherId == request.TeacherId && t.AdminId == adminId, cancellationToken)
                ?? throw new KeyNotFoundException("Teacher not found");

            entity.Name = request.Name.Trim();
            entity.DateOfBirth = request.DateOfBirth;
            entity.Anniversary = request.Anniversary;
            entity.Phone = phone;
            entity.Consent = request.Consent;
            entity.UpdatedAt = now;
        }
        else
        {
            entity = new CelebrationTeacher
            {
                AdminId = adminId,
                Name = request.Name.Trim(),
                DateOfBirth = request.DateOfBirth,
                Anniversary = request.Anniversary,
                Phone = phone,
                Consent = request.Consent,
                CreatedAt = now
            };

            _dbContext.CelebrationTeachers.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditLogAsync(adminId, CelebrationAuditAction.UpsertTeacher, $"TeacherId={entity.TeacherId}", now, cancellationToken);

        return new TeacherEventDto(
            entity.TeacherId,
            entity.Name,
            CelebrationEventType.Birthday,
            entity.Phone,
            CelebrationDeliveryStatus.Sent,
            0,
            null,
            null);
    }

    public async Task<TodayDashboardResponse> GetTodayDashboardAsync(int adminId, CancellationToken cancellationToken)
    {
        var today = GetCurrentLocalDate();

        var schoolName = await _dbContext.CelebrationConfigurations
            .AsNoTracking()
            .Where(c => c.AdminId == adminId)
            .Select(c => c.SchoolName)
            .FirstOrDefaultAsync(cancellationToken);

        var teacherBirthdays = await _dbContext.CelebrationTeachers
            .AsNoTracking()
            .Where(t => t.AdminId == adminId && t.Consent && t.DateOfBirth.Month == today.Month && t.DateOfBirth.Day == today.Day)
            .Select(t => new { t.TeacherId, t.Name, t.Phone, EventType = CelebrationEventType.Birthday })
            .ToListAsync(cancellationToken);

        var teacherAnniversaries = await _dbContext.CelebrationTeachers
            .AsNoTracking()
            .Where(t => t.AdminId == adminId && t.Consent && t.Anniversary.HasValue && t.Anniversary.Value.Month == today.Month && t.Anniversary.Value.Day == today.Day)
            .Select(t => new { t.TeacherId, t.Name, t.Phone, EventType = CelebrationEventType.Anniversary })
            .ToListAsync(cancellationToken);

        var teacherEvents = teacherBirthdays.Concat(teacherAnniversaries).ToList();

        var logs = await _dbContext.CelebrationLogs
            .AsNoTracking()
            .Where(l => l.AdminId == adminId && l.EventDate == today)
            .ToListAsync(cancellationToken);

        var teacherDtos = new List<TeacherEventDto>();
        foreach (var evt in teacherEvents)
        {
            var statusLog = logs
                .Where(l => l.RecipientType == CelebrationRecipientType.Teacher && l.RecipientId == evt.TeacherId && l.EventType == evt.EventType)
                .OrderByDescending(l => l.Attempt)
                .ThenByDescending(l => l.SentAt)
                .FirstOrDefault();

            teacherDtos.Add(new TeacherEventDto(
                evt.TeacherId,
                evt.Name,
                evt.EventType,
                evt.Phone,
                statusLog?.Status ?? CelebrationDeliveryStatus.Sent,
                statusLog?.Attempt ?? 0,
                statusLog?.SentAt,
                statusLog?.FailureReason));
        }

        var studentBirthdays = await _dbContext.CelebrationStudents
            .AsNoTracking()
            .Where(s => s.AdminId == adminId && s.Consent && s.DateOfBirth.Month == today.Month && s.DateOfBirth.Day == today.Day)
            .Select(s => new StudentSummaryDto(s.StudentId, s.Name, s.Phone))
            .ToListAsync(cancellationToken);

        var totalEvents = teacherEvents.Count + studentBirthdays.Count;
        var totalFailed = logs.Count(l => l.Status == CelebrationDeliveryStatus.Failed && l.EventDate == today);
        var totalSent = logs.Count(l => l.Status != CelebrationDeliveryStatus.Failed && l.EventDate == today);
        var successRate = totalEvents == 0
            ? 0
            : Math.Round((decimal)(totalEvents - totalFailed) / totalEvents * 100, 2);

        var alerts = new List<DashboardAlertDto>();
        if (totalFailed > 0)
        {
            alerts.Add(new DashboardAlertDto($"{totalFailed} messages failed", "error"));
        }

        return new TodayDashboardResponse
        {
            Date = today,
            SchoolName = schoolName,
            Teachers = teacherDtos,
            StudentsCount = studentBirthdays.Count,
            Students = studentBirthdays.Count <= 50 ? studentBirthdays : Array.Empty<StudentSummaryDto>(),
            TotalEvents = totalEvents,
            TotalSent = totalSent,
            TotalFailed = totalFailed,
            DeliverySuccessRate = successRate,
            Alerts = alerts
        };
    }

    public async Task<HistoryResponse> GetHistoryAsync(int adminId, DateOnly date, CancellationToken cancellationToken)
    {
        var logs = await _dbContext.CelebrationLogs
            .AsNoTracking()
            .Where(l => l.AdminId == adminId && l.EventDate == date)
            .OrderBy(l => l.SentAt)
            .Select(l => new HistoryLogDto(
                l.LogId,
                l.RecipientType,
                l.RecipientName,
                l.EventType,
                l.Phone,
                l.Status,
                l.Attempt,
                l.SentAt,
                l.Message,
                l.FailureReason))
            .ToListAsync(cancellationToken);

        return new HistoryResponse
        {
            Date = date,
            Logs = logs
        };
    }

    public async Task<SchoolConfigResponse> SetSchoolNameAsync(int adminId, SchoolConfigRequest request, CancellationToken cancellationToken)
    {
        var name = request.SchoolName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("SchoolName cannot be empty", nameof(request.SchoolName));
        }

        var now = GetCurrentLocalTime();

        var config = await _dbContext.CelebrationConfigurations
            .FirstOrDefaultAsync(c => c.AdminId == adminId, cancellationToken);

        if (config == null)
        {
            config = new CelebrationConfiguration
            {
                AdminId = adminId,
                SchoolName = name,
                UpdatedAt = now
            };

            _dbContext.CelebrationConfigurations.Add(config);
        }
        else
        {
            config.SchoolName = name;
            config.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditLogAsync(adminId, CelebrationAuditAction.ConfigureSchool, name, now, cancellationToken);

        return new SchoolConfigResponse
        {
            SchoolName = config.SchoolName,
            UpdatedAt = config.UpdatedAt
        };
    }

    public async Task<SmsSendResultDto> SendTestSmsAsync(int adminId, SmsTestRequest request, CancellationToken cancellationToken)
    {
        var schoolName = await _dbContext.CelebrationConfigurations
            .AsNoTracking()
            .Where(c => c.AdminId == adminId)
            .Select(c => c.SchoolName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Your School";

        string name;
        string phone;
        CelebrationEventType eventType = CelebrationEventType.Birthday;

        if (request.RecipientType == CelebrationRecipientType.Student)
        {
            var student = await _dbContext.CelebrationStudents
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentId == request.RecipientId && s.AdminId == adminId, cancellationToken)
                ?? throw new KeyNotFoundException("Student not found");

            name = student.Name;
            phone = student.Phone;
        }
        else
        {
            var teacher = await _dbContext.CelebrationTeachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TeacherId == request.RecipientId && t.AdminId == adminId, cancellationToken)
                ?? throw new KeyNotFoundException("Teacher not found");

            name = teacher.Name;
            phone = teacher.Phone;
            eventType = CelebrationEventType.Anniversary;
        }

        var outcome = await SendCelebrationMessageAsync(phone, name, schoolName, eventType, request.Message, cancellationToken);

        var now = GetCurrentLocalTime();
        await AddAuditLogAsync(adminId, CelebrationAuditAction.TestSms, outcome.Success ? "Success" : outcome.Error, now, cancellationToken);

        return new SmsSendResultDto
        {
            Success = outcome.Success,
            Error = outcome.Error
        };
    }

    public async Task<OptOutResponse> HandleOptOutAsync(OptOutRequest request, CancellationToken cancellationToken)
    {
        if (!TryNormalizePhone(request.Phone, out var phone, out var phoneError))
        {
            throw new ArgumentException(phoneError ?? "Invalid phone", nameof(request.Phone));
        }

        var message = request.Message?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!message.Contains("STOP", StringComparison.OrdinalIgnoreCase))
        {
            return new OptOutResponse { Updated = false };
        }

        var now = GetCurrentLocalTime();

        var students = await _dbContext.CelebrationStudents
            .Where(s => s.Phone == phone && s.Consent)
            .ToListAsync(cancellationToken);

        var teachers = await _dbContext.CelebrationTeachers
            .Where(t => t.Phone == phone && t.Consent)
            .ToListAsync(cancellationToken);

        foreach (var student in students)
        {
            student.Consent = false;
            student.UpdatedAt = now;
        }

        foreach (var teacher in teachers)
        {
            teacher.Consent = false;
            teacher.UpdatedAt = now;
        }

        var updated = students.Count + teachers.Count;
        if (updated > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            var adminIds = students.Select(s => s.AdminId)
                .Concat(teachers.Select(t => t.AdminId))
                .Distinct()
                .ToList();

            foreach (var adminId in adminIds)
            {
                await AddAuditLogAsync(adminId, CelebrationAuditAction.OptOut, $"Phone {phone}", now, cancellationToken);
            }
        }

        return new OptOutResponse { Updated = updated > 0 };
    }

    public async Task SendDailyRemindersAsync(CancellationToken cancellationToken)
    {
        var today = GetCurrentLocalDate();
        var now = GetCurrentLocalTime();

        var adminIds = await _dbContext.CelebrationStudents.Select(s => s.AdminId)
            .Union(_dbContext.CelebrationTeachers.Select(t => t.AdminId))
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var adminId in adminIds)
        {
            var schoolName = await _dbContext.CelebrationConfigurations
                .AsNoTracking()
                .Where(c => c.AdminId == adminId)
                .Select(c => c.SchoolName)
                .FirstOrDefaultAsync(cancellationToken) ?? "Your School";

            await SendStudentMessagesAsync(adminId, schoolName, today, cancellationToken);
            await SendTeacherMessagesAsync(adminId, schoolName, today, cancellationToken);

            await AddAuditLogAsync(adminId, CelebrationAuditAction.Send, $"Daily reminders for {today:yyyy-MM-dd}", now, cancellationToken);
        }

        await PruneOldRecordsAsync(today, cancellationToken);
    }

    private async Task SendStudentMessagesAsync(int adminId, string schoolName, DateOnly date, CancellationToken cancellationToken)
    {
        var students = await _dbContext.CelebrationStudents
            .Where(s => s.AdminId == adminId && s.Consent && s.DateOfBirth.Month == date.Month && s.DateOfBirth.Day == date.Day)
            .ToListAsync(cancellationToken);

        if (students.Count == 0)
        {
            return;
        }

        var existingLogs = await _dbContext.CelebrationLogs
            .Where(l => l.AdminId == adminId && l.EventDate == date && l.RecipientType == CelebrationRecipientType.Student)
            .ToListAsync(cancellationToken);

        foreach (var student in students)
        {
            await SendWithRetriesAsync(adminId, schoolName, date, existingLogs, CelebrationRecipientType.Student, student.StudentId, student.Name, student.Phone, CelebrationEventType.Birthday, cancellationToken);
        }
    }

    private async Task SendTeacherMessagesAsync(int adminId, string schoolName, DateOnly date, CancellationToken cancellationToken)
    {
        var teachers = await _dbContext.CelebrationTeachers
            .Where(t => t.AdminId == adminId && t.Consent &&
                        ((t.DateOfBirth.Month == date.Month && t.DateOfBirth.Day == date.Day) ||
                         (t.Anniversary.HasValue && t.Anniversary.Value.Month == date.Month && t.Anniversary.Value.Day == date.Day)))
            .ToListAsync(cancellationToken);

        if (teachers.Count == 0)
        {
            return;
        }

        var existingLogs = await _dbContext.CelebrationLogs
            .Where(l => l.AdminId == adminId && l.EventDate == date && l.RecipientType == CelebrationRecipientType.Teacher)
            .ToListAsync(cancellationToken);

        foreach (var teacher in teachers)
        {
            if (teacher.DateOfBirth.Month == date.Month && teacher.DateOfBirth.Day == date.Day)
            {
                await SendWithRetriesAsync(adminId, schoolName, date, existingLogs, CelebrationRecipientType.Teacher, teacher.TeacherId, teacher.Name, teacher.Phone, CelebrationEventType.Birthday, cancellationToken);
            }

            if (teacher.Anniversary.HasValue && teacher.Anniversary.Value.Month == date.Month && teacher.Anniversary.Value.Day == date.Day)
            {
                await SendWithRetriesAsync(adminId, schoolName, date, existingLogs, CelebrationRecipientType.Teacher, teacher.TeacherId, teacher.Name, teacher.Phone, CelebrationEventType.Anniversary, cancellationToken);
            }
        }
    }

    private async Task SendWithRetriesAsync(
        int adminId,
        string schoolName,
        DateOnly date,
        List<CelebrationLog> existingLogs,
        CelebrationRecipientType recipientType,
        int recipientId,
        string name,
        string phone,
        CelebrationEventType eventType,
        CancellationToken cancellationToken)
    {
        var attemptsToday = existingLogs
            .Where(l => l.RecipientType == recipientType && l.RecipientId == recipientId && l.EventType == eventType)
            .OrderBy(l => l.Attempt)
            .ToList();

        if (attemptsToday.Any(l => l.Status != CelebrationDeliveryStatus.Failed))
        {
            return;
        }

        for (var attemptIndex = attemptsToday.Count; attemptIndex < _options.SendRetryLimit; attemptIndex++)
        {
            var currentAttempt = attemptIndex + 1;
            var attemptTime = GetCurrentLocalTime();
            var outcome = await SendCelebrationMessageAsync(phone, name, schoolName, eventType, null, cancellationToken);

            var log = new CelebrationLog
            {
                AdminId = adminId,
                RecipientType = recipientType,
                RecipientId = recipientId,
                RecipientName = name,
                EventType = eventType,
                Phone = phone,
                Message = outcome.Message,
                Status = outcome.Success ? CelebrationDeliveryStatus.Sent : CelebrationDeliveryStatus.Failed,
                FailureReason = outcome.Error,
                Attempt = currentAttempt,
                EventDate = date,
                SentAt = attemptTime
            };

            _dbContext.CelebrationLogs.Add(log);
            existingLogs.Add(log);

            await _dbContext.SaveChangesAsync(cancellationToken);

            if (outcome.Success)
            {
                break;
            }
        }
    }

    private async Task PruneOldRecordsAsync(DateOnly date, CancellationToken cancellationToken)
    {
        if (_options.LogRetentionDays <= 0)
        {
            return;
        }

        var cutoff = date.AddDays(-_options.LogRetentionDays);

        var oldLogs = await _dbContext.CelebrationLogs
            .Where(l => l.EventDate < cutoff)
            .ToListAsync(cancellationToken);

        if (oldLogs.Count > 0)
        {
            _dbContext.CelebrationLogs.RemoveRange(oldLogs);
        }

        var oldAudits = await _dbContext.CelebrationAuditLogs
            .Where(a => DateOnly.FromDateTime(a.Timestamp.DateTime) < cutoff)
            .ToListAsync(cancellationToken);

        if (oldAudits.Count > 0)
        {
            _dbContext.CelebrationAuditLogs.RemoveRange(oldAudits);
        }

        if (oldLogs.Count > 0 || oldAudits.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task AddAuditLogAsync(int adminId, CelebrationAuditAction action, string? details, DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        var audit = new CelebrationAuditLog
        {
            AdminId = adminId,
            Action = action,
            Details = details,
            Timestamp = timestamp
        };

        _dbContext.CelebrationAuditLogs.Add(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SmsOutcome> SendCelebrationMessageAsync(string phone, string name, string schoolName, CelebrationEventType eventType, string? overrideMessage, CancellationToken cancellationToken)
    {
        var defaultMessage = ComposeDefaultMessage(eventType, name, schoolName);

        if (!string.IsNullOrWhiteSpace(overrideMessage))
        {
            return await SendCustomMessageAsync(phone, overrideMessage!, cancellationToken);
        }

        var templateId = eventType == CelebrationEventType.Birthday
            ? _options.BirthdayTemplateId
            : _options.AnniversaryTemplateId;

        if (string.IsNullOrWhiteSpace(templateId))
        {
            _logger.LogWarning("MSG91 template ID not configured for {EventType}. Falling back to custom message.", eventType);
            return await SendCustomMessageAsync(phone, defaultMessage, cancellationToken);
        }

        var client = _httpClientFactory.CreateClient("msg91");
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v5/flow/");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("authkey", _options.ApiKey);

        var payload = new
        {
            flow_id = templateId,
            sender = _options.SenderId,
            recipients = new[]
            {
                new
                {
                    mobiles = StripPlus(phone),
                    VAR1 = name,
                    VAR2 = schoolName,
                    PE_ID = _options.PeId
                }
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return SmsOutcome.Success(defaultMessage);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("MSG91 template send failed: {Status} {Body}", response.StatusCode, body);
            return SmsOutcome.Failure(defaultMessage, $"MSG91 template error: {body}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send MSG91 template message");
            return SmsOutcome.Failure(defaultMessage, ex.Message);
        }
    }

    private async Task<SmsOutcome> SendCustomMessageAsync(string phone, string message, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("msg91");
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/sendsms");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("authkey", _options.ApiKey);

        var payload = new
        {
            sender = _options.SenderId,
            route = "4",
            country = "91",
            sms = new[]
            {
                new
                {
                    message,
                    to = new[] { StripPlus(phone) }
                }
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return SmsOutcome.Success(message);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("MSG91 custom send failed: {Status} {Body}", response.StatusCode, body);
            return SmsOutcome.Failure(message, $"MSG91 custom error: {body}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send MSG91 custom message");
            return SmsOutcome.Failure(message, ex.Message);
        }
    }

    private static string ComposeDefaultMessage(CelebrationEventType eventType, string name, string schoolName)
    {
        return eventType switch
        {
            CelebrationEventType.Anniversary => $"Happy Work Anniversary {name}! Thank you from {schoolName}. Reply STOP to opt-out.",
            _ => $"Happy Birthday {name}! Wishing you joy from {schoolName}. Reply STOP to opt-out."
        };
    }

    private static string StripPlus(string phone)
    {
        return phone.StartsWith("+") ? phone[1..] : phone;
    }

    private DateOnly GetCurrentLocalDate()
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _timeZone).DateTime);
    }

    private DateTimeOffset GetCurrentLocalTime()
    {
        var utcNow = _timeProvider.GetUtcNow();
        return TimeZoneInfo.ConvertTime(utcNow, _timeZone);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        var fallbackIds = new[] { "Asia/Kolkata", "India Standard Time" };
        foreach (var id in fallbackIds)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch
            {
                // ignored
            }
        }

        return TimeZoneInfo.Utc;
    }

    private static bool TryNormalizeType(string? value, out CelebrationRecipientType type)
    {
        if (string.Equals(value, "student", StringComparison.OrdinalIgnoreCase))
        {
            type = CelebrationRecipientType.Student;
            return true;
        }

        if (string.Equals(value, "teacher", StringComparison.OrdinalIgnoreCase))
        {
            type = CelebrationRecipientType.Teacher;
            return true;
        }

        type = CelebrationRecipientType.Student;
        return false;
    }

    private static bool TryNormalizePhone(string? value, out string phone, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            phone = string.Empty;
            error = "Phone number is required";
            return false;
        }

        var digits = DigitsOnly.Matches(value)
            .Select(m => m.Value)
            .Aggregate(new StringBuilder(), (sb, part) => sb.Append(part), sb => sb.ToString());

        if (digits.Length == 10)
        {
            phone = $"+91{digits}";
            error = null;
            return true;
        }

        if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
        {
            phone = $"+{digits}";
            error = null;
            return true;
        }

        phone = string.Empty;
        error = "Phone must be +91XXXXXXXXXX";
        return false;
    }

    private static bool TryParseDate(string? value, out DateOnly date, out string? error)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            error = null;
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            date = DateOnly.FromDateTime(dt);
            error = null;
            return true;
        }

        date = default;
        error = "Invalid DOB";
        return false;
    }

    private static bool TryParseAnniversary(string? value, out DateOnly? anniversary, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            anniversary = null;
            error = null;
            return true;
        }

        if (TryParseDate(value, out var date, out error))
        {
            anniversary = date;
            return true;
        }

        error = "Invalid anniversary";
        anniversary = null;
        return false;
    }

    private static bool ParseConsent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Trim() switch
        {
            "0" => false,
            "false" or "FALSE" => false,
            _ => true
        };
    }

    private static List<UploadRecord> ParseCsv(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, leaveOpen: false);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant(),
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);
        var records = new List<UploadRecord>();

        var rowNumber = 0;
        while (csv.Read())
        {
            rowNumber = csv.Parser.RawRow;

            if (csv.Parser.Record?.All(string.IsNullOrWhiteSpace) ?? true)
            {
                continue;
            }

            var record = new UploadRecord
            {
                RowNumber = rowNumber,
                Type = csv.GetField("type"),
                Name = csv.GetField("name"),
                Dob = csv.GetField("dob"),
                Anniversary = csv.GetField("anniversary"),
                Phone = csv.GetField("phone"),
                Consent = csv.GetField("consent")
            };

            records.Add(record);
        }

        return records;
    }

    private static List<UploadRecord> ParseExcel(IFormFile file)
    {
        RegisterExcelEncoding();

        using var stream = file.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var headers = Array.Empty<string>();
        var records = new List<UploadRecord>();
        var rowNumber = 0;

        do
        {
            while (reader.Read())
            {
                rowNumber++;

                if (rowNumber == 1)
                {
                    headers = Enumerable.Range(0, reader.FieldCount)
                        .Select(i => reader.GetValue(i)?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty)
                        .ToArray();
                    continue;
                }

                if (headers.Length == 0 || headers.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                if (Enumerable.Range(0, headers.Length).All(i => string.IsNullOrWhiteSpace(reader.GetValue(i)?.ToString())))
                {
                    continue;
                }

                string? GetValue(string column)
                {
                    var index = Array.FindIndex(headers, h => h == column);
                    if (index < 0 || index >= reader.FieldCount)
                    {
                        return null;
                    }

                    return reader.GetValue(index)?.ToString();
                }

                records.Add(new UploadRecord
                {
                    RowNumber = rowNumber,
                    Type = GetValue("type"),
                    Name = GetValue("name"),
                    Dob = GetValue("dob"),
                    Anniversary = GetValue("anniversary"),
                    Phone = GetValue("phone"),
                    Consent = GetValue("consent")
                });
            }
        } while (reader.NextResult());

        return records;
    }

    private static void RegisterExcelEncoding()
    {
        if (_encodingRegistered)
        {
            return;
        }

        lock (typeof(CelebrationService))
        {
            if (_encodingRegistered)
            {
                return;
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            _encodingRegistered = true;
        }
    }

    private sealed class UploadRecord
    {
        public int RowNumber { get; set; }

        public string? Type { get; set; }

        public string? Name { get; set; }

        public string? Dob { get; set; }

        public string? Anniversary { get; set; }

        public string? Phone { get; set; }

        public string? Consent { get; set; }
    }

    private readonly record struct SmsOutcome(bool Success, string Message, string? Error)
    {
        public static SmsOutcome Success(string message) => new(true, message, null);

        public static SmsOutcome Failure(string message, string? error) => new(false, message, error);
    }
}
