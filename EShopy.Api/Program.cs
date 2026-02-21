using EShopy.Api.Middlewares;
using EShopy.Application.Common.Context;
using EShopy.Infrastructure;
using EShopy.Infrastructure.Identity;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserContextAccessor>();

// Infrastructure + handlers (CQRS)
builder.Services.AddInfrastructure(builder.Configuration);

// CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
  ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
  options.AddPolicy("EShopyPolicy", policy =>
  {
    if (corsOrigins.Length > 0)
      policy.WithOrigins(corsOrigins);

    policy.SetIsOriginAllowedToAllowWildcardSubdomains()
      .AllowAnyMethod()
      .AllowAnyHeader()
      .WithExposedHeaders("X-Correlation-Id");
  });
});

// Auth - Keycloak OIDC / JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    var keycloak = builder.Configuration.GetSection("Keycloak");
    var authority = keycloak["Authority"] ?? builder.Configuration["Auth:Authority"];
    var audience = keycloak["Audience"] ?? builder.Configuration["Auth:Audience"];

    options.Authority = authority;
    options.Audience = audience;
    options.RequireHttpsMetadata = keycloak.GetValue<bool?>("RequireHttpsMetadata") ?? false;
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = keycloak.GetValue<bool?>("ValidateIssuer") ?? true,
      ValidateAudience = keycloak.GetValue<bool?>("ValidateAudience") ?? true,
      ValidateLifetime = keycloak.GetValue<bool?>("ValidateLifetime") ?? true,
      ValidateIssuerSigningKey = true,
      ClockSkew = TimeSpan.FromMinutes(5),
      NameClaimType = "preferred_username",
      RoleClaimType = "roles"
    };

    options.Events = new JwtBearerEvents
    {
      OnAuthenticationFailed = context =>
      {
        if (builder.Environment.IsDevelopment())
          context.Response.Headers["Token-Error"] = "invalid_token";

        return Task.CompletedTask;
      }
    };
  });

builder.Services.AddAuthorization(options =>
{
  options.AddPolicy("TenantsWrite", policy =>
    policy.RequireClaim("permissions", "tenants.write"));

  options.AddPolicy("TenantsRead", policy =>
    policy.RequireClaim("permissions", "tenants.read"));

  options.AddPolicy("StoreWrite", policy =>
    policy.RequireClaim("permissions", "store.write"));

  options.AddPolicy("StoreRead", policy =>
    policy.RequireClaim("permissions", "store.read"));

  options.AddPolicy("CatalogRead", policy =>
    policy.RequireClaim("permissions", "catalog.read"));

  options.AddPolicy("CatalogWrite", policy =>
    policy.RequireClaim("permissions", "catalog.write"));

  options.AddPolicy("OrdersRead", policy =>
    policy.RequireClaim("permissions", "orders.read"));

  options.AddPolicy("OrdersWrite", policy =>
    policy.RequireClaim("permissions", "orders.write"));

  options.AddPolicy("PaymentsRead", policy =>
    policy.RequireClaim("permissions", "payments.read"));

  options.AddPolicy("UsersManage", policy =>
    policy.RequireClaim("permissions", "users.manage"));

  options.AddPolicy("BillingManage", policy =>
    policy.RequireClaim("permissions", "billing.manage"));
});

var app = builder.Build();

// Pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<RequestLoggingScopeMiddleware>();

app.UseCors("EShopyPolicy");

app.Use(async (context, next) =>
{
  context.Response.Headers["X-Content-Type-Options"] = "nosniff";
  context.Response.Headers["X-Frame-Options"] = "DENY";
  context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

  if (!app.Environment.IsDevelopment() && context.Request.IsHttps)
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

  await next();
});

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
