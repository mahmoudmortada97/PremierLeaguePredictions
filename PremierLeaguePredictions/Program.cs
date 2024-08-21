
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.OpenApi.Models;
using PremierLeaguePredictions.Services;
using System.Reflection;



namespace PremierLeaguePredictions
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "PremierLeaguePredictions",
                });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                option.IncludeXmlComments(xmlPath);
            });

            builder.Services.AddHangfire(c => c.UseMemoryStorage()); JobStorage.Current = new MemoryStorage();
            builder.Services.AddHangfireServer();

            var apiKey = builder.Configuration["JotForm:ApiKey"];
            var formId = builder.Configuration["JotForm:FormId"];
            builder.Services.AddSingleton(new JotFormService(formId, apiKey));
            // In Startup.cs or Program.cs
            builder.Services.AddTransient<ScoringService>(provider =>
                new ScoringService($"https://api.jotform.com/form/{formId}/submissions?apiKey={apiKey}"));
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddHttpClient(); // Register IHttpClientFactory

            var app = builder.Build();
            app.UseHangfireDashboard();

            // Configure the HTTP request pipeline.
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
