using System.Text.Json.Serialization;

namespace SafePoint_IRS.DTOs;

public record SendOtpRequest(
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("type")] string? Type
);
//for emails