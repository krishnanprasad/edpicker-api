using Azure.Identity;
using edpicker_api.Services;
using edpicker_api.Services.Interface;

var builder = WebApplication.CreateBuilder(args);

string openAiApiKey = "OpenAIKey";
builder.Configuration.AddAzureKeyVault(
    new Uri("https://edpicker-vault.vault.azure.net/"),
    new DefaultAzureCredential()
);
builder.Services.AddSingleton(new OpenAI.OpenAIClient(openAiApiKey));

// Add services to the container.
builder.Services.AddSingleton(new OpenAI.OpenAIClient(openAiApiKey));
builder.Services.AddSingleton<IJobBoardRepository, JobBoardRepository>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationInsightsTelemetry();
// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://edpicker-gkg3hndqb4feeqdt.eastasia-01.azurewebsites.net", "https://edpicker.in") // Specify allowed origin (Angular app)
              .AllowAnyHeader() // Allow all headers
              .AllowAnyMethod(); // Allow all HTTP methods
    });
});

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
