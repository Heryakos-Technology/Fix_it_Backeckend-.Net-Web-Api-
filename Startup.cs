using System;
using AutoMapper;
using fixit.Data;
using fixit.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using fixit.Helpers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
// using fixit.Service;

namespace fixit
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
            string connectionString; // Explicit declaration
            //     ));
     var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(databaseUrl))
{
    // Local development
    connectionString = Configuration.GetConnectionString("PostgreSqlConnectionString");
}
else
{
    // Fix for Render's connection format
    var fixedUrl = databaseUrl
        .Replace("postgres://", "postgresql://", StringComparison.OrdinalIgnoreCase)
        .Replace("postgresql://", "postgresql://", StringComparison.OrdinalIgnoreCase);

    var uri = new Uri(fixedUrl);
    var userInfo = uri.UserInfo.Split(':');
    
    // Handle port extraction
    var port = uri.Port > 0 ? uri.Port : 5432;
    
    // Handle special characters in password
    var password = Uri.UnescapeDataString(userInfo[1]);

    connectionString = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = port,
        Username = userInfo[0],
        Password = password,
        Database = uri.AbsolutePath.TrimStart('/'),
        SslMode = SslMode.Require,
        // TrustServerCertificate = true
    }.ToString();
}
if (string.IsNullOrEmpty(connectionString) || !connectionString.Contains("Host="))
{
    throw new InvalidOperationException("Invalid database connection configuration");
}
            services.AddDbContext<DataContext>(opt => opt.UseNpgsql(connectionString));
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddCors(option =>
            {
                option.AddPolicy("allowedOrigin",
                    builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
                    );
            });
            services.AddControllers();
            // configure strongly typed settings objects
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            // configure jwt authentication
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
            services.AddHealthChecks();
            // services.AddControllersWithViews()
            // configure DI for application services
            // services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRepository<Service>, ServiceRepository>();
            services.AddScoped<IRepository<Job>, JobRepository>();
            services.AddScoped<IRepository<Role>, RoleRepository>();
            services.AddScoped<IRepository<User>, UserRepository>();
            services.AddScoped<IRepository<Technician>, TechnicianRepository>();
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();
            app.UseHealthChecks("/health");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
