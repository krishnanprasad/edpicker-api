using edpicker_api.Models.Job;

namespace edpicker_api.Services
{
    public class JobBoardRepository : IJobBoardRepository
    {
        private readonly List<JobBoard> _jobs;

        public JobBoardRepository()
        {
            _jobs = new List<JobBoard>
            {
                new JobBoard
                {
                    Id = Guid.NewGuid().ToString(),
                    Board = "TeacherJobs",
                    City = "Chennai",
                    Verified = true,
                    Salary = 30000,
                    UserCity = "Chennai",
                    UpdatedDate = DateTime.UtcNow.AddDays(-1)
                },
                new JobBoard
                {
                    Id = Guid.NewGuid().ToString(),
                    Board = "TeacherJobs",
                    City = "Bangalore",
                    Verified = false,
                    Salary = 28000,
                    UserCity = "Bangalore",
                    UpdatedDate = DateTime.UtcNow.AddDays(-2)
                }
            };
        }

        public Task<IEnumerable<JobBoard>> GetJobBoardsAsync(
            string? city,
            string? board,
            bool? verified,
            decimal? minSalary,
            decimal? maxSalary,
            string? userCity,
            int page,
            int pageSize)
        {
            var query = _jobs.AsQueryable();

            if (!string.IsNullOrEmpty(city))
                query = query.Where(j => j.City.Equals(city, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(board))
                query = query.Where(j => j.Board.Equals(board, StringComparison.OrdinalIgnoreCase));

            if (verified.HasValue)
                query = query.Where(j => j.Verified == verified.Value);

            if (minSalary.HasValue)
                query = query.Where(j => j.Salary >= minSalary.Value);

            if (maxSalary.HasValue)
                query = query.Where(j => j.Salary <= maxSalary.Value);

            if (!string.IsNullOrEmpty(userCity))
                query = query.Where(j => j.UserCity.Equals(userCity, StringComparison.OrdinalIgnoreCase));

            query = query.OrderByDescending(j => j.UpdatedDate);

            var skip = (page - 1) * pageSize;
            var result = query.Skip(skip).Take(pageSize).ToList();
            return Task.FromResult<IEnumerable<JobBoard>>(result);
        }
    }
}
