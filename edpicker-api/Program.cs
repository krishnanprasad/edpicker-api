using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using edpicker_api;
using edpicker_api.Models;
using edpicker_api.Services;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenAI;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Capture startup errors so they can be reported on the root endpoint.
string? startupError = null;
try
{
    string openAiKey = GetSecretFromKeyVault(builder.Configuration, "KeyVault:OpenAIKeySecretName").GetAwaiter().GetResult();
    builder.Services.AddSingleton(_ => new OpenAIClient(openAiKey));
    // Retrieve Database Password from Azure Key Vault
    string dbPassword = GetSecretFromKeyVault(builder.Configuration, "KeyVault:DbPasswordSecretName").GetAwaiter().GetResult();

    // Add services to the container.
    builder.Services.AddScoped<IJobBoardRepository, JobBoardRepository>();
    builder.Services.AddScoped<ISchoolAccountRepository, SchoolAccountRepository>();
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
    builder.Services.AddScoped<IQuestionPaperRepository, QuestionPaperRepository>();
    builder.Services.AddControllers();
    builder.Services.AddDbContext<EdPickerDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddDbContext<EdPickerQuestionPaperDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddApplicationInsightsTelemetry();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ICommonRepository, CommonRepository>();
    builder.Services.AddScoped<ILoginRepository, LoginRepository>();
    builder.Services.AddHttpClient();
}
catch (Exception ex)
{
    startupError = ex.ToString();
}
// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://edpicker-gkg3hndqb4feeqdt.eastasia-01.azurewebsites.net",
                "https://edpicker.in",
                "https://lively-ground-07276021e.1.azurestaticapps.net" // ADD THIS LINE
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
 
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // Set a breakpoint on the next line
            var exception = context.Exception; // Inspect this in the debugger
            return Task.CompletedTask;
        }
    };
});
var app = builder.Build();

// Basic exception handler to surface errors in plain text while debugging.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(exception?.ToString() ?? "An error occurred");
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowSpecificOrigins");
app.UseHttpsRedirection();
app.UseAuthentication(); // <--- Add this BEFORE app.UseAuthorization()
app.UseAuthorization();

// Return a simple message on the root endpoint so the service can be probed easily.
app.MapGet("/", () =>
    string.IsNullOrEmpty(startupError)
        ? Results.Ok("Server is live")
        : Results.Problem(startupError));

app.MapControllers();

app.Run();
static async Task<string> GetSecretFromKeyVault(IConfiguration configuration, string secretNameConfig)
{
    string keyVaultUrl = configuration["KeyVault:Url"];
    string secretName = configuration[secretNameConfig];

    // Create a Key Vault client
    var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

    try
    {
        // Get the secret
        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        return secret.Value;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error retrieving secret: {ex.Message}");
        throw; // Re-throw the exception to prevent the application from running without the key
    }
}