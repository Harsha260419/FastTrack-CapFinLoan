using CapFinLoan.Chat.API.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<LMStudioOptions>(builder.Configuration.GetSection(LMStudioOptions.SectionName));
builder.Services.Configure<ApplicationServiceOptions>(builder.Configuration.GetSection(ApplicationServiceOptions.SectionName));

builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

app.MapControllers();

app.Run();
