using System.Text.Json;
using System.Text.Json.Serialization;

namespace EShopy.Api.Common.Json;

/// <summary>
/// Fuerza <see cref="DateTimeKind.Utc"/> en cada fecha que sale por la API, sin importar el Kind con
/// el que EF la haya devuelto.
///
/// Las columnas <c>datetime2</c> no guardan el offset, asi que EF Core devuelve
/// <see cref="DateTimeKind.Unspecified"/> aunque el valor guardado siempre sea UTC (convencion del
/// proyecto). Sin este converter, <c>System.Text.Json</c> serializa esas fechas sin el sufijo "Z"
/// (ej. "2026-09-06T18:51:43.81" en vez de "2026-09-06T18:51:43.81Z"), y el frontend las interpreta
/// como hora local del navegador (D-05 en agents/backend/BACKLOG.md).
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
  public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    => AsUtc(reader.GetDateTime());

  public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    => writer.WriteStringValue(AsUtc(value));

  internal static DateTime AsUtc(DateTime value) => value.Kind switch
  {
    DateTimeKind.Utc => value,
    DateTimeKind.Local => value.ToUniversalTime(),
    _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
  };
}

/// <summary>Misma normalizacion que <see cref="UtcDateTimeConverter"/>, para propiedades <c>DateTime?</c>.</summary>
public sealed class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
{
  public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    => reader.TokenType == JsonTokenType.Null ? null : UtcDateTimeConverter.AsUtc(reader.GetDateTime());

  public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
  {
    if (value is null)
      writer.WriteNullValue();
    else
      writer.WriteStringValue(UtcDateTimeConverter.AsUtc(value.Value));
  }
}
