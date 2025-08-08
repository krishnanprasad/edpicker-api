using edpicker_api.Models.Dto;
using edpicker_api.Models.Job;
using edpicker_api.Models.Results;

namespace edpicker_api.Services
{
    public interface IJobBoardRepository
    {
        Task<IEnumerable<SearchJobDto>> SearchJobBoardsAsync(
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
    int pageSize);

        Task<JobBoardDetailsDto?> GetJobBoardDetailsByIdAsync(int id);

        Task<ApplyForJobResult?> ApplyForJobAsync(int userId, int jobId, string notes, string createdBy);

    }
}
