using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using edpicker_api.Models;
using edpicker_api.Models.Dto;
using edpicker_api.Models.Job;
using edpicker_api.Models.Results;
using edpicker_api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

public class JobBoardRepository : IJobBoardRepository
{
    private readonly EdPickerDbContext _context;

    public JobBoardRepository(EdPickerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<JobBoard>> GetAllAsync()
    {
        return await _context.JobBoard
            .Include(j => j.School)
            .Include(j => j.ContactDetails)
            .Where(j => !j.IsDeleted)
            .ToListAsync();
    }

    public async Task<JobBoard> GetByIdAsync(int id)
    {
        return await _context.JobBoard
            .Include(j => j.School)
            .Include(j => j.ContactDetails)
            .FirstOrDefaultAsync(j => j.JobBoardId == id && !j.IsDeleted);
    }

    public async Task AddAsync(JobBoard jobBoard)
    {
        _context.JobBoard.Add(jobBoard);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(JobBoard jobBoard)
    {
        _context.JobBoard.Update(jobBoard);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var jobBoard = await _context.JobBoard.FindAsync(id);
        if (jobBoard != null)
        {
            jobBoard.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    // Fix for CS0535: Implement missing interface member
    public async Task<IEnumerable<SearchJobDto>> SearchJobBoardsAsync(
    int? cityId,
    int? boardId,
    bool? isVerified,
    decimal? minSalary,
    decimal? maxSalary,
    int? minExperience,
    int? maxExperience,
    decimal? userLatitude,    // NEW
    decimal? userLongitude,   // NEW
    int pageNumber,
    int pageSize)
    {
        var cityIdParam = cityId.HasValue ? (object)cityId.Value : DBNull.Value;
        var boardIdParam = boardId.HasValue ? (object)boardId.Value : DBNull.Value;
        var verifiedParam = isVerified.HasValue ? (object)isVerified.Value : DBNull.Value;
        var minSalaryParam = minSalary.HasValue ? (object)minSalary.Value : DBNull.Value;
        var maxSalaryParam = maxSalary.HasValue ? (object)maxSalary.Value : DBNull.Value;
        var minExpParam = minExperience.HasValue ? (object)minExperience.Value : DBNull.Value;
        var maxExpParam = maxExperience.HasValue ? (object)maxExperience.Value : DBNull.Value;
        var userLatParam = userLatitude.HasValue ? (object)userLatitude.Value : DBNull.Value;
        var userLonParam = userLongitude.HasValue ? (object)userLongitude.Value : DBNull.Value;
        var pageNumberParam = pageNumber;
        var pageSizeParam = pageSize;

        var sql = @"
        EXEC dbo.SearchJobBoards
            @CityId        = {0},
            @BoardId       = {1},
            @IsVerified    = {2},
            @MinSalary     = {3},
            @MaxSalary     = {4},
            @MinExperience = {5},
            @MaxExperience = {6},
            @UserLatitude  = {7},
            @UserLongitude = {8},
            @PageNumber    = {9},
            @PageSize      = {10}";

        var result = await _context.SearchJobResults
            .FromSqlRaw(sql,
                cityIdParam,
                boardIdParam,
                verifiedParam,
                minSalaryParam,
                maxSalaryParam,
                minExpParam,
                maxExpParam,
                userLatParam,
                userLonParam,
                pageNumberParam,
                pageSizeParam)
            .AsNoTracking()
            .ToListAsync();

        return result;
    }

    public async Task<JobBoardDetailsDto?> GetJobBoardDetailsByIdAsync(int id)
    {
        return _context.JobBoardDetails
       .FromSqlRaw("EXEC dbo.GetJobBoardDetailsById @JobBoardId = {0}", id)
       .AsNoTracking()
       .AsEnumerable()          // pull results into memory
       .FirstOrDefault();       // now you can LINQ on the client


    }

    public async Task<ApplyForJobResult?> ApplyForJobAsync(int userId, int jobId, string notes, string createdBy)
    {
        try { 
        // Build SQL and parameters
        var sql = "EXEC dbo.ApplyForJob @JobId = {0}, @UserId = {1}, @Notes = {2}, @CreatedBy = {3}";
        var results = await _context.Set<ApplyForJobResult>()
            .FromSqlRaw(sql, jobId, userId, notes ?? (object)DBNull.Value, createdBy)
            .ToListAsync();

        return results.FirstOrDefault() ?? new ApplyForJobResult { ResultCode = 0, Message = "Unknown error." };
        }
        catch (Exception E)
        {
            throw E;
        }
    }

}
