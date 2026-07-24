using AsMart.Web.Models.Entities;
using AsMart.Web.Models.Security;

namespace AsMart.Web.Services.Security
{
    public interface IJwtTokenService
    {
        Task<JwtTokenResult> CreateAccessTokenAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default);
    }
}
