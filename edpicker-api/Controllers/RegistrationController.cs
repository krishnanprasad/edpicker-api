using edpicker_api.Models;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace edpicker_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly IRegistrationRepository _registrationRepository;

        public RegistrationController(IRegistrationRepository registrationRepository)
        {
            _registrationRepository = registrationRepository;
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveRegistration([FromBody] Registration_Post registration)
        {
            try
            {
                await _registrationRepository.SaveRegistrationAsync(registration);
                return Ok(new { message = "Registration saved successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while saving registration.", details = ex.Message });
            }
        }
    }
}
