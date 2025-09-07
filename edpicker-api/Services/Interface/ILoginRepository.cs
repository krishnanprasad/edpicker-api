using edpicker_api.Models.Dto;

namespace edpicker_api.Services.Interface
{
    public interface ILoginRepository
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
    }
}
