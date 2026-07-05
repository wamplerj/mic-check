namespace MicCheck.Api.Common;

public record PagedResult<T>(int Total, IReadOnlyList<T> Items);
