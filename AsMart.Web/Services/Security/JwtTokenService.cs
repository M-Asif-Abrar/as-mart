using AsMart.Web.Models.Entities;
using AsMart.Web.Models.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace AsMart.Web.Services.Security
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtOptions _options;
        private readonly TimeProvider _timeProvider;

        public JwtTokenService(
            UserManager<ApplicationUser> userManager,
            IOptions<JwtOptions> options,
            TimeProvider timeProvider)
        {
            _userManager = userManager;
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public async Task<JwtTokenResult> CreateAccessTokenAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            cancellationToken.ThrowIfCancellationRequested();

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var expiresAtUtc = nowUtc.AddMinutes(
                _options.AccessTokenMinutes);

            var claims = new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    userId),

                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString("N")),

                new(
                    JwtRegisteredClaimNames.Iat,
                    EpochTime.GetIntDate(nowUtc).ToString(),
                    ClaimValueTypes.Integer64),

                new(
                    ClaimTypes.NameIdentifier,
                    userId)
            };

            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Name,
                        user.UserName));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(
                    new Claim(
                        JwtRegisteredClaimNames.Email,
                        email));

                claims.Add(
                    new Claim(
                        ClaimTypes.Email,
                        email));
            }

            foreach (var role in roles.Distinct(
                         StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        role));
            }

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _options.SigningKey));

            var signingCredentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Subject = new ClaimsIdentity(claims),
                NotBefore = nowUtc,
                IssuedAt = nowUtc,
                Expires = expiresAtUtc,
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JsonWebTokenHandler();

            var accessToken = tokenHandler.CreateToken(
                tokenDescriptor);

            return new JwtTokenResult(
                accessToken,
                expiresAtUtc);
        }
    }
}
