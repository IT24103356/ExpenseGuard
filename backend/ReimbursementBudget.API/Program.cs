using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ReimbursementBudget.API.Agents;
using ReimbursementBudget.API.Data;
using ReimbursementBudget.API.Data.Seed;
using ReimbursementBudget.API.Infrastructure.Payment;
using ReimbursementBudget.API.Services;
using ReimbursementBudget.API.Tools;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console()
          .WriteTo.File("logs/reimbursement-.txt", rollingInterval: RollingInterval.Day));

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npg => npg.EnableRetryOnFailure(3)));

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key not configured. Set Jwt:Key in environment/appsettings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IReimbursementService, ReimbursementService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IReportingService, ReportingService>();

// ── Payment Sandbox ───────────────────────────────────────────────────────────
builder.Services.AddSingleton<IPaymentProvider, SandboxPaymentProvider>();

// ── Agentic AI Layer ──────────────────────────────────────────────────────────
builder.Services.AddScoped<AgentToolbox>();
builder.Services.AddScoped<CoordinatorPlannerAgent>();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts => opts.AddPolicy("AllowAll", policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ReimbursementBudget API",
        Version = "v1",
        Description = "Reimbursement & Budget Tracking with CoordinatorPlannerAgent"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ReimbursementBudget v1"));
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Auto-migrate & seed on startup ────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.SeedAsync(db);
}

app.Run();
