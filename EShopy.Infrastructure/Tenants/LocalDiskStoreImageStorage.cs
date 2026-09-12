using EShopy.Application.Tenants;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace EShopy.Infrastructure.Tenants;

public sealed class LocalDiskStoreImageStorage(IHostEnvironment environment) : IStoreImageStorage
{
  private readonly string _rootPath = StoreImageStoragePaths.ResolveRoot(environment.ContentRootPath);

  public async Task<string> SaveAsync(
    Guid storeId,
    StoreImageKind kind,
    Stream content,
    string fileName,
    string contentType,
    CancellationToken ct)
  {
    if (!content.CanRead || string.IsNullOrWhiteSpace(fileName))
      throw new InvalidDataException("Seleccioná una imagen válida.");

    if (!StoreImageConstraints.IsAllowedContentType(contentType))
      throw new InvalidDataException("Usá una imagen JPEG, PNG o WebP.");

    await using var buffer = new MemoryStream();
    await CopyWithLimitAsync(content, buffer, ct);
    buffer.Position = 0;

    try
    {
      var format = await Image.DetectFormatAsync(buffer, ct);
      if (format is not JpegFormat and not PngFormat and not WebpFormat)
        throw new InvalidDataException("El contenido del archivo no es una imagen JPEG, PNG o WebP.");

      buffer.Position = 0;
      using var image = await Image.LoadAsync(buffer, ct);
      image.Mutate(context => context.AutoOrient());

      var maxDimension = kind == StoreImageKind.Logo
        ? StoreImageConstraints.LogoMaxDimensionPixels
        : StoreImageConstraints.HeroMaxDimensionPixels;
      if (image.Width > maxDimension || image.Height > maxDimension)
      {
        image.Mutate(context => context.Resize(new ResizeOptions
        {
          Mode = ResizeMode.Max,
          Size = new Size(maxDimension, maxDimension)
        }));
      }

      var storeDirectory = Path.Combine(_rootPath, storeId.ToString("N"));
      Directory.CreateDirectory(storeDirectory);
      var storedFileName = $"{kind.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}.webp";
      var finalPath = Path.Combine(storeDirectory, storedFileName);
      var temporaryPath = $"{finalPath}.tmp";

      try
      {
        await image.SaveAsWebpAsync(temporaryPath, new WebpEncoder { Quality = 85 }, ct);
        File.Move(temporaryPath, finalPath);
      }
      finally
      {
        if (File.Exists(temporaryPath))
          File.Delete(temporaryPath);
      }

      return $"{StoreImageStoragePaths.RequestPath}/{storeId:N}/{storedFileName}";
    }
    catch (UnknownImageFormatException ex)
    {
      throw new InvalidDataException("El contenido del archivo no es una imagen válida.", ex);
    }
    catch (InvalidImageContentException ex)
    {
      throw new InvalidDataException("La imagen está dañada o no se puede leer.", ex);
    }
  }

  public Task DeleteAsync(Guid storeId, string imageUrl, CancellationToken ct)
  {
    var storeRequestPath = $"{StoreImageStoragePaths.RequestPath}/{storeId:N}/";
    if (!imageUrl.StartsWith(storeRequestPath, StringComparison.OrdinalIgnoreCase))
      return Task.CompletedTask;

    try
    {
      var fileName = imageUrl[storeRequestPath.Length..];
      var storeRoot = Path.GetFullPath(Path.Combine(_rootPath, storeId.ToString("N")));
      var fullPath = Path.GetFullPath(Path.Combine(storeRoot, fileName));
      var normalizedRoot = storeRoot + Path.DirectorySeparatorChar;
      if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        return Task.CompletedTask;

      if (File.Exists(fullPath))
        File.Delete(fullPath);
    }
    catch (IOException)
    {
      // Best-effort: un archivo anterior huérfano no debe revertir la configuración guardada.
    }
    catch (UnauthorizedAccessException)
    {
      // Best-effort por el mismo motivo.
    }
    catch (ArgumentException)
    {
      // Una URL histórica malformada tampoco debe impedir guardar la configuración nueva.
    }

    return Task.CompletedTask;
  }

  private static async Task CopyWithLimitAsync(Stream source, Stream destination, CancellationToken ct)
  {
    var readBuffer = new byte[81920];
    long total = 0;
    int read;
    while ((read = await source.ReadAsync(readBuffer, ct)) > 0)
    {
      total += read;
      if (total > StoreImageConstraints.MaxFileBytes)
        throw new InvalidDataException("La imagen no puede superar 5 MB.");

      await destination.WriteAsync(readBuffer.AsMemory(0, read), ct);
    }
  }
}
