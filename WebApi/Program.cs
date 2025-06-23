using Application.Appointments.Commands;
using Application.Common.Interfaces;
using Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Services;
using Infrastructure.BackgroundJobs;
using WebApi.Converters;
using Application.Common.Behaviors;
using MediatR;
using WebApi.FIlters;

namespace WebApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<GlobalExceptionFilter>();
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new TimeOnlyConverter());
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(CreateAppointmentCommand).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            builder.Services.AddValidatorsFromAssembly(typeof(CreateAppointmentValidator).Assembly);

            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddSingleton<IDateTimeService, DateTimeService>();

            builder.Services.AddHostedService<ReminderService>();

            builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

            var app = builder.Build();

            await InitializeDatabaseAsync(app);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

            app.Run();
        }

        static async Task InitializeDatabaseAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {   
                var initializer = services.GetRequiredService<IDatabaseInitializer>();
                await initializer.InitializeAsync();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Database initialization failed");
                throw;
            }
        }
    }
}