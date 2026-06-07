using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Allow both http and https variants of common dev servers (Vite / CRA)
        policy.WithOrigins(
            "http://localhost:5173",
            "https://localhost:5173",
            "http://localhost:3000",
            "https://localhost:3000",
            "http://localhost:61243",
            "https://localhost:61243"
        ).AllowAnyHeader().AllowAnyMethod();
    });
});

// EF Core DbContext registration
builder.Services.AddDbContext<GenAI_Insurance.Server.Data.LoanDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register other application services
builder.Services.AddSingleton<GenAI_Insurance.Server.Services.MemoryService>();
builder.Services.AddScoped<GenAI_Insurance.Server.Services.SqlAgentService>();
builder.Services.AddScoped<GenAI_Insurance.Server.Services.AgentService>();
// Additional app services
// Register OpenAIService as a typed HttpClient so HttpClientFactory + DI provide HttpClient and ILogger
builder.Services.AddHttpClient<GenAI_Insurance.Server.Services.OpenAIService>();
builder.Services.AddSingleton<GenAI_Insurance.Server.Services.MemoryService>();
builder.Services.AddScoped<GenAI_Insurance.Server.Services.SqlAgentService>();
builder.Services.AddScoped<GenAI_Insurance.Server.Services.AgentService>();
builder.Services.AddSingleton<GenAI_Insurance.Server.Services.RagService>();
builder.Services.AddScoped<GenAI_Insurance.Server.Services.SqlGeneratorService>();
builder.Services.AddSingleton<GenAI_Insurance.Server.Services.DbService>();
builder.Services.AddSingleton<GenAI_Insurance.Server.Services.LoanEligibilityService>();

var app = builder.Build();

app.UseDefaultFiles();
// Serve static files from wwwroot (will contain built SPA)
app.UseStaticFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();

// Fallback to SPA index if path not found
app.MapFallbackToFile("index.html");

app.Run();
