using System.Text;
using Azure.Identity;
using edpicker_api.Models;
using edpicker_api.Services;
using edpicker_api.Services.Interface;
using OpenAI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Register OpenAI client using API key from configuration
var openAiKey = builder.Configuration["OpenAIKey"];
builder.Services.AddSingleton(_ => new OpenAIClient(openAiKey));

// Add services to the container.
builder.Services.AddScoped<IJobBoardRepository, JobBoardRepository>();
builder.Services.AddScoped<ISchoolAccountRepository, SchoolAccountRepository>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IQuestionPaperRepository, QuestionPaperRepository>();
builder.Services.AddControllers();
builder.Services.AddDbContext<EdPickerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICommonRepository, CommonRepository>();
builder.Services.AddHttpClient();
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

app.MapControllers();

app.Run();
