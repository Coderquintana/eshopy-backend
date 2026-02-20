namespace EShopy.Domain.Common.Entities;

public abstract class AppEntity
{
  protected AppEntity(Guid id,
    Guid tenantId,
    DateTime createdAtUtc,
    string? createdBy,
    DateTime? updatedAtUtc,
    string? updatedBy,
    string? data)
  {
    Id = id;
    TenantId = tenantId;
    CreatedAtUtc = createdAtUtc;
    CreatedBy = createdBy;
    UpdatedAtUtc = updatedAtUtc;
    UpdatedBy = updatedBy;
    Data = data;
  }

  public Guid Id { get; protected set; }
  public Guid TenantId { get; protected set; }
  public DateTime CreatedAtUtc { get; protected set; }
  public string? CreatedBy { get; protected set; }
  public DateTime? UpdatedAtUtc { get; protected set; }
  public string? UpdatedBy { get; protected set; }
  public byte[]? RowVersion { get; protected set; }
  public string? Data { get; protected set; }

  protected T? GetData<T>()
  {
    if (string.IsNullOrWhiteSpace(Data))
      return default;

    return System.Text.Json.JsonSerializer.Deserialize<T>(Data);
  }

  protected void SetData<T>(T data)
  {
    Data = System.Text.Json.JsonSerializer.Serialize(data);
  }
}
