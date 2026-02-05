using EShopy.Api.Middlewares;
using EShopy.Application.Common.Context;
using EShopy.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Contexts
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<UserContext>();

// Infrastructure (tenant resolver placeholder)
builder.Services.AddInfrastructure();

// Auth (Keycloak) - wiring placeholder:
// builder.Services.AddAuthentication("Bearer").AddJwtBearer(...);
// builder.Services.AddAuthorization(...);

var app = builder.Build();

// Pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<RequestLoggingScopeMiddleware>();

// app.UseAuthentication();
// app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program { }
