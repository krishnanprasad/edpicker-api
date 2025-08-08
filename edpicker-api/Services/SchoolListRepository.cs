using System.ComponentModel;
using System.Text;
using edpicker_api.Models;
using Microsoft.Azure.Cosmos;

namespace edpicker_api.Services
{
    public class SchoolListRepository : ISchoolListRepository
    {
        private readonly Microsoft.Azure.Cosmos.Container _container;
        public SchoolListRepository(string conn, string key, string dbName, string containerName)
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
                // Step 1: Query the document to retrieve the school details, including partition keys
                var query = _container.GetItemQueryIterator<School>(
                    new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", id)
                );

                var results = new List<School>();
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                School school = results.FirstOrDefault();
                if (school == null)
                {
                    return null; // Document not found
                }
                bool doesItHasIncrement = school.Viewcount != 0;
                // Step 2: Call IncrementViewCountAsync with all partition key values
                await IncrementViewCountAsync(school.Id, school.SchoolType.ToString(), school.City, school.Board, doesItHasIncrement);

                // Step 3: Return the school details
                return school;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Document not found: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }

        }


        public async Task IncrementViewCountAsync(string id, string schoolType, string city, string board, bool doesItHasIncrement)
        {
            try
            {
                var patchOperations = new List<PatchOperation>();

                if (doesItHasIncrement)
                {
                    // If viewcount exists, increment it by 1
                    patchOperations.Add(PatchOperation.Increment("/viewcount", 1));
                }
                else
                {
                    // If viewcount does not exist, set it to 1
                    patchOperations.Add(PatchOperation.Set("/viewcount", 1));
                }


                PartitionKeyBuilder pkBuilder = new PartitionKeyBuilder();
                pkBuilder.Add(Convert.ToInt16(schoolType));              // schooltype (int)
                pkBuilder.Add(city);   // city (string)
                pkBuilder.Add(board);            // board (string)
                PartitionKey partitionKeyValue = pkBuilder.Build();


                // Perform the patch operation
                var response = await _container.PatchItemAsync<object>(
                    id,
                    partitionKeyValue,
                    patchOperations
                );

                // Log success
                Console.WriteLine($"Document updated successfully. Status Code: {response.StatusCode}");
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Cosmos DB error: {ex.StatusCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }

    }
}
