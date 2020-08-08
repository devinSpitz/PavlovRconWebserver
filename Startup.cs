using System;
using System.Net;
using AspNetCore.Identity.LiteDB;
using AspNetCore.Identity.LiteDB.Data;
using AspNetCore.Identity.LiteDB.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver
{
   public class Startup
   {
      public Startup(IConfiguration configuration) => Configuration = configuration;

      public IConfiguration Configuration { get; }

      // This method gets called by the runtime. Use this method to add services to the container.
      public void ConfigureServices(IServiceCollection services)
      {
         // Add LiteDB Dependency. Thare are three ways to set database:
         // 1. By default it uses the first connection string on appsettings.json, ConnectionStrings section.
         services.AddSingleton<ILiteDbContext, LiteDbContext>();
         services.AddTransient<RconServerSerivce>();
         services.AddTransient<UserService>();
         services.AddTransient<RconService>();

         // 2. Custom context implementing ILiteDbContext
         //services.AddSingleton<AppDbContext>();

         // 3. Cusom context by using constructor
         //services.AddSingleton<ILiteDbContext, LiteDbContext>(x => new LiteDbContext(new LiteDatabase("Filename=Database.db")));

         services.AddIdentity<InbuildUser, AspNetCore.Identity.LiteDB.IdentityRole>(options =>
            {
               
               options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
               options.SignIn.RequireConfirmedEmail = false;
               options.Password.RequireDigit = false;
               options.Password.RequireUppercase = false;
               options.Password.RequireLowercase = false;
               options.Password.RequireNonAlphanumeric = false;
               options.Password.RequiredLength = 6;

               //opts.SignIn.RequireConfirmedEmail = true;

               options.Lockout.AllowedForNewUsers = true;
               options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
               options.Lockout.MaxFailedAccessAttempts = 3;
            })
            //.AddEntityFrameworkStores<ApplicationDbContext>()
            .AddUserStore<LiteDbUserStore<InbuildUser>>().AddRoles<AspNetCore.Identity.LiteDB.IdentityRole>().AddRoleManager<RoleManager<AspNetCore.Identity.LiteDB.IdentityRole>>()
            .AddRoleStore<LiteDbRoleStore<AspNetCore.Identity.LiteDB.IdentityRole>>()
            .AddDefaultTokenProviders();

         // Add application services.
         services.AddTransient<IEmailSender, EmailSender>();
         
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
