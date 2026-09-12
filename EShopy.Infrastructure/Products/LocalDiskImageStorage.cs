using EShopy.Application.Products;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace EShopy.Infrastructure.Products;

public sealed class LocalDiskImageStorage(IHostEnvironment environment) : IProductImageStorage
{
  private readonly string _rootPath = ProductImageStoragePaths.ResolveRoot(environment.ContentRootPath);

  public async Task<string> SaveAsync(
    Stream content,
    string fileName,
    string contentType,
    CancellationToken ct)
  {
    if (!content.CanRead || string.IsNullOrWhiteSpace(fileName))
      throw new InvalidDataException("Seleccioná una imagen válida.");

    if (!ProductImageConstraints.IsAllowedContentType(contentType))
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

      if (image.Width > ProductImageConstraints.MaxDimensionPixels ||
          image.Height > ProductImageConstraints.MaxDimensionPixels)
      {
        image.Mutate(context => context.Resize(new ResizeOptions
        {
          Mode = ResizeMode.Max,
          Size = new Size(
            ProductImageConstraints.MaxDimensionPixels,
            ProductImageConstraints.MaxDimensionPixels)
        }));
      }

      Directory.CreateDirectory(_rootPath);
      var storedFileName = $"{Guid.NewGuid():N}.webp";
      var finalPath = Path.Combine(_rootPath, storedFileName);
      var temporaryPath = $"{finalPath}.tmp";

      try
      {
        await image.SaveAsWebpAsync(
          temporaryPath,
          new WebpEncoder { Quality = 85 },
          ct);
        File.Move(temporaryPath, finalPath);
      }
      finally
      {
        if (File.Exists(temporaryPath))
          File.Delete(temporaryPath);
      }

      return $"{ProductImageStoragePaths.RequestPath}/{storedFileName}";
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

  public Task DeleteAsync(string imageUrl, CancellationToken ct)
  {
    if (!imageUrl.StartsWith($"{ProductImageStoragePaths.RequestPath}/", StringComparison.Ordinal))
      return Task.CompletedTask;

    var fileName = Path.GetFileName(imageUrl);
    if (string.IsNullOrWhiteSpace(fileName))
      return Task.CompletedTask;

    try
    {
      var path = Path.Combine(_rootPath, fileName);
      if (File.Exists(path))
        File.Delete(path);
    }
    catch (IOException)
    {
      // Best-effort: una imagen anterior huérfana no debe revertir el producto ya guardado.
    }
    catch (UnauthorizedAccessException)
    {
      // Best-effort por el mismo motivo; la siguiente limpieza operativa puede retirarla.
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
      if (total > ProductImageConstraints.MaxFileBytes)
        throw new InvalidDataException("La imagen no puede superar 5 MB.");

      await destination.WriteAsync(readBuffer.AsMemory(0, read), ct);
    }
  }
}
