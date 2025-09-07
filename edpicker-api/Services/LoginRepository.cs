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

                var validation = await db.QueryFirstOrDefaultAsync<LoginValidationResult>(
                    "dbo.Auth_ValidateLogin",
                    new { UserName = request.Username, Password = request.Password },
                    commandType: CommandType.StoredProcedure);

                if (validation == null || !validation.Success || validation.Reason == "PASSWORD_EXPIRED")
                    return null;

                var user = await db.QueryFirstOrDefaultAsync<UserCredential>(
                    @"SELECT u.UserId, u.UserName, u.SchoolId, u.PasswordExpiresAtUtc AS ExpiryDateUtc, s.Name AS SchoolName
                      FROM dbo.AppUser u
                      JOIN dbo.School s ON s.SchoolId = u.SchoolId
                      WHERE u.UserId = @UserId",
                    new { UserId = validation.UserId });

                if (user == null)
                    return null;

                if (user.ExpiryDateUtc.HasValue && user.ExpiryDateUtc.Value <= DateTime.UtcNow)
                    return null;

                var token = _jwtTokenService.GenerateToken(user.UserName, user.SchoolId, user.SchoolName);

                return new LoginResponseDto
                {
                    UserId = user.UserName,
                    SchoolId = user.SchoolId,
                    SchoolName = user.SchoolName,
                    Token = token,
                    ExpiryDate = user.ExpiryDateUtc ?? DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
