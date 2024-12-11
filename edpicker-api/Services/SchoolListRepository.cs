using System.ComponentModel;
using System.Text;
using edpicker_api.Models;
using Microsoft.Azure.Cosmos;

namespace edpicker_api.Services
{
    public class SchoolListRepository : ISchoolListRepository
    {
        private readonly Microsoft.Azure.Cosmos.Container _container;
        public SchoolListRepository(string conn,string key,string dbName,string containerName)
        {
            var cosmoClient = new CosmosClient(conn, key, new CosmosClientOptions() { });
            _container = cosmoClient.GetContainer(dbName, containerName);
        }

        public Task<School> GetSchoolAllAsync(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<School>> GetSchoolAsync(int schoolType, string? id, string? board, string? state, string? city, string? location)
        {
            var queryBuilder = new StringBuilder("SELECT * FROM c WHERE c.schooltype = @schoolType");

            if (!string.IsNullOrEmpty(board))
            {
                queryBuilder.Append(" AND c.board = @board");
            }

            if (!string.IsNullOrEmpty(city))
            {
                queryBuilder.Append(" AND c.city = @city");
            }

            if (!string.IsNullOrEmpty(state))
            {
                queryBuilder.Append(" AND c.state = @state");
            }

            if (!string.IsNullOrEmpty(location))
            {
                queryBuilder.Append(" AND c.location = @location");
            }

            var queryDefinition = new QueryDefinition(queryBuilder.ToString())
                .WithParameter("@schoolType", schoolType);

            if (!string.IsNullOrEmpty(board))
            {
                queryDefinition.WithParameter("@board", board);
            }

            if (!string.IsNullOrEmpty(city))
            {
                queryDefinition.WithParameter("@city", city);
            }

            if (!string.IsNullOrEmpty(state))
            {
                queryDefinition.WithParameter("@state", state);
            }

            if (!string.IsNullOrEmpty(location))
            {
                queryDefinition.WithParameter("@location", location);
            }

            var query = _container.GetItemQueryIterator<School>(queryDefinition);
            var results = new List<School>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<IEnumerable<dynamic>> GetSchoolListAsync(int schoolType, string? id, string? board, string? state, string? city, string? location)
        {
            try
            {
                var queryBuilder = new StringBuilder("SELECT c.id, c.schoolname, c.address, c.fees, c.city, c.board from c WHERE c.schooltype = @schoolType");

                if (!string.IsNullOrEmpty(board))
                {
                    var boardValues = board.Split(',').Select((b, index) => $"@board{index}").ToArray();
                    queryBuilder.Append($" AND c.board IN ({string.Join(", ", boardValues)})");
                }

                if (!string.IsNullOrEmpty(city))
                {
                    queryBuilder.Append(" AND c.city = @city");
                }

                if (!string.IsNullOrEmpty(state))
                {
                    queryBuilder.Append(" AND c.state = @state");
                }

                if (!string.IsNullOrEmpty(location))
                {
                    queryBuilder.Append(" AND c.location = @location");
                }

                var queryDefinition = new QueryDefinition(queryBuilder.ToString())
                    .WithParameter("@schoolType", schoolType);

                if (!string.IsNullOrEmpty(board))
                {
                    var boardValues = board.Split(',');
                    for (int i = 0; i < boardValues.Length; i++)
                    {
                        queryDefinition.WithParameter($"@board{i}", boardValues[i].Trim());
                    }
                }

                if (!string.IsNullOrEmpty(city))
                {
                    queryDefinition.WithParameter("@city", city);
                }

                if (!string.IsNullOrEmpty(state))
                {
                    queryDefinition.WithParameter("@state", state);
                }

                if (!string.IsNullOrEmpty(location))
                {
                    queryDefinition.WithParameter("@location", location);
                }

                var query = _container.GetItemQueryIterator<School>(queryDefinition);
                var results = new List<School>();

                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                return results;
            }
            catch (CosmosException ex)
            {
                // Log the Cosmos DB error
                Console.WriteLine($"Cosmos DB error occurred: {ex.StatusCode} - {ex.Message}");
                throw; // Optionally rethrow the exception or handle it appropriately
            }
            catch (Exception ex)
            {
                // Log general errors
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw; // Optionally rethrow the exception or handle it appropriately
            }
        }

        public Task<IEnumerable<School>> GetSchools()
        {
            throw new NotImplementedException();
        }

        Task<School> ISchoolListRepository.UpdateSchool(School school)
        {
            throw new NotImplementedException();
        }

        public async Task<School> GetSchoolDetailAsync(string id)
        {
            try
            {
                // Query the document to retrieve the partition key value
                var query = _container.GetItemQueryIterator<School>(
                    new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", id)
                );

                var results = new List<School>();
                while (query.HasMoreResults)
                {
                    var response2 = await query.ReadNextAsync();
                    results.AddRange(response2.ToList());
                }

                var school = results.FirstOrDefault();
                if (school == null)
                {
                    return null; // Document not found
                }

                return school;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Document not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }
    }
}
