using EShopy.Api.Middlewares;
using EShopy.Application.Common.Context;
using EShopy.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
  options.SwaggerDoc("v1", new OpenApiInfo
  {
    Title = "EShopy API",
    Version = "v1"
  });

  var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
  var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
  if (File.Exists(xmlPath))
    options.IncludeXmlComments(xmlPath);
});

// Contexts
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<UserContext>();

// Infrastructure + handlers (CQRS)
builder.Services.AddInfrastructure(builder.Configuration);

// Auth — Keycloak OIDC / JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    options.Authority = builder.Configuration["Auth:Authority"];
    options.Audience = builder.Configuration["Auth:Audience"];
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
      NameClaimType = "preferred_username",
      RoleClaimType = "roles"
    };
  });

builder.Services.AddAuthorization(options =>
{
  options.AddPolicy("CatalogRead", policy =>
    policy.RequireClaim("permissions", "catalog.read"));

  options.AddPolicy("CatalogWrite", policy =>
    policy.RequireClaim("permissions", "catalog.write"));

  options.AddPolicy("OrdersRead", policy =>
    policy.RequireClaim("permissions", "orders.read"));

  options.AddPolicy("OrdersWrite", policy =>
    policy.RequireClaim("permissions", "orders.write"));

  options.AddPolicy("UsersManage", policy =>
    policy.RequireClaim("permissions", "users.manage"));
});

var app = builder.Build();

// Pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<RequestLoggingScopeMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/", (IWebHostEnvironment env) =>
{
  if (env.IsDevelopment())
    return Results.Redirect("/swagger");

  return Results.Ok(new { service = "EShopy.Api", status = "ok" });
});

app.Run();

public partial class Program { }
