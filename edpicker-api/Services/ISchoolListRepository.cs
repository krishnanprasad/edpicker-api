using edpicker_api.Models;

namespace edpicker_api.Services
{
    public interface ISchoolListRepository
    {
        public Task<IEnumerable<School>> GetSchools();
        public Task<IEnumerable<School>> GetSchoolAsync(int schoolType, string? id, string? board, string? state, string? city, string? location);
        public Task<IEnumerable<dynamic>> GetSchoolListAsync(int schoolType, string? id, string? board, string? state, string? city, string? location);
        Task<School> GetSchoolAllAsync(string id);
        Task<School> UpdateSchool(School school);
        Task<School> GetSchoolDetailAsync(string id);
        Task IncrementViewCountAsync(string id, string schoolType, string city, string board);
    }
}
