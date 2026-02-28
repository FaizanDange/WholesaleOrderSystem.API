using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using WholesaleOrderSystem.API.Data;
using Serilog;
using WholesaleOrderSystem.API.Middleware;

// Configure Serilog from configuration and use it as the logging provider
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build())
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    options.AddPolicy("AllowAll", policy =>
    {
        if (origins != null && origins.Length > 0)
        {
            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi();
    app.MapScalarApiReference();
//}

//if (app.Environment.IsDevelopment())
//{
    //app.UseHttpsRedirection();
//}
app.UseStaticFiles();

// Add request/response logging middleware early in the pipeline
app.UseRequestResponseLogging();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed Default Admin
//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    context.Database.Migrate();
//    if (!context.Users.Any(u => u.Role == "Admin"))
//    {
//        var admin = new WholesaleOrderSystem.API.Models.User
//        {
//            Name = "System Admin",
//            Email = "admin@wholesalebox.com",
//            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
//            Role = "Admin"
//        };
//        context.Users.Add(admin);
//        context.SaveChangesAsync().Wait();
//    }
//}

app.Run();
