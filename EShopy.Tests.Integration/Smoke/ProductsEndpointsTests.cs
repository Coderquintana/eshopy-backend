using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Products.Commands;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Products;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class ProductsEndpointsTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public ProductsEndpointsTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task ProductsFlow_ShouldCreateAndExposePublicProduct()
  {
    var client = CreateAuthorizedClient();
    var createCommand = new CreateProductCommand(
      Slug: $"coffee-mug-{Guid.NewGuid():N}"[..24],
      Sku: null,
      Name: "Coffee Mug",
      Description: "Ceramic",
      Price: 12.5m,
      StockOnHand: 5);

    var createResponse = await client.PostAsJsonAsync("/api/products", new[] { createCommand });
    createResponse.IsSuccessStatusCode.Should().BeTrue();

    var created = (await createResponse.Content.ReadFromJsonAsync<IReadOnlyList<ProductAdminDto>>())!.Single();
    created.RowVersion.Should().NotBeNullOrWhiteSpace();

    var statusCommand = new ChangeProductStatusCommand(created.Id, ProductStatus.Active, created.RowVersion);
    var statusResponse = await client.PatchAsync($"/api/products/{created.Id}/status", JsonContent.Create(statusCommand));
    statusResponse.IsSuccessStatusCode.Should().BeTrue();

    var publicResponse = await client.GetAsync("/api/public/products");
    publicResponse.IsSuccessStatusCode.Should().BeTrue();

    var paged = await publicResponse.Content.ReadFromJsonAsync<PagedResult<ProductPublicDto>>();
    paged.Should().NotBeNull();
    paged!.Items.Should().ContainSingle(item => item.Id == created.Id);
  }

  [Fact]
  public async Task CreateProducts_ShouldPersistTheWholeBatchAtomically()
  {
    var client = CreateAuthorizedClient();
    var prefix = Guid.NewGuid().ToString("N")[..8];
    var batch = new[]
    {
      new CreateProductCommand($"batch-{prefix}-a", null, "Batch A", null, 10m, 1),
      new CreateProductCommand($"batch-{prefix}-b", null, "Batch B", null, 20m, 2),
      new CreateProductCommand($"batch-{prefix}-c", null, "Batch C", null, 30m, 3)
    };

    var response = await client.PostAsJsonAsync("/api/products", batch);
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    var created = await response.Content.ReadFromJsonAsync<IReadOnlyList<ProductAdminDto>>();
    created.Should().HaveCount(3);
    created!.Select(p => p.Slug).Should().BeEquivalentTo(batch.Select(b => b.Slug));
  }

  [Fact]
  public async Task CreateProducts_WithDuplicateSlugWithinTheBatch_ShouldRejectAllOfIt()
  {
    var client = CreateAuthorizedClient();
    var slug = $"dup-{Guid.NewGuid():N}"[..20];
    var batch = new[]
    {
      new CreateProductCommand(slug, null, "First", null, 10m, 1),
      new CreateProductCommand(slug, null, "Second", null, 20m, 2)
    };

    var response = await client.PostAsJsonAsync("/api/products", batch);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    // Confirmar que el lote rechazado no dejo nada creado a medias.
    var found = await client.GetAsync("/api/products?page=1&pageSize=50");
    var paged = await found.Content.ReadFromJsonAsync<PagedResult<ProductAdminDto>>();
    paged!.Items.Should().NotContain(p => p.Slug == slug);
  }

  [Fact]
  public async Task UpdateProduct_WithStaleRowVersion_ShouldReturn409()
  {
    var client = CreateAuthorizedClient();
    var created = await CreateProductAsync(client);

    var firstUpdate = new UpdateProductCommand(
      created.Id, "First update", null, 15m, 5, null, created.RowVersion);
    var firstResponse = await client.PutAsJsonAsync("/api/products", new[] { firstUpdate });
    firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var updated = (await firstResponse.Content.ReadFromJsonAsync<IReadOnlyList<ProductAdminDto>>())!.Single();
    updated.RowVersion.Should().NotBe(created.RowVersion);

    var staleUpdate = new UpdateProductCommand(
      created.Id, "Stale update", null, 20m, 5, null, created.RowVersion);
    var staleResponse = await client.PutAsJsonAsync("/api/products", new[] { staleUpdate });

    staleResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task UpdateProducts_WhenOneItemInTheBatchIsStale_ShouldNotApplyAnyOfThem()
  {
    var client = CreateAuthorizedClient();
    var freshOne = await CreateProductAsync(client);
    var staleOne = await CreateProductAsync(client);

    // Dejar a `staleOne` desactualizado con un update previo exitoso
    var priorUpdate = new UpdateProductCommand(staleOne.Id, "Renamed once", null, staleOne.Price, staleOne.StockOnHand, null, staleOne.RowVersion);
    await client.PutAsJsonAsync("/api/products", new[] { priorUpdate });

    var batch = new[]
    {
      new UpdateProductCommand(freshOne.Id, "Fresh renamed", null, 99m, 9, null, freshOne.RowVersion),
      new UpdateProductCommand(staleOne.Id, "Stale renamed", null, 88m, 8, null, staleOne.RowVersion) // RowVersion vieja a proposito
    };

    var response = await client.PutAsJsonAsync("/api/products", batch);
    response.StatusCode.Should().Be(HttpStatusCode.Conflict);

    var reread = await client.GetAsync($"/api/products/{freshOne.Id}");
    var stillFresh = await reread.Content.ReadFromJsonAsync<ProductAdminDto>();
    stillFresh!.Name.Should().Be(freshOne.Name); // el item "bueno" del lote NO se aplico
  }

  [Fact]
  public async Task ChangeStatus_WithStaleRowVersion_ShouldReturn409()
  {
    var client = CreateAuthorizedClient();
    var created = await CreateProductAsync(client);

    var activate = new ChangeProductStatusCommand(created.Id, ProductStatus.Active, created.RowVersion);
    var activateResponse = await client.PatchAsync(
      $"/api/products/{created.Id}/status",
      JsonContent.Create(activate));
    activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var staleArchive = new ChangeProductStatusCommand(created.Id, ProductStatus.Archived, created.RowVersion);
    var staleResponse = await client.PatchAsync(
      $"/api/products/{created.Id}/status",
      JsonContent.Create(staleArchive));

    staleResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  /// <summary>
  /// Bug real (encontrado en vivo desde el Admin, 2026-09-12): los tests anteriores serializan
  /// <see cref="ChangeProductStatusCommand"/> construyendo el record en C#, y
  /// <c>System.Text.Json</c> sin converter serializa un enum como su valor numerico (ej. 1), no
  /// como texto. El Admin real manda el nombre del estado ("Active"), como cualquier cliente HTTP
  /// ajeno al tipo C# del backend — sin un <c>JsonStringEnumConverter</c> global, esa forma nunca
  /// se probaba y el bind fallaba con el 400 generico de ASP.NET (sin el <c>ErrorResponse</c> propio),
  /// publicar un producto rompia siempre. Este test manda el mismo shape que el navegador.
  /// </summary>
  [Fact]
  public async Task ChangeStatus_WithStringEnumBody_ShouldSucceed()
  {
    var client = CreateAuthorizedClient();
    var created = await CreateProductAsync(client);

    var response = await client.PatchAsync(
      $"/api/products/{created.Id}/status",
      JsonContent.Create(new { status = "Active", rowVersion = created.RowVersion }));

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var updated = await response.Content.ReadFromJsonAsync<ProductAdminDto>();
    updated!.Status.Should().Be("Active");
  }

  [Fact]
  public async Task UpdateProduct_WithoutRowVersion_ShouldReturn400()
  {
    var client = CreateAuthorizedClient();
    var created = await CreateProductAsync(client);

    var response = await client.PutAsJsonAsync(
      "/api/products",
      new[]
      {
        new
        {
          id = created.Id,
          name = "Missing version",
          description = (string?)null,
          price = 10m,
          stockOnHand = 5,
          sku = (string?)null
        }
      });

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  private HttpClient CreateAuthorizedClient()
  {
    var client = _factory.CreateClient();
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["catalog.read", "catalog.write"],
      roles: ["TENANT_ADMIN"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client;
  }

  private static async Task<ProductAdminDto> CreateProductAsync(HttpClient client)
  {
    var command = new CreateProductCommand(
      $"concurrency-{Guid.NewGuid():N}"[..28],
      null,
      "Concurrency Product",
      null,
      10m,
      5);

    var response = await client.PostAsJsonAsync("/api/products", new[] { command });
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<IReadOnlyList<ProductAdminDto>>())!.Single();
  }
}
