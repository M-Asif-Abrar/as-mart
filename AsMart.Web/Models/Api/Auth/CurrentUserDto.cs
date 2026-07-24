namespace AsMart.Web.Models.Api.Auth
{
    public sealed record CurrentUserDto(
        string Id,
        string? UserName,
        string? Email,
        bool EmailConfirmed,
        IReadOnlyCollection<string> Roles);
}
