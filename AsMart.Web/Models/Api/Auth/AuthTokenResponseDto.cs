namespace AsMart.Web.Models.Api.Auth
{
    public sealed record AuthTokenResponseDto(
        string TokenType,
        string AccessToken,
        DateTime AccessTokenExpiresAtUtc,
        string RefreshToken,
        DateTime RefreshTokenExpiresAtUtc,
        CurrentUserDto User);
}
