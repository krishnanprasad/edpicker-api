using System;
using System.Threading;
using System.Threading.Tasks;
using edpicker_api.Models.Celebration.Dtos;
using Microsoft.AspNetCore.Http;

namespace edpicker_api.Services.Interface;

public interface ICelebrationService
{
    Task<UploadProfilesResult> UploadProfilesAsync(int adminId, IFormFile file, CancellationToken cancellationToken);

    Task<StudentSummaryDto> UpsertStudentAsync(int adminId, StudentUpsertRequest request, CancellationToken cancellationToken);

    Task<TeacherEventDto> UpsertTeacherAsync(int adminId, TeacherUpsertRequest request, CancellationToken cancellationToken);

    Task<TodayDashboardResponse> GetTodayDashboardAsync(int adminId, CancellationToken cancellationToken);

    Task<HistoryResponse> GetHistoryAsync(int adminId, DateOnly date, CancellationToken cancellationToken);

    Task<SchoolConfigResponse> SetSchoolNameAsync(int adminId, SchoolConfigRequest request, CancellationToken cancellationToken);

    Task<SmsSendResultDto> SendTestSmsAsync(int adminId, SmsTestRequest request, CancellationToken cancellationToken);

    Task<OptOutResponse> HandleOptOutAsync(OptOutRequest request, CancellationToken cancellationToken);

    Task SendDailyRemindersAsync(CancellationToken cancellationToken);
}
