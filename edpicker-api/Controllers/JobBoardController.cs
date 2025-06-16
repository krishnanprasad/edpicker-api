using Microsoft.AspNetCore.Mvc;
using edpicker_api.Services;
using edpicker_api.Models.Job;

namespace edpicker_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobBoardController : ControllerBase
    {
        private readonly IJobBoardRepository _repo;

        public JobBoardController(IJobBoardRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetJobBoards(
            string? city,
            string? board,
            bool? verified,
            decimal? minSalary,
            decimal? maxSalary,
            string? userCity,
            int page = 1,
            int pageSize = 10)
        {
            var jobs = await _repo.GetJobBoardsAsync(city, board, verified, minSalary, maxSalary, userCity, page, pageSize);
            return Ok(jobs);
        }
    }
}
