using System.Text;
using CapFinLoan.Application.Application.Interfaces;
using CapFinLoan.Application.Application.Services;
using CapFinLoan.Application.Infrastructure;
using CapFinLoan.Application.Infrastructure.Clients;
using CapFinLoan.Application.Infrastructure.Options;
using CapFinLoan.Application.Persistence;
using CapFinLoan.Application.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var applicationConnectionString =
	builder.Configuration.GetConnectionString("ApplicationServiceConnection")
	?? throw new InvalidOperationException("Missing ConnectionStrings:ApplicationServiceConnection for ApplicationService.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(applicationConnectionString));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<AdminServiceOptions>(builder.Configuration.GetSection(AdminServiceOptions.SectionName));

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<IAdminStatusHistoryClient, AdminStatusHistoryClient>((serviceProvider, client) =>
{
	var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminServiceOptions>>().Value;
	var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
		? "http://localhost:5088"
		: options.BaseUrl;

	client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();
builder.Services.AddScoped<ILoanApplicationService, LoanApplicationService>();

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
	});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "CapFinLoan Application API",
		Version = "v1",
		Description = "Loan application lifecycle service for CapFinLoan"
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
		options.SwaggerEndpoint("/swagger/v1/swagger.json", "Application API v1");
	});
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
