using EShopy.Application.Common.Tenants;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Tenants;

public sealed class SubdomainResolverTests
{
  [Fact]
  public void Extract_Localhost_ReturnsLocalhost()
  {
    SubdomainResolver.Extract("localhost").Should().Be("localhost");
  }

  [Fact]
  public void Extract_LocalhostCaseInsensitive_ReturnsLocalhost()
  {
    SubdomainResolver.Extract("LOCALHOST").Should().Be("localhost");
  }

  [Fact]
  public void Extract_SimpleTenantSubdomain_ReturnsTenant()
  {
    SubdomainResolver.Extract("demo.eshopy.com.py").Should().Be("demo");
  }

  [Fact]
  public void Extract_AdminPrefixedHost_ReturnsTenantAfterAdmin()
  {
    SubdomainResolver.Extract("admin.demo.eshopy.com.py").Should().Be("demo");
  }

  [Fact]
  public void Extract_TwoLabelHost_ReturnsFirstLabel()
  {
    // Menos de 3 labels: no hay suficiente informacion para distinguir subdominio de dominio raiz.
    SubdomainResolver.Extract("eshopy.com").Should().Be("eshopy");
  }

  [Fact]
  public void Extract_EmptyHost_ReturnsEmpty()
  {
    SubdomainResolver.Extract("").Should().Be("");
  }

  [Fact]
  public void Extract_NullHost_ReturnsEmpty()
  {
    SubdomainResolver.Extract(null!).Should().Be("");
  }

  [Fact]
  public void Extract_WhitespaceHost_ReturnsEmpty()
  {
    SubdomainResolver.Extract("   ").Should().Be("");
  }

  [Fact]
  public void Extract_AdminWithoutFollowingLabel_FallsBackToAdmin()
  {
    // "admin" seguido de menos de 2 labels totales cae en la rama < 3 antes de llegar al chequeo de "admin".
    SubdomainResolver.Extract("admin.localhost").Should().Be("admin");
  }
}
