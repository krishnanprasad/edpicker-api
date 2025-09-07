using System.Data;
using edpicker_api.Models;
using edpicker_api.Models.Dto;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace edpicker_api.Services.Interface
{
    public interface ISchoolAccountRepository
    {
        Task<SchoolAccounts?> LoginAsync(string email, string password);
        Task<(bool Success, int SchoolId, int SchoolAccountId, string Error)> CreateAsync(SchoolAccountDto dto);
        Task<bool> UpdatePasswordAsync(string email, string oldPassword, string newPassword);
        Task<bool> UpdateDetailsAsync(UpdateDetailsDto dto);
        bool IsPasswordValid(string password);
        Task<int> AddJobAsync(CreateJobRequest job, string createdBy);
        Task<School_JobApplicationsWithCountsDto> GetJobApplicationsBySchoolAndJobAsync(int schoolId, int jobId);
        Task<List<School_JobListDto>> GetJobsBySchoolIdAsync(int schoolId, int pageNumber = 1, int pageSize = 50);
        Task<bool> UpdateSchoolAccountAsync(int schoolId, School_UpdateSchoolAccountDto dto);
        Task<School_GetProfileDto?> GetProfileBySchoolIdAsync(int schoolId);
        Task<School_ChangePasswordResultDto> ChangePasswordAsync(int schoolId, School_ChangePasswordDto Password);


    }
}
