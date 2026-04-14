using System.Text;
using CapFinLoan.Admin.Application.Interfaces;
using CapFinLoan.Admin.Application.Middleware;
using CapFinLoan.Admin.Application.Services;
using CapFinLoan.Admin.Infrastructure;
using CapFinLoan.Admin.Infrastructure.Clients;
using CapFinLoan.Admin.Infrastructure.Options;
using CapFinLoan.Admin.Persistence;
using CapFinLoan.Admin.Persistence.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5088");

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

var adminConnectionString =
    builder.Configuration.GetConnectionString("AdminServiceConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:AdminServiceConnection or ConnectionStrings:DefaultConnection");

builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer(adminConnectionString));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<ApplicationServiceOptions>(builder.Configuration.GetSection("ApplicationService"));
builder.Services.Configure<DocumentServiceOptions>(builder.Configuration.GetSection("DocumentService"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

var rabbitMqOptions = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
    ?? new RabbitMqOptions();

builder.Services.AddMassTransit(configurator =>
{
    configurator.UsingRabbitMq((_, cfg) =>
    {
        cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.Port, rabbitMqOptions.VirtualHost, host =>
        {
            host.Username(rabbitMqOptions.Username);
            host.Password(rabbitMqOptions.Password);
        });
    });
});

builder.Services.AddScoped<IDecisionRepository, DecisionRepository>();
builder.Services.AddScoped<IStatusHistoryRepository, StatusHistoryRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddHttpClient<IApplicationClient, ApplicationClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApplicationServiceOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException("ApplicationService:BaseUrl is missing from configuration.");
    }

    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddHttpClient<IDocumentClient, DocumentClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DocumentServiceOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException("DocumentService:BaseUrl is missing from configuration.");
    }

    client.BaseAddress = new Uri(options.BaseUrl);
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    var roleClaims = identity.FindAll(identity.RoleClaimType).ToList();
                    foreach (var roleClaim in roleClaims)
                    {
                        var normalizedRole = roleClaim.Value.ToUpperInvariant();
                        if (!identity.HasClaim(identity.RoleClaimType, normalizedRole))
                        {
                            identity.AddClaim(new Claim(identity.RoleClaimType, normalizedRole));
                        }
                    }
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CapFinLoan Admin API",
        Version = "v1",
        Description = "Admin and reporting workflows for CapFinLoan"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT token: Bearer {your token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin API v1");
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
