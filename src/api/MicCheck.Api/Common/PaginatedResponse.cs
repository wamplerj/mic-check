namespace MicCheck.Api.Common;

public record PaginatedResponse<T>(int Count, string? Next, string? Previous, IReadOnlyList<T> Results);
