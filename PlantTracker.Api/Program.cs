using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PlantTracker.Api.Data;
using PlantTracker.Api.Models;
using PlantTracker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
// Resolve the db path relative to the content root so it's always in the
// project folder regardless of what working directory Rider launches from.
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "planttracker.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ── Identity ────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ── JWT ──────────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);
builder.Services.AddScoped<JwtService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
        };
    });

// ── CORS (allows the MAUI app to call the API) ───────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMauiApp", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Controllers & OpenAPI ────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<PlantService>();
builder.Services.AddHttpClient<ZoneService>();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PlantTracker API", Version = "v1" });

    // Adds the "Authorize" button to Swagger UI so you can test JWT-protected endpoints
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });
    options.AddSecurityRequirement(doc =>
    {
        var scheme = new OpenApiSecuritySchemeReference("Bearer", doc);
        return new OpenApiSecurityRequirement { [scheme] = [] };
    });
});

var app = builder.Build();

// ── Auto-apply migrations on startup ────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("==============================================");
Console.WriteLine($"  [DB] Database path:");
Console.WriteLine($"  {dbPath}");
Console.WriteLine("==============================================");
Console.ResetColor();
Console.Out.Flush();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    Console.WriteLine("[DB] Migrations applied. Database is ready.");
    app.Logger.LogInformation("[DB] Migrations applied. Database is ready.");
}

// ── Middleware pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PlantTracker API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowMauiApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

