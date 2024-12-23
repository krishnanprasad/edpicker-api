using edpicker_api.Models;
using edpicker_api.Services.Interface;
using Microsoft.Azure.Cosmos;

namespace edpicker_api.Services
{
    public class RegistrationRepository:IRegistrationRepository
    {
        private readonly Microsoft.Azure.Cosmos.Container _container;
        public RegistrationRepository(string conn, string key, string dbName, string containerName)
        {
            var cosmoClient = new CosmosClient(conn, key, new CosmosClientOptions() { });
            _container = cosmoClient.GetContainer(dbName, containerName);
        }
        public async Task<bool> SaveRegistrationAsync(Registration_Post registration)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(registration.Name))
                throw new ArgumentException("Name cannot be empty.");
            if (string.IsNullOrWhiteSpace(registration.PhoneNumber))
                throw new ArgumentException("Phone number cannot be empty.");
            if (registration.MessageType == registration.MessageType && string.IsNullOrWhiteSpace(registration.SchoolId))
                throw new ArgumentException("SchoolId is required for School Registration.");
            Registration registration_add = new Registration();
            // Set properties
            registration_add.Id = Guid.NewGuid().ToString(); // Unique ID
            registration_add.StatusId = GenerateStatusId();  // 16-digit unique Status ID
            registration_add.CreatedDate = DateTime.Now.ToString(); // ISO 8601 timestamp
            registration_add.UpdatedDate =DateTime.Now.ToString();
            registration_add.Status = "1"; // Default status to "1"

            try
            {
                // Save to Cosmos DB
                await _container.CreateItemAsync(registration, new PartitionKey(registration_add.StatusId));
                return true; // Successful save
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Cosmos DB error: {ex.Message}");
                throw;
            }
        }

        private string GenerateStatusId()
        {
            // Use current timestamp and random generator for 16-digit unique number
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Random random = new Random();
            return $"{timestamp}{random.Next(1000, 9999)}".Substring(0, 16);
        }
    }
}
