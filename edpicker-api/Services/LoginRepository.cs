using System.Data;
using Dapper;
using edpicker_api.Models;
using edpicker_api.Models.Dto;
using edpicker_api.Services.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using BCryptNet = BCrypt.Net.BCrypt;

namespace edpicker_api.Services
{
    public class LoginRepository : ILoginRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IJwtTokenService _jwtTokenService;

        public LoginRepository(IConfiguration configuration, IJwtTokenService jwtTokenService)
        {
            _configuration = configuration;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            try
            {
                using IDbConnection db = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                var parameters = new { SchoolCode = request.SchoolCode, UserId = request.Username, Password = request.Password };
                var user = await db.QueryFirstOrDefaultAsync<UserCredential>(
                    "dbo.User_LoginValidate",
                    parameters,
                    commandType: CommandType.StoredProcedure);
                if (user == null)
                    return null;
                if (user.IsDeleted || user.ExpiryDateUtc <= DateTime.UtcNow)
                    return null;
                if (!BCryptNet.Verify(request.Password, user.PasswordHash))
                    return null;
                var token = _jwtTokenService.GenerateToken(user.UserId, user.SchoolId, user.SchoolName);
                return new LoginResponseDto
                {
                    UserId = user.UserId,
                    SchoolId = user.SchoolId,
                    SchoolName = user.SchoolName,
                    Token = token,
                    ExpiryDate = user.ExpiryDateUtc
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
