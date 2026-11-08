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
using Microsoft.Extensions.Caching.Distributed;
using VehicleStatusSystem.Caching;

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

builder.Services.AddScoped<
    IInspectionTimeoutRepository,
    InspectionTimeoutRepository>();

builder.Services.AddHostedService<
    InspectionTimeoutBackgroundService>();

builder.Services.AddScoped<
    IAdminInspectionTimeoutRepository,
    AdminInspectionTimeoutRepository>();

builder.Services.AddScoped<
    IAdminInspectionTimeoutService,
    AdminInspectionTimeoutService>();

builder.Services.AddScoped<
    IInspectionReminderRepository,
    InspectionReminderRepository>();

builder.Services.AddScoped<
    IOperatorInspectionReminderRepository,
    OperatorInspectionReminderRepository>();

builder.Services.AddScoped<
    IOperatorInspectionReminderService,
    OperatorInspectionReminderService>();

builder.Services.AddScoped<
    IRefundRetryRepository,
    RefundRetryRepository>();


//Redis
builder.Services.AddScoped<
    IDashboardCacheInvalidator,
    DashboardCacheInvalidator>();

builder.Services.AddSingleton<DashboardCacheGate>();
builder.Services.AddSingleton<VehicleCacheGate>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


string jwtSecret =
    builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException(
        "JWT secret key is not configured.");


//JWT注册
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

//Redis注册
var redisConnection =
    builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException(
        "Redis connection string is missing.");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "FleetFlow:";
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

//测试Redis

if (app.Environment.IsDevelopment())
{
    app.MapGet(
        "/api/dev/redis-test",
        async (
            IDistributedCache cache,
            CancellationToken cancellationToken) =>
        {
            // 每次生成独立测试 Key
            string key =
                $"test:connection:{Guid.NewGuid():N}";

            string value =
                $"FleetFlow Redis OK {DateTime.UtcNow:O}";

            // 写入 Redis，120 秒后过期
            await cache.SetStringAsync(
                key,
                value,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(120)
                },
                cancellationToken);

            // 从 Redis 读取数据
            string? cachedValue =
                await cache.GetStringAsync(
                    key,
                    cancellationToken);

            // 检验读写是否一致
            bool success = value == cachedValue;

            return Results.Ok(new
            {
                success,
                key,
                originalValue = value,
                cachedValue,
                ttlSeconds = 120
            });
        })
        .RequireAuthorization(policy =>
            policy.RequireRole("Admin"));
}
//

app.Run();
