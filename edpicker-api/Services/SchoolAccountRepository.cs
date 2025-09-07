using System.Data;
using edpicker_api.Models;
using edpicker_api.Models.Dto;
using edpicker_api.Models.Job;
using edpicker_api.Services.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static SchoolAccountRepository;

public class SchoolAccountRepository : ISchoolAccountRepository
{
    private readonly EdPickerDbContext _context;
    public SchoolAccountRepository(EdPickerDbContext context) { _context = context; }

    public async Task<SchoolAccounts?> LoginAsync(string email, string password)
    {
        try { 
        var results = await _context.SchoolAccounts
            .FromSqlRaw("EXEC LoginCheck @SchoolEmail, @SchoolPassword",
                new SqlParameter("@SchoolEmail", email),
                new SqlParameter("@SchoolPassword", password))
            .AsNoTracking()
            .ToListAsync();

        // Now safely use LINQ on the result
        return results.FirstOrDefault();
        }
        catch (Exception E)
        {
            throw E;
        }
    }

    public async Task<(bool Success, int SchoolId, int SchoolAccountId, string Error)> CreateAsync(SchoolAccountDto dto)
    {
        try
        {
            var schoolIdParam = new SqlParameter("@SchoolId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            var schoolAccountIdParam = new SqlParameter("@SchoolAccountId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC CreateFullSchool @Email, @Phone, @Password, @CityId, @StateId, @BoardId, @SchoolName, @SchoolId OUTPUT, @SchoolAccountId OUTPUT",
                new SqlParameter("@Email", dto.Email),
                new SqlParameter("@Phone", dto.Phone),
                new SqlParameter("@Password", dto.Password),
                new SqlParameter("@CityId", dto.CityId),
                new SqlParameter("@StateId", dto.StateId),
                new SqlParameter("@BoardId", dto.BoardId),
                new SqlParameter("@SchoolName", (object?)dto.SchoolName ?? DBNull.Value),
                schoolIdParam,
                schoolAccountIdParam
            );

            int schoolId = (int)(schoolIdParam.Value ?? 0);
            int schoolAccountId = (int)(schoolAccountIdParam.Value ?? 0);

            return (true, schoolId, schoolAccountId, null);
        }
        catch
        {
            return (true, 0, 0, null); ; // e.g., email already exists
        }
    }

    public async Task<bool> UpdatePasswordAsync(string email, string oldPassword, string newPassword)
    {
        var rows = await _context.Database.ExecuteSqlRawAsync(
            "EXEC UpdateSchoolPassword @SchoolEmail, @OldPassword, @NewPassword",
            new SqlParameter("@SchoolEmail", email),
            new SqlParameter("@OldPassword", oldPassword),
            new SqlParameter("@NewPassword", newPassword)
        );
        return rows > 0;
    }

    public async Task<bool> UpdateDetailsAsync(UpdateDetailsDto dto)
    {
        var rows = await _context.Database.ExecuteSqlRawAsync(
            "EXEC UpdateSchoolDetails @SchoolId, @SchoolName, @SchoolAddress, @SchoolContactName, @SchoolPincode, @SchoolContactNumber1, @SchoolContactNumber2",
            new SqlParameter("@SchoolId", dto.SchoolId),
            new SqlParameter("@SchoolName", dto.SchoolName),
            new SqlParameter("@SchoolAddress", (object)dto.SchoolAddress ?? DBNull.Value),
            new SqlParameter("@SchoolContactName", (object)dto.SchoolContactName ?? DBNull.Value),
            new SqlParameter("@SchoolPincode", dto.SchoolPincode),
            new SqlParameter("@SchoolContactNumber1", dto.SchoolContactNumber1),
            new SqlParameter("@SchoolContactNumber2", (object)dto.SchoolContactNumber2 ?? DBNull.Value)
        );
        return rows > 0;
    }

