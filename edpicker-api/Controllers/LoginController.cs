using edpicker_api.Models.Dto;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace edpicker_api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginRepository _loginRepository;

        public LoginController(ILoginRepository loginRepository)
        {
            _loginRepository = loginRepository;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _loginRepository.LoginAsync(request);
                if (result == null)
                    return Unauthorized();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
