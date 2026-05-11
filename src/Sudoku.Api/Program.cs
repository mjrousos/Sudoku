using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sudoku.Api;
using Sudoku.Api.Auth;
using Sudoku.Api.Contracts;
using Sudoku.Api.Data;
using Sudoku.Api.GameEngine;
using Sudoku.Api.Games;
using Sudoku.Api.Storage;

const string SpaCorsPolicy = "SpaCors";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
        context.ProblemDetails.Extensions.TryAdd("code", ApiProblemCodes.UnexpectedError);
    };
});

builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Type = ApiProblemTypes.Validation,
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Correct the request body and try again.",
            Instance = context.HttpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        problemDetails.Extensions["code"] = ApiProblemCodes.ValidationFailed;

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "__Host-Sudoku.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(SpaCorsPolicy, policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });
}

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(RateLimitPolicies.GameGeneration, limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter(RateLimitPolicies.Hints, limiter =>
    {
        limiter.PermitLimit = 60;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.AddNpgsqlDbContext<SudokuDbContext>("sudokudb");
builder.Services.AddScoped<ICompletedGameRepository, CompletedGameRepository>();
builder.Services.AddSudokuAuthentication(builder.Configuration, builder.Environment);
builder.Services.Configure<ProfilePictureOptions>(builder.Configuration.GetSection(ProfilePictureOptions.SectionName));
builder.Services.AddSingleton<IProfilePictureService, BlobProfilePictureService>();
builder.Services.AddSingleton<ISudokuSolver, SudokuSolver>();
builder.Services.AddSingleton<ISudokuGenerator, SudokuGenerator>();
if (builder.Configuration.GetValue("E2E:UseDeterministicGenerator", false))
{
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException("The deterministic E2E puzzle generator cannot be enabled in production.");
    }

    builder.Services.AddSingleton<IPuzzleGenerationService, DeterministicPuzzleGenerationService>();
}
else
{
    builder.Services.AddSingleton<IPuzzleGenerationService, PuzzleGenerationService>();
}
builder.Services.AddSingleton<IGameStore, InMemoryGameStore>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<PuzzleGenerationOptions>(builder.Configuration.GetSection("PuzzleGeneration"));
builder.Services.Configure<GameStoreOptions>(builder.Configuration.GetSection("GameStore"));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
if (allowedOrigins.Length > 0)
{
    app.UseCors(SpaCorsPolicy);
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
