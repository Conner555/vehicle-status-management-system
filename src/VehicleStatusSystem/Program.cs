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
using VehicleStatusSystem.Domain;

using VehicleStatusSystem.BackgroundServices;


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

builder.Services.AddScoped<
    IDashboardRepository,
    DashboardRepository>();

builder.Services.AddScoped<
    IDashboardService,
    DashboardService>();

builder.Services.AddScoped<
    IRentalOrderRepository,
    RentalOrderRepository>();

builder.Services.AddScoped<
    IRentalOrderService,
    RentalOrderService>();


builder.Services.AddHostedService<
    OrderExpirationBackgroundService>();

builder.Services.AddScoped<
    IPaymentRepository,
    PaymentRepository>();

builder.Services.AddScoped<
    IPaymentService,
    PaymentService>();

builder.Services.AddScoped<
    IHandoverRepository,
    HandoverRepository>();

builder.Services.AddScoped<
    IHandoverService,
    HandoverService>();

builder.Services.AddScoped<
    IReturnRepository,
    ReturnRepository>();

builder.Services.AddScoped<
    IReturnService,
    ReturnService>();

builder.Services.AddScoped<
    IInspectionRepository,
    InspectionRepository>();

builder.Services.AddScoped<
    IInspectionService,
    InspectionService>();


builder.Services.AddScoped<
    IMaintenanceRepository,
    MaintenanceRepository>();

builder.Services.AddScoped<
    IMaintenanceService,
    MaintenanceService>();

builder.Services.AddScoped<
    IAdminRentalOrderRepository,
    AdminRentalOrderRepository>();

builder.Services.AddScoped<
    IAdminRentalOrderService,
    AdminRentalOrderService>();

builder.Services.AddScoped<
    IAdminRentalOrderDetailRepository,
    AdminRentalOrderDetailRepository>();

builder.Services.AddScoped<
    IAdminRentalOrderDetailService,
    AdminRentalOrderDetailService>();

builder.Services.AddScoped<
    IRefundRepository,
    RefundRepository>();

builder.Services.AddScoped<
    IRefundService,
    RefundService>();

builder.Services.AddScoped<
    IEarlyReturnSettlementRepository,
    EarlyReturnSettlementRepository>();

builder.Services.AddScoped<
    IEarlyReturnSettlementService,
    EarlyReturnSettlementService>();

builder.Services.AddScoped<
    IVehicleAvailabilityRepository,
    VehicleAvailabilityRepository>();

builder.Services.AddScoped<
    IVehicleAvailabilityService,
    VehicleAvailabilityService>();

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


builder.Services.AddCors(options =>
{
    options.AddPolicy("VueFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


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

app.UseCors("VueFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
