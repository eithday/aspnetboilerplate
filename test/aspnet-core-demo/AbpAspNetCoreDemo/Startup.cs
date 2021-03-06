﻿using System;
using System.Reflection;
using Abp.AspNetCore;
using Abp.AspNetCore.Mvc;
using Abp.AspNetCore.Mvc.Providers;
using AbpAspNetCoreDemo.Core.Application;
using AbpAspNetCoreDemo.EntityFrameworkCore;
using Castle.Facilities.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AbpAspNetCoreDemo
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<MyDbContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString("Default"))
            );

            var mvc = services.AddMvc(options =>
            {
                options.AddAbp(); //Add ABP infrastructure to MVC
            });

            //TODO: Move this to a configurable extension method.
            mvc.PartManager.ApplicationParts.Add(new AssemblyPart(Assembly.GetAssembly(typeof(ProductAppService))));
            mvc.PartManager.FeatureProviders.Add(new AbpAppServiceControllerFeatureProvider());

            mvc.AddControllersAsServices();

            //Configure Abp and Dependency Injection
            return services.AddAbp(abpBootstrapper =>
            {
                //Configure Log4Net logging
                abpBootstrapper.IocManager.IocContainer.AddFacility<LoggingFacility>(
                    f => f.UseLog4Net().WithConfig("log4net.config")
                );
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseAbp(); //Initializes ABP framework.

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}"
                    );

                //TEST route
                routes.MapRoute(
                    name: "service-1",
                    template: "api/services/app/demo/{action}",
                    defaults: new
                    {
                        controller = "MyDemoAppService"
                    });
            });
        }
    }
}
