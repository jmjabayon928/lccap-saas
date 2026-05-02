using Lccap.Api.Auth;
using Lccap.Api.Extensions;
using Lccap.Api.Health;
using Lccap.Api.Options;
using Lccap.Application.Common;
using Lccap.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.AddLccapServices();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));
builder.Services.Configure<DemoSeedOptions>(builder.Configuration.GetSection("DemoSeed"));
if (builder.Environment.IsDevelopment())
{
    builder.Services.PostConfigure<FileStorageOptions>(
        options =>
        {
            options.RootPath = string.IsNullOrWhiteSpace(options.RootPath)
                ? Path.Combine(builder.Environment.ContentRootPath, "uploads")
                : options.RootPath;
            options.MaxUploadBytes = options.MaxUploadBytes <= 0 ? 10 * 1024 * 1024 : options.MaxUploadBytes;
            options.AllowedExtensions = options.AllowedExtensions is { Length: > 0 }
                ? options.AllowedExtensions
                : [".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg"];
        });
}
builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtAuthHandler>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddAuthorization();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(
        options =>
        {
            options.AddDefaultPolicy(
                policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:3000",
                            "http://localhost:3001",
                            "http://localhost:3010")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
        });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var demoSeedOptions = app.Services.GetRequiredService<IOptions<DemoSeedOptions>>().Value;
    if (demoSeedOptions.Enabled)
    {
        if (string.IsNullOrWhiteSpace(demoSeedOptions.Password))
        {
            throw new InvalidOperationException("DemoSeed:Enabled is true but DemoSeed:Password is blank. Please provide a password via environment variable or local secrets.");
        }

        using var scope = app.Services.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<Lccap.Infrastructure.Seed.DemoSeedService>();
        await seedService.SeedAsync(demoSeedOptions.Password);
    }
    app.UseCors();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.Use(
    async (context, next) =>
    {
        var currentUserContext = context.RequestServices.GetRequiredService<ICurrentUserContext>();
        if (currentUserContext is CurrentUserContext mutableContext)
        {
            mutableContext.SetFromPrincipal(context.User);
        }

        await next();
    });
app.UseAuthorization();
app.MapHealthEndpoints();
app.MapControllers();

app.Run();

public partial class Program;
