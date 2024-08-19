
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


            var apiKey = builder.Configuration["JotForm:ApiKey"];
            var formId = builder.Configuration["JotForm:FormId"];
            builder.Services.AddSingleton(new JotFormService(formId, apiKey));
            // In Startup.cs or Program.cs
            builder.Services.AddTransient<ScoringService>(provider =>
                new ScoringService("https://api.example.com/realOrder", $"https://api.jotform.com/form/{formId}/submissions?apiKey={apiKey}"));

            var app = builder.Build();

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
