using edpicker_api.Models.Dto;

namespace edpicker_api.Services.Interface
{
    public interface ICommonRepository
    {
        Task<IEnumerable<CityDto>> GetCitiesAsync(int? stateId = null);
        Task<IEnumerable<BoardDto>> GetBoardsAsync();
    }
}
