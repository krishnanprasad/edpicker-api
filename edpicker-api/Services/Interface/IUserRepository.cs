using edpicker_api.Models;
using edpicker_api.Models.Dto;
using edpicker_api.Models.Results;

namespace edpicker_api.Services.Interface
{
    public interface IUserRepository
    {
        Task<User?> GetUserByGoogleIdAsync(string googleId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> CreateOrUpdateUserAsync(User user);
        Task<IEnumerable<UserJobApplicationDto>> GetUserJobApplicationsAsync(int userId);
        Task<ApplyForJobResult?> WithdrawlOfJobAsync(int userId, int jobId, string notes, string createdBy);
    }
}
