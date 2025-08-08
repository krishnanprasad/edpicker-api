using System.Linq.Expressions;
using edpicker_api.Services;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace edpicker_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommonController : ControllerBase
    {
        private readonly ICommonRepository _repo;
        public CommonController(ICommonRepository repo) => _repo = repo;
        [HttpGet("getcity")]
        [HttpGet]
        public async Task<IActionResult> GetCities([FromQuery] int? stateId = null)
        {
            try
            {
                var cities = await _repo.GetCitiesAsync(stateId);
                return Ok(cities);

            }
            catch( Exception E){
                return StatusCode(500, "Internal Server Error");
            }
        }
        [HttpGet("boards")]
        public async Task<IActionResult> GetBoards()
        {
            try
            {
                var boards = await _repo.GetBoardsAsync();
                return Ok(boards);
            }
            catch (Exception ex)
            {
                // Log the exception as needed
                return StatusCode(500, "An error occurred while loading boards.");
            }
        }
    }
}
