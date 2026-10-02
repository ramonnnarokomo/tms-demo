namespace TmsDemo.Api.Domain;

/// <summary>Thrown when an operation would break a business rule. Mapped to HTTP 422.</summary>
public sealed class DomainException(string message) : Exception(message);

/// <summary>Thrown when a requested resource doesn't exist. Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);
