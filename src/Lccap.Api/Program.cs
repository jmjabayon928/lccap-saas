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

var app = builder.Build();

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