    public bool IsPasswordValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;
        return password.Any(char.IsLetter) && password.Any(char.IsDigit);
    }
    public async Task<int> AddJobAsync(CreateJobRequest dto, string createdBy)
    {
        try
        {


            using (var conn = _context.Database.GetDbConnection())
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "dbo.AddJob";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@SchoolId", dto.SchoolId));
                    cmd.Parameters.Add(new SqlParameter("@Title", dto.Title));
                    cmd.Parameters.Add(new SqlParameter("@Description", dto.Description));
                    cmd.Parameters.Add(new SqlParameter("@MinExperience", (object)dto.MinExperience ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@MaxExperience", (object)dto.MaxExperience ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@ContactId", (object)dto.ContactId ?? (object)dto.SchoolId));
                    cmd.Parameters.Add(new SqlParameter("@MinSalary", (object)dto.MinSalary ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@MaxSalary", (object)dto.MaxSalary ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@IsVerified", dto.IsVerified));
                    cmd.Parameters.Add(new SqlParameter("@JobBoardStatusId", dto.JobBoardStatusId ?? (object)DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@CreatedBy", createdBy));
                    cmd.Parameters.Add(new SqlParameter("@Benefits", (object)dto.Benefits ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@Expectation", (object)dto.Expectation ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@Education", (object)dto.Education ?? DBNull.Value));

                    // Output parameter for JobId
                    var jobIdParam = new SqlParameter("@JobBoardId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(jobIdParam);

                    await cmd.ExecuteNonQueryAsync();

                    return (int)jobIdParam.Value;
                }
            }
        }
        catch (Exception Ex)
        {
            Console.WriteLine(Ex);
            return 0;
        }
    }
    public async Task<School_JobApplicationsWithCountsDto> GetJobApplicationsBySchoolAndJobAsync(int schoolId, int jobId)
    {
        var result = new School_JobApplicationsWithCountsDto();

        using var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.GetJobApplicationsBySchoolAndJob";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@SchoolId", schoolId));
        cmd.Parameters.Add(new SqlParameter("@JobId", jobId));

        using var reader = await cmd.ExecuteReaderAsync();

        // Read applications
        var applications = new List<School_JobApplicationDto>();
        while (await reader.ReadAsync())
        {
            applications.Add(new School_JobApplicationDto
            {
                ApplicationId = reader.GetInt32(0),
                JobId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                AppliedDate = reader.GetDateTime(3),
                ApplicationStatusId = reader.GetInt32(4),
                ApplicationStatus = reader.GetString(5),
                StatusDate = reader.GetDateTime(6),
                Notes = reader.IsDBNull(7) ? null : reader.GetString(7),
                LastModifiedDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                LastModifiedBy = reader.IsDBNull(9) ? null : reader.GetString(9),
                FullName = reader.IsDBNull(10) ? null : reader.GetString(10),
                ResumeUrl = reader.IsDBNull(11) ? null : reader.GetString(11),
                Email = reader.IsDBNull(12) ? null : reader.GetString(12)
            });
        }

        // Next result: Status counts
        if (await reader.NextResultAsync())
        {
            var statusCounts = new List<School_ApplicationStatusCountDto>();
            while (await reader.ReadAsync())
            {
                statusCounts.Add(new School_ApplicationStatusCountDto
                {
                    ApplicationStatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1),
                    StatusCount = reader.GetInt32(2)
                });
            }
            result.StatusCounts = statusCounts;
        }

        result.Applications = applications;
        return result;
    }

    public async Task<List<School_JobListDto>> GetJobsBySchoolIdAsync(int schoolId, int pageNumber = 1, int pageSize = 50)
    {
        var schoolIdParam = new SqlParameter("@SchoolId", schoolId);
        var pageNumberParam = new SqlParameter("@PageNumber", pageNumber);
        var pageSizeParam = new SqlParameter("@PageSize", pageSize);

        var result = await _context.Set<School_JobListDto>().FromSqlRaw(
            "EXEC [dbo].[GetJobsBySchoolId] @SchoolId, @PageNumber, @PageSize",
            schoolIdParam, pageNumberParam, pageSizeParam
        ).AsNoTracking().ToListAsync();

        return result;
    }
    public async Task<bool> UpdateSchoolAccountAsync(int schoolId, School_UpdateSchoolAccountDto dto)
    {
        try
        {
            var result = await _context.Database.ExecuteSqlRawAsync(
                @"EXEC dbo.UpdateSchoolAccount 
                    @SchoolId, 
                    @ContactName, 
                    @ContactAddress, 
                    @ContactPincode, 
                    @PrimaryPhone, 
                    @SecondaryPhone, 
                    @SchoolName",
                new SqlParameter("@SchoolId", schoolId),
                new SqlParameter("@ContactName", (object?)dto.ContactName ?? DBNull.Value),
                new SqlParameter("@ContactAddress", (object?)dto.ContactAddress ?? DBNull.Value),
                new SqlParameter("@ContactPincode", dto.ContactPincode),
                new SqlParameter("@PrimaryPhone", dto.PrimaryPhone),
                new SqlParameter("@SecondaryPhone", (object?)dto.SecondaryPhone ?? DBNull.Value),
                new SqlParameter("@SchoolName", (object?)dto.SchoolName ?? DBNull.Value)
            );
            return true;
        }
        catch (Exception ex)
        {
            // log the error (use ILogger in production)
            throw new Exception("Failed to update school account.", ex);
        }
    }
    public async Task<School_GetProfileDto?> GetProfileBySchoolIdAsync(int schoolId)
    {
        try
        {
            var param = new SqlParameter("@SchoolId", schoolId);
            var results = await _context.Set<School_GetProfileDto>()
     .FromSqlRaw("EXEC dbo.GetSchoolProfileById @SchoolId", param)
     .AsNoTracking()
     .ToListAsync();

            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            // Log error if needed
            throw new Exception("Database error when loading profile.", ex);
        }
    }
    public async Task<School_ChangePasswordResultDto> ChangePasswordAsync(
     int schoolId,
     School_ChangePasswordDto dto
 )
    {
        try
        {
            using (var conn = _context.Database.GetDbConnection())
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "dbo.ChangeSchoolPassword";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@SchoolAccountId", schoolId));
                    cmd.Parameters.Add(new SqlParameter("@OldPassword", dto.OldPassword));
                    cmd.Parameters.Add(new SqlParameter("@NewPassword", dto.NewPassword));

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var result = new School_ChangePasswordResultDto
                        {
                            ResultCode = 0,
                            Message = "Unknown error."
                        };

                        if (await reader.ReadAsync())
                        {
                            result.ResultCode = reader.GetInt32(0);
                            result.Message = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                        }

                        return result;
                    }
                }
            }
        }
        catch (SqlException ex)
        {
            // log ex if you have ILogger
            return new School_ChangePasswordResultDto
            {
                ResultCode = 0,
                Message = "Database error while changing password."
            };
        }
        catch (Exception)
        {
            return new School_ChangePasswordResultDto
            {
                ResultCode = 0,
                Message = "Unexpected error while changing password."
            };
        }
    }
}
