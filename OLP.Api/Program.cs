using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

using OLP.Infrastructure.Data;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Repositories;
using OLP.Infrastructure.Services;
using OLP.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// === DbContext ===
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// === Repositories ===
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<ILessonCompletionRepository, LessonCompletionRepository>();
builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();
builder.Services.AddScoped<IQuizAttemptRepository, QuizAttemptRepository>();
builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped<IQuizAttemptAnswerRepository, QuizAttemptAnswerRepository>();

// === Services ===
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<CertificateService>();

// === Cloudinary ===
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// === CORS (optional but recommended for frontend) ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ✅ Controllers + JSON options (ENUM as string)
// This makes DifficultyLevel.Beginner serialize as "Beginner" instead of 0
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Optional: if you want enums to be case-insensitive in some cases,
        // this setting doesn't affect parsing from your DTO strings.
    });

builder.Services.AddEndpointsApiExplorer();

// === JWT Authentication ===
var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key")
             ?? throw new Exception("Missing Jwt:Key");
var jwtIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer");
var jwtAudience = builder.Configuration.GetValue<string>("Jwt:Audience");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// === Swagger + JWT Authorize button ===
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OLP API",
        Version = "v1"
    });

    // Add JWT auth to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

// === SEED ADMIN + SUPERADMIN USERS ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await DbSeeder.SeedUsersAsync(db, config);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // optional during dev

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
