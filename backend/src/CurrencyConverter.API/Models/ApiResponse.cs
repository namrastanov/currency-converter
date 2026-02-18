namespace CurrencyConverter.API.Models;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public static ApiResponse<T> Success(T data, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse<T> { Data = data, Metadata = metadata };
    }
}

public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }

    public static ErrorResponse Create(int status, string detail, string? errorCode = null)
    {
        var (type, title) = status switch
        {
            400 => ("https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request"),
            401 => ("https://tools.ietf.org/html/rfc7231#section-6.5.2", "Unauthorized"),
            404 => ("https://tools.ietf.org/html/rfc7231#section-6.5.4", "Not Found"),
            409 => ("https://tools.ietf.org/html/rfc7231#section-6.5.8", "Conflict"),
            _ => ("https://tools.ietf.org/html/rfc7231#section-6.6.1", "Internal Server Error")
        };

        return new ErrorResponse
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
            Errors = errorCode is not null ? new Dictionary<string, string[]> { [errorCode] = [detail] } : null
        };
    }

    public static ErrorResponse NotFound(string detail) => Create(404, detail);
    public static ErrorResponse BadRequest(string detail) => Create(400, detail);
}
