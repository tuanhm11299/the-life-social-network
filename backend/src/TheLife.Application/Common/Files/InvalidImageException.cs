namespace TheLife.Application.Common.Files;

/// <summary>
/// Thrown when an upload passed <see cref="ImageRules"/> (right size, type and first bytes) but turns out
/// to be broken or far too large once the image is actually decoded. The API turns it into a 400 Bad Request.
/// </summary>
public sealed class InvalidImageException(string message) : Exception(message);
