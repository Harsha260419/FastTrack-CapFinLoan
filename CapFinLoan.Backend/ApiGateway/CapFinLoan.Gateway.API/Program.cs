using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

var ocelotFileName = builder.Environment.IsEnvironment("Docker") ? "ocelot.Docker.json" : "ocelot.json";
builder.Configuration.AddJsonFile(ocelotFileName, optional: false, reloadOnChange: true);

var jwtSection = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JwtSettings:Key is missing.");
var jwtIssuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is missing.");
var jwtAudience = jwtSection["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is missing.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "GatewayJwt";
        options.DefaultChallengeScheme = "GatewayJwt";
    })
    .AddJwtBearer("GatewayJwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerForOcelot(builder.Configuration, options =>
{
    options.GenerateDocsForGatewayItSelf = false;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/swagger/docs/v1/application", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/swagger/docs/v1/applications", permanent: false);
        return;
    }

    if (context.Request.Path.Equals("/swagger/docs/v1/document", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/swagger/docs/v1/documents", permanent: false);
        return;
    }

    if (context.Request.Path.Equals("/swagger/docs/v1/gateway", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/swagger/docs/v1/auth", permanent: false);
        return;
    }

    await next();
});

app.UseSwaggerForOcelotUI(options =>
{
    options.PathToSwaggerGenerator = "/swagger/docs";
});

await app.UseOcelot();

app.Run();
