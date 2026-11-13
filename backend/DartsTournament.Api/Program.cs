using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using DartsTournament.Api.Configuration;
using DartsTournament.Api.Data;
using DartsTournament.Api.Services;
using DartsTournament.Api.Middleware;
using DartsTournament.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Darts Tournament API",
        Description = "API de gestion de tournois de fléchettes - Permet de gérer les tournois, joueurs, matchs et scores.",
        Contact = new OpenApiContact
        {
            Name = "Darts Tournament"
        }
    });

    // Inclure les commentaires XML
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    // Configuration de l'authentification JWT dans Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Entrez 'Bearer' suivi d'un espace et du token. Exemple: 'Bearer eyJhbGc...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Secrets : jamais dans appsettings.json.
// Dev : dotnet user-secrets ; Prod : variables d'environnement (ConnectionStrings__DefaultConnection, Jwt__Key)
// User-secrets chargés hors Production (pas seulement en Development) : DOTNET_ENVIRONMENT=Staging
// sur le poste de dev l'emporterait sinon sur le profil de lancement.
if (!builder.Environment.IsProduction())
    builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection non configurée (user-secrets en dev, variable ConnectionStrings__DefaultConnection en prod).");

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Jwt:Key absente ou trop courte (32 octets minimum) : user-secrets en dev, variable Jwt__Key en prod.");
if (jwtKey.Contains("CHANGE", StringComparison.OrdinalIgnoreCase) && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Jwt:Key contient encore une valeur d'exemple : définir une vraie clé.");

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// JWT Authentication
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Modules activables (section Features, surchargeable par env : Features__Interclubs=false)
builder.Services.Configure<FeatureOptions>(builder.Configuration.GetSection(FeatureOptions.SectionName));
builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection(BrandingOptions.SectionName));

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TournamentService>();
// Grille FSGT (formats de tableaux selon le nombre d'inscrits), chargée et validée au démarrage
builder.Services.AddSingleton(FsgtFormatCatalog.Load(
    Path.Combine(AppContext.BaseDirectory, "Data", FsgtFormatCatalog.DefaultFileName)));
builder.Services.AddScoped<MatchSessionService>();
builder.Services.AddScoped<MatchStatsService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<PlayerStatsService>();
builder.Services.AddScoped<CricketService>();
builder.Services.AddScoped<CircuitService>();
builder.Services.AddScoped<InterclubService>();
builder.Services.AddScoped<SeasonService>();
builder.Services.AddScoped<ExploitService>();

// SignalR
builder.Services.AddSignalR();

// CORS (Cors:Origins = liste séparée par des virgules, surchargeable via CORS__Origins)
var corsOrigins = (builder.Configuration["Cors:Origins"] ?? "http://localhost:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseExceptionHandling(); // Global exception handler - must be first

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MatchHub>("/hubs/match");

app.Run();
