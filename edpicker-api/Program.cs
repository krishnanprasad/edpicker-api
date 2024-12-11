using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using edpicker_api.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Specify allowed origin (Angular app)
              .AllowAnyHeader() // Allow all headers
              .AllowAnyMethod(); // Allow all HTTP methods
    });
});
string vaultUrl = builder.Configuration["CosmoConfig:vault-url"];
var client = new SecretClient(vaultUri: new Uri(vaultUrl), credential: new DefaultAzureCredential());

KeyVaultSecret secret = client.GetSecret("edpicker-cosmo-primarykey");
builder.Services.AddScoped<ISchoolListRepository, SchoolListRepository>(
    x => new SchoolListRepository(builder.Configuration.GetConnectionString("cosmoDb"),
    builder.Configuration[secret.Value],
    builder.Configuration["CosmoConfig:databaseName"],
    builder.Configuration["CosmoConfig:containerName"]
    ));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowSpecificOrigins"); 
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
