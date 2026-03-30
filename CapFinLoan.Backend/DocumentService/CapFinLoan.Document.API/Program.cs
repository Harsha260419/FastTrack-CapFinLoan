using System.Security.Claims;
using System.Text;
using CapFinLoan.Document.API.Extensions;
using CapFinLoan.Document.Application.Interfaces;
using CapFinLoan.Document.Application.Services;
using CapFinLoan.Document.Infrastructure;
using CapFinLoan.Document.Infrastructure.Clients;
using CapFinLoan.Document.Infrastructure.Options;
using CapFinLoan.Document.Infrastructure.Storage;
using CapFinLoan.Document.Persistence;
using CapFinLoan.Document.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<DocumentsDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.SectionName));
builder.Services.Configure<ApplicationServiceOptions>(builder.Configuration.GetSection(ApplicationServiceOptions.SectionName));

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

builder.Services.AddHttpClient<IApplicationServiceClient, ApplicationServiceClient>((serviceProvider, client) =>
{
	var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApplicationServiceOptions>>().Value;
	if (string.IsNullOrWhiteSpace(options.BaseUrl))
	{
		throw new InvalidOperationException("ApplicationService:BaseUrl is missing from configuration.");
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
		Title = "CapFinLoan Document API",
		Version = "v1",
		Description = "Document upload and verification service for CapFinLoan"
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

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(options =>
	{
		options.SwaggerEndpoint("/swagger/v1/swagger.json", "Document API v1");
	});
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
