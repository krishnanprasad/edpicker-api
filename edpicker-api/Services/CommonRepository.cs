using edpicker_api.Models;
using edpicker_api.Models.Dto;
using edpicker_api.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace edpicker_api.Services
{
    public class CommonRepository : ICommonRepository
    {
        private readonly EdPickerDbContext _context;
        public CommonRepository(EdPickerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<BoardDto>> GetBoardsAsync()
        {
            var boards = await _context.Boards
           .FromSqlRaw("EXEC dbo.GetBoards")
           .ToListAsync();
            return boards;
        }

        public async Task<IEnumerable<CityDto>> GetCitiesAsync(int? stateId = null)
        {
            var stateIdParam = stateId.HasValue ? (object)stateId.Value : DBNull.Value;
            var sql = "EXEC dbo.GetCities @StateId = {0}";
            return await _context.Set<CityDto>()
                .FromSqlRaw(sql, stateIdParam)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
