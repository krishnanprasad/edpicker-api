using edpicker_api.Models;

namespace edpicker_api.Services.Interface
{
    public interface IJwtTokenService
    {
        string GenerateToken(SchoolAccounts user);
        string GenerateToken(User user);
        string GenerateToken(string userId, int schoolId, string schoolName);
        string GenerateRefreshToken();
    }
}
