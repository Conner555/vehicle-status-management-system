//依赖注入
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Repositories;
using VehicleStatusSystem.Services;
using VehicleStatusSystem.Exceptions;
using Microsoft.AspNetCore.Identity;
using VehicleStatusSystem.Models; 
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;




var builder = WebApplication.CreateBuilder(args);

//注册————Add services to the container.
builder.Services.AddControllers();

builder.Services.AddScoped<
    IVehicleRepository,
    VehicleRepository>();

builder.Services.AddScoped<
    IVehicleStatusRepository,
    VehicleStatusRepository>();

builder.Services.AddScoped<
    IVehicleService,
    VehicleService>();

builder.Services.AddScoped<
    IVehicleStatusService,
    VehicleStatusService>();


builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();

builder.Services.AddScoped<
    ITokenService,
    JwtTokenService>();


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


string jwtSecret =
    builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException(
        "JWT secret key is not configured.");


builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSecret)),

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();


//————————————————————————————————————

var app = builder.Build();

//注册excepiton
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
