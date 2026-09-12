using EShopy.Application.Tenants;

namespace EShopy.Application.Tenants.Commands;

public sealed record UploadStoreImageCommand(
  StoreImageKind Kind,
  Stream Content,
  string FileName,
  string ContentType,
  long Length);
