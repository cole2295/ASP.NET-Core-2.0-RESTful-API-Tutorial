﻿using System.IO;
using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyRestful.Api.Resources;
using MyRestful.Api.Validators;
using MyRestful.Core.Interfaces;
using MyRestful.Infrastructure;
using MyRestful.Infrastructure.Repositories;
using Serilog;
using Serilog.Events;

namespace MyRestful.Api
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;
        public static IConfiguration Configuration { get; private set; }

        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine("logs", @"log.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            services.AddScoped<ICountryRepository, CountryRepository>();
            services.AddScoped<ICityRepository, CityRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddAutoMapper();
            services.AddDbContext<MyContext>(options =>
            {
                options.UseInMemoryDatabase("MyDatabase");
            });
            services.AddMvc(options =>
            {
                options.ReturnHttpNotAcceptable = true;
                options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                options.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());
            }).AddFluentValidation();

            services.AddTransient<IValidator<CityAddResource>, CityAddOrUpdateResourceValidator<CityAddResource>>();
            services.AddTransient<IValidator<CityUpdateResource>, CityUpdateResourceValidator>();
            services.AddTransient<IValidator<CountryAddResource>, CountryAddResourceValidator>();

            // services.AddMvc()
            //     .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseStatusCodePages();

            app.UseExceptionHandler(builder =>
            {
                builder.Run(async context =>
                {
                    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (exceptionHandlerFeature != null)
                    {
                        var logger = _loggerFactory.CreateLogger("Global Exception Logger");
                        logger.LogError(500, exceptionHandlerFeature.Error, exceptionHandlerFeature.Error.Message);
                    }
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(exceptionHandlerFeature?.Error?.Message ?? "An Error Occurred.");
                });
            });

            app.UseMvc();
        }
    }
}
