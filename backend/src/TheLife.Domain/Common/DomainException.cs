namespace TheLife.Domain.Common;

/// <summary>
/// Thrown when code tries to put an entity into an invalid state
/// (for example a post without images, or a user following themselves).
/// Request validators normally catch these cases first; this is the last line of defence.
/// The API turns it into a "400 Bad Request" response.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
