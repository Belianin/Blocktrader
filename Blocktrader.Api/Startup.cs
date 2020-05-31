using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Blocktrader.Service.Files;
using Nexus.Logging;
using Nexus.Logging.Console;
using Nexus.Logging.File;

namespace Nexus.Blocktrader.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddMvc(options => options.EnableEndpointRouting = false).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.Converters.Add(new TickerJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new ExchangeTitleJsonConverter());
            });

            services.AddSingleton<ILog>(sp => 
                new AggregationLog(new FileLog(), new ColourConsoleLog()));
            services.AddSingleton<ITimestampManager>(sp => 
                new BufferedTimestampManager(new FileTimestampManager(sp.GetRequiredService<ILog>())));
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, IWebHostEnvironment env, ILog log)
        {
            log.Info("Starting Blocktrader.API");
            lifetime.ApplicationStopping.Register(l => ((ILog) l)?.Dispose(), log);
            
            app.UseCors(o => o.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseRouting();
            app.UseMvc();

            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}