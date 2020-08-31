
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PavlovRconWebserver.Services;
using LiteDB.Identity.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;

namespace PavlovRconWebserver
{
   public class Startup
   {
      public Startup(IConfiguration configuration) => Configuration = configuration;

      public IConfiguration Configuration { get; }

      // This method gets called by the runtime. Use this method to add services to the container.
      public void ConfigureServices(IServiceCollection services)
      {
         string connectionString = Configuration.GetConnectionString("DefaultConnection");
         services.AddLiteDBIdentity(connectionString).AddDefaultTokenProviders();
         // Add LiteDB Dependency. Thare are three ways to set database:
         // 1. By default it uses the first connection string on appsettings.json, ConnectionStrings section.
         services.AddTransient<RconServerSerivce>();
         services.AddTransient<UserService>();
         services.AddTransient<RconService>();
         
         // Add application services.
         services.AddTransient<IEmailSender, EmailSender>();
         services.AddSwaggerGen(c =>
         {
            c.SwaggerDoc("v0.0.1", new OpenApiInfo { 
               Title = "Pavlov Rcon Webserver API", 
               Version = "v0.0.1",
               Description ="Here you can see all function of the PavlovRconWebserver"
            });
         });
         services.AddMvc().AddRazorRuntimeCompilation();
      }

      // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         
         if (env.IsDevelopment())
         {
            app.UseDeveloperExceptionPage();
         }
         else
         {
            app.UseExceptionHandler("/Home/Error");
         }

         if (env.EnvironmentName == "Development")
         {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
               c.SwaggerEndpoint("/swagger/v0.0.1/swagger.json", "Pavlov Rcon Webserver V0.0.1");
          
            });
         }
         app.UseStaticFiles();

         app.UseRouting();
         app.UseAuthentication();
         app.UseAuthorization();
         app.UseHttpsRedirection();
         app.UseEndpoints(endpoints =>
         {
            endpoints.MapDefaultControllerRoute();
         });
      }
   }
}
