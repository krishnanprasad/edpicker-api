using edpicker_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace edpicker_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchoolController : Controller
    {
        private readonly ISchoolListRepository _schoolListRepository;
        public SchoolController(ISchoolListRepository schoolListRepository)
        {
            _schoolListRepository = schoolListRepository;
        }
        [HttpGet, Route("getschool")]
        public async Task<IActionResult> GetSchool(int schooltype, string board, string city)
        {
            var results = await _schoolListRepository.GetSchoolAsync(schooltype, "", board, "", city, "");
            return Ok(results);
        }
        [HttpGet, Route("getschoolsearch")]
        public async Task<IActionResult> GetSchoolList(int schooltype, string board, string city)
        {
            var results = await _schoolListRepository.GetSchoolListAsync(schooltype, "", board, "", city, "");
            return Ok(results);
        }
        [HttpGet, Route("getschooldetail")]
        public async Task<IActionResult> GetSchoolDetail(string id)
        {
            var school = await _schoolListRepository.GetSchoolDetailAsync(id);
            if (school == null)
            {
                return NotFound(new { message = "School not found" });
            }
            return Ok(school);
        }

    }
}
