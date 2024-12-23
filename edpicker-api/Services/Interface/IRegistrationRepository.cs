using edpicker_api.Models;

namespace edpicker_api.Services.Interface
{
    public interface IRegistrationRepository
    {
        Task<bool> SaveRegistrationAsync(Registration_Post registration);
    }
}
