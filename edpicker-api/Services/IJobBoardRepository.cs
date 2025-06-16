using edpicker_api.Models.Job;

namespace edpicker_api.Services
{
    public interface IJobBoardRepository
    {
        Task<IEnumerable<JobBoard>> GetJobBoardsAsync(
            string? city,
            string? board,
            bool? verified,
            decimal? minSalary,
            decimal? maxSalary,
            string? userCity,
            int page,
            int pageSize);
    }
}
