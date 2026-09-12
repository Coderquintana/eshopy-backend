namespace EShopy.Application.Products.Commands;

public sealed record UploadProductImageCommand(
  Guid Id,
  Stream Content,
  string FileName,
  string ContentType,
  long Length,
  string RowVersion);
