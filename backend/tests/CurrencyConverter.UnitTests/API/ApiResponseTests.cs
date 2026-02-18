using CurrencyConverter.API.Models;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.API;

public class ApiResponseTests
{
    [Fact]
    public void Success_ShouldSetDataAndMetadata()
    {
        var metadata = new Dictionary<string, object> { ["page"] = 1 };
        var response = ApiResponse<string>.Success("test", metadata);

        response.Data.Should().Be("test");
        response.Metadata.Should().ContainKey("page");
    }

    [Fact]
    public void Success_ShouldWorkWithoutMetadata()
    {
        var response = ApiResponse<int>.Success(42);

        response.Data.Should().Be(42);
        response.Metadata.Should().BeNull();
    }

    [Fact]
    public void ErrorResponse_ShouldHaveDefaultValues()
    {
        var errorResponse = new ErrorResponse();

        errorResponse.Type.Should().Be(string.Empty);
        errorResponse.Title.Should().Be(string.Empty);
        errorResponse.Status.Should().Be(0);
        errorResponse.Detail.Should().Be(string.Empty);
        errorResponse.Errors.Should().BeNull();
    }

    [Fact]
    public void ErrorResponse_ShouldSetProperties()
    {
        var errorResponse = new ErrorResponse
        {
            Type = "ValidationError",
            Title = "Bad Request",
            Status = 400,
            Detail = "Validation failed",
            Errors = new Dictionary<string, string[]>
            {
                ["Field1"] = new[] { "Required" }
            }
        };

        errorResponse.Type.Should().Be("ValidationError");
        errorResponse.Title.Should().Be("Bad Request");
        errorResponse.Status.Should().Be(400);
        errorResponse.Detail.Should().Be("Validation failed");
        errorResponse.Errors.Should().ContainKey("Field1");
    }

    [Fact]
    public void ErrorResponse_Create_ShouldSetCorrectStatusAndTitle()
    {
        var errorResponse = ErrorResponse.Create(400, "Some detail");

        errorResponse.Status.Should().Be(400);
        errorResponse.Title.Should().Be("Bad Request");
        errorResponse.Detail.Should().Be("Some detail");
    }

    [Fact]
    public void ErrorResponse_NotFound_ShouldReturn404()
    {
        var errorResponse = ErrorResponse.NotFound("User not found.");

        errorResponse.Status.Should().Be(404);
        errorResponse.Title.Should().Be("Not Found");
        errorResponse.Detail.Should().Be("User not found.");
    }

    [Fact]
    public void ErrorResponse_BadRequest_ShouldReturn400()
    {
        var errorResponse = ErrorResponse.BadRequest("Invalid input.");

        errorResponse.Status.Should().Be(400);
        errorResponse.Title.Should().Be("Bad Request");
        errorResponse.Detail.Should().Be("Invalid input.");
    }
}
