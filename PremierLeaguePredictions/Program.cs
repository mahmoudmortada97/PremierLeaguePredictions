using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.OpenApi.Models;
using PremierLeaguePredictions.Filters;
using PremierLeaguePredictions.Services;
using System.Reflection;

namespace PremierLeaguePredictions
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "PremierLeaguePredictions" });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                option.IncludeXmlComments(xmlPath);
            });

            // Hangfire
            builder.Services.AddHangfire(c => c.UseMemoryStorage());
            JobStorage.Current = new MemoryStorage();
            builder.Services.AddHangfireServer();

            // Named HttpClient for JotForm — reuses connections via connection pooling
            builder.Services.AddHttpClient("JotForm", client =>
            {
                client.BaseAddress = new Uri("https://api.jotform.com/");
            });

            // Services — constructor injection now handles all config reading
            builder.Services.AddSingleton<JotFormService>();
            builder.Services.AddSingleton<ScoringService>();
            builder.Services.AddScoped<EmailService>();

            var app = builder.Build();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthFilter(
                    builder.Configuration["HangfireDashboardKey"] ?? string.Empty) }
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}