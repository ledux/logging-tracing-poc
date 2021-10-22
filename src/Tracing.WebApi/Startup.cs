using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Tracing.WebApi.Controllers;
using Tracing.WebApi.Producer;


namespace Tracing.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<KafkaProducer>();
            services.AddOpenTelemetryTracing(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddSource(nameof(DataController))
                .AddSource(nameof(KafkaProducer))
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Tracing.WebApi"))
                .AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug)
                .AddJaegerExporter(options =>
                    {
                        options.AgentHost = "localhost";
                        options.AgentPort = 6831;
                        options.ExportProcessorType = ExportProcessorType.Simple;
                    }));
            
            // var tracerProvider = Sdk.CreateTracerProviderBuilder()
            //     .SetSampler(new AlwaysOnSampler())
            //     .AddSource("tracer: Tracing.WebApi")
            //     .AddJaegerExporter(options =>
            //        {
            //            options.AgentHost = "localhost";
            //            options.AgentPort = 6831;
            //        })
            //     .AddHttpClientInstrumentation()
            //     .AddAspNetCoreInstrumentation()
            //     .Build();
            //
            // services.AddSingleton(tracerProvider);
            
            services.AddControllers();
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tracing.WebApi", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tracing.WebApi v1"));
            }
            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
