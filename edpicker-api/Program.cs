using edpicker_api.Services;

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
        policy.WithOrigins("http://localhost:4200", "https://edpicker-gkg3hndqb4feeqdt.eastasia-01.azurewebsites.net") // Specify allowed origin (Angular app)
              .AllowAnyHeader() // Allow all headers
              .AllowAnyMethod(); // Allow all HTTP methods
    });
});

builder.Services.AddScoped<ISchoolListRepository, SchoolListRepository>(
    x => new SchoolListRepository(builder.Configuration.GetConnectionString("cosmoDb"),
    builder.Configuration["CosmoConfig:primaryKey"],
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
