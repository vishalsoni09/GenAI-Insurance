using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register application services (simple singletons for POC)
builder.Services.AddSingleton<LoanEligibility.Poc.Api.Services.LoanEligibilityService>();
builder.Services.AddSingleton<LoanEligibility.Poc.Api.Services.DbService>();
builder.Services.AddSingleton<LoanEligibility.Poc.Api.Services.OpenAIService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
