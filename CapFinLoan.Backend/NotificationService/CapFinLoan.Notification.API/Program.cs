using CapFinLoan.Notification.API.Consumers;
using CapFinLoan.Notification.API.Options;
using CapFinLoan.Notification.API.Services;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5095");

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

var rabbitMqOptions = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
    ?? new RabbitMqOptions();

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<DecisionMadeEventConsumer, DecisionMadeEventConsumerDefinition>();
    configurator.AddConsumer<DecisionMadeEventFaultConsumer>();
    configurator.AddConsumer<OtpRequestedEventConsumer, OtpRequestedEventConsumerDefinition>();
    configurator.AddConsumer<OtpRequestedEventFaultConsumer>();

    configurator.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.Port, rabbitMqOptions.VirtualHost, host =>
        {
            host.Username(rabbitMqOptions.Username);
            host.Password(rabbitMqOptions.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.Run();
