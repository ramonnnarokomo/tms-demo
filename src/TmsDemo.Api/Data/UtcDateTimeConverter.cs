using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TmsDemo.Api.Data;

/// <summary>Saves DateTime values as UTC and marks them as UTC when reading them back.</summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
