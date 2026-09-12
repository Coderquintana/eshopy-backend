using EShopy.Api.Common.Json;
using EShopy.Api.DevTools;
using EShopy.Api.Middlewares;
using EShopy.Application.Common.Context;
using EShopy.Infrastructure;
using EShopy.Infrastructure.Identity;
using EShopy.Infrastructure.Persistence;
using EShopy.Infrastructure.Products;
using EShopy.Infrastructure.Tenants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Serilog;

// Bootstrap logger: cubre errores de arranque anteriores a que la configuracion real (leida de
// appsettings, ver seccion "Serilog") este disponible. Patron recomendado por Serilog.AspNetCore.
Log.Logger = new LoggerConfiguration()
  .WriteTo.Console()
  .CreateBootstrapLogger();

try
{
  Log.Information("Iniciando EShopy.Api");

  var builder = WebApplication.CreateBuilder(args);

  builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

  // Controllers + Swagger
  // D-05: convertidores globales de fecha, no tocar cada DTO uno por uno — ver UtcDateTimeConverter.
  // Enums como string: sin este converter, System.Text.Json espera el valor numerico del enum al
  // deserializar (ej. ChangeProductStatusCommand.Status), pero el frontend siempre manda el nombre
  // ("Active") porque asi lo devuelve el propio backend en las respuestas (ProductAdminDto.Status
  // ya es string por mapeo manual). Sin el converter, todo cambio de estado real desde el Admin
  // fallaba con un 400 generico del framework (sin el ErrorResponse propio) — los tests no lo
  // detectaban porque serializan el comando construyendo el record en C#, que serializa el enum
  // como numero y por eso siempre deserializaba bien.
  builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
      options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
      options.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeConverter());
      options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
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

  // B-02: en Development, fallar rapido y claro si faltan migraciones por aplicar, en vez de un
  // error de SQL confuso la primera vez que un request toca una tabla/columna que no existe
  // todavia. En Production queda manual a proposito: un auto-migrate silencioso en el ambiente que
  // importa es mas riesgoso que un paso explicito documentado (ver docs/keycloak-setup.md).
  // Sincrono a proposito (GetPendingMigrations, no *Async): un Main de nivel superior con "await"
  // rompe el mecanismo que usa WebApplicationFactory para interceptar Build() en los tests de
  // integracion (HostFactoryResolver espera un entry point sincrono).
  if (app.Environment.IsDevelopment())
  {
    using var migrationCheckScope = app.Services.CreateScope();
    var db = migrationCheckScope.ServiceProvider.GetRequiredService<EShopyDbContext>();
    var pendingMigrations = db.Database.GetPendingMigrations().ToList();

    if (pendingMigrations.Count > 0)
    {
      throw new InvalidOperationException(
        $"Faltan migraciones por aplicar: {string.Join(", ", pendingMigrations)}. " +
        "Correr: dotnet ef database update --project EShopy.Infrastructure --startup-project EShopy.Api");
    }
  }

  // Modo seed: siembra un tenant de prueba y termina sin levantar el servidor HTTP. Solo en
  // Development a proposito — es un atajo para probar el storefront sin Keycloak, no una feature
  // de producto. Ver DevSeeder para el porque (el onboarding real necesita Keycloak).
  // GetAwaiter().GetResult(), no "await": mismo motivo que el chequeo de migraciones de arriba, un
  // Main de nivel superior async rompe WebApplicationFactory en los tests de integracion.
  if (args.Contains("seed"))
  {
    if (!app.Environment.IsDevelopment())
      throw new InvalidOperationException("El modo seed solo esta disponible en Development.");

    var subdomain = args.SkipWhile(a => a != "seed").Skip(1).FirstOrDefault() ?? DevSeeder.DefaultSubdomain;
    DevSeeder.RunAsync(app.Services, subdomain).GetAwaiter().GetResult();
    return;
  }

  // Pipeline
  // Primero de todos a proposito: envuelve el pipeline completo, asi loguea CUALQUIER respuesta
  // (200, 401, 403, 500) — si fuera despues de UseAuthorization(), una request denegada nunca
  // llegaria a este middleware (el pipeline corta antes). TenantId/UserId se enriquecen via
  // EnrichDiagnosticContext (lee el HttpContext ya resuelto al final de la request, no depende de
  // en que momento del pipeline corre este middleware).
  app.UseSerilogRequestLogging(options =>
  {
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
      var tenantContext = httpContext.RequestServices.GetRequiredService<TenantContext>();
      diagnosticContext.Set("TenantId", tenantContext.TenantId);
      diagnosticContext.Set("Subdomain", tenantContext.Subdomain);

      var user = httpContext.RequestServices.GetRequiredService<UserContextAccessor>().GetUserContext();
      diagnosticContext.Set("UserId", user.UserId);
      diagnosticContext.Set("UserEmail", user.Email);
    };
  });

  app.UseMiddleware<CorrelationIdMiddleware>();
  app.UseMiddleware<GlobalExceptionMiddleware>();
  app.UseMiddleware<TenantResolutionMiddleware>();

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

  var productImagesRoot = ProductImageStoragePaths.ResolveRoot(app.Environment.ContentRootPath);
  Directory.CreateDirectory(productImagesRoot);
  app.UseStaticFiles(new StaticFileOptions
  {
    FileProvider = new PhysicalFileProvider(productImagesRoot),
    RequestPath = ProductImageStoragePaths.RequestPath,
    OnPrepareResponse = context =>
      context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable"
  });

  var storeImagesRoot = StoreImageStoragePaths.ResolveRoot(app.Environment.ContentRootPath);
  Directory.CreateDirectory(storeImagesRoot);
  app.UseStaticFiles(new StaticFileOptions
  {
    FileProvider = new PhysicalFileProvider(storeImagesRoot),
    RequestPath = StoreImageStoragePaths.RequestPath,
    OnPrepareResponse = context =>
      context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable"
  });

  app.UseAuthentication();
  app.UseAuthorization();

  app.UseMiddleware<TenantMembershipMiddleware>();

  // Despues de auth a proposito: enriquece los logs que emiten controllers/handlers (donde vive
  // casi toda la logica de negocio) con UserId/Email ya resueltos — antes de UseAuthentication()
  // el ClaimsPrincipal todavia no esta poblado.
  app.UseMiddleware<RequestLoggingScopeMiddleware>();

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
}
catch (Exception ex) when (ex is not HostAbortedException)
{
  // HostAbortedException: lanzada por WebApplicationFactory en tests de integracion al construir
  // el host sin correrlo — no es un fallo real de arranque, no debe loguearse como fatal.
  Log.Fatal(ex, "EShopy.Api termino inesperadamente durante el arranque");
}
finally
{
  Log.CloseAndFlush();
}

public partial class Program { }
