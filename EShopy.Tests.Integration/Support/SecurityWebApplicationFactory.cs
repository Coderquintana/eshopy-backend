using System.Text;
using EShopy.Application.Carts;
using EShopy.Application.Common.Identity;
using EShopy.Application.Common.Stores;
using EShopy.Application.Common.Tenants;
using EShopy.Application.Products;
using EShopy.Application.Subscriptions;
using EShopy.Application.Tenants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace EShopy.Tests.Integration.Support;

public sealed class SecurityWebApplicationFactory : WebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureAppConfiguration((_, configBuilder) =>
    {
      var overrides = new Dictionary<string, string?>
      {
        ["Keycloak:Authority"] = TestJwtTokenFactory.Issuer,
        ["Keycloak:Audience"] = TestJwtTokenFactory.Audience,
        ["Keycloak:RequireHttpsMetadata"] = "false",
        ["Keycloak:ValidateIssuer"] = "true",
        ["Keycloak:ValidateAudience"] = "true",
        ["Keycloak:ValidateLifetime"] = "true"
      };

      configBuilder.AddInMemoryCollection(overrides);
    });

    builder.ConfigureServices(services =>
    {
      services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
      {
        options.Authority = string.Empty;
        options.MetadataAddress = string.Empty;
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = TestJwtTokenFactory.Issuer,
          ValidAudience = TestJwtTokenFactory.Audience,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtTokenFactory.SigningKey)),
          NameClaimType = "preferred_username",
          RoleClaimType = "roles",
          ClockSkew = TimeSpan.Zero
        };
      });

      services.RemoveAll<IProductRepository>();
      services.AddSingleton<IProductRepository, InMemoryProductRepository>();

      services.RemoveAll<ITenantResolver>();
      services.AddSingleton<ITenantResolver, FakeTenantResolver>();

      services.RemoveAll<IStoreService>();
      services.AddSingleton<IStoreService, FakeStoreService>();

      services.AddSingleton<InMemoryTenantsState>();

      services.RemoveAll<ITenantRepository>();
      services.AddSingleton<ITenantRepository, InMemoryTenantRepository>();

      services.RemoveAll<IStoreRepository>();
      services.AddSingleton<IStoreRepository, InMemoryStoreRepository>();

      services.RemoveAll<ITenantUserRepository>();
      services.AddSingleton<ITenantUserRepository, InMemoryTenantUserRepository>();

      services.RemoveAll<ISubscriptionRepository>();
      services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();

      services.RemoveAll<ITenantOnboardingWriter>();
      services.AddSingleton<ITenantOnboardingWriter, InMemoryTenantOnboardingWriter>();

      services.RemoveAll<ITenantActivationWriter>();
      services.AddSingleton<ITenantActivationWriter, InMemoryTenantActivationWriter>();

      services.RemoveAll<IKeycloakUserProvisioner>();
      services.AddSingleton<IKeycloakUserProvisioner, FakeKeycloakUserProvisioner>();

      services.RemoveAll<ICartRepository>();
      services.AddSingleton<ICartRepository, InMemoryCartRepository>();
    });
  }
}
