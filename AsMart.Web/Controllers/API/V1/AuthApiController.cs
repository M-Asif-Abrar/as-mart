using AsMart.Web.Models.Api.Auth;
using AsMart.Web.Models.Entities;
using AsMart.Web.Services.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AsMart.Web.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/auth")]
    [Produces("application/json")]
    public sealed class AuthApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshTokenService _refreshTokenService;

        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwtTokenService,
            IRefreshTokenService refreshTokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _refreshTokenService = refreshTokenService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(
            typeof(AuthApiResponse<AuthTokenResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(AuthApiResponse<object>),
            StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto request,
            CancellationToken cancellationToken)
        {
            var normalizedEmail =
                _userManager.NormalizeEmail(request.Email.Trim());

            var user = await _userManager.Users
                .SingleOrDefaultAsync(
                    x => x.NormalizedEmail == normalizedEmail,
                    cancellationToken);

            if (user is null)
            {
                return InvalidCredentials();
            }

            var result =
                await _signInManager.CheckPasswordSignInAsync(
                    user,
                    request.Password,
                    lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return StatusCode(
                        StatusCodes.Status423Locked,
                        AuthApiResponse<object>.Fail(
                            "account_locked",
                            "The account is temporarily locked.",
                            HttpContext.TraceIdentifier));
                }

                if (result.IsNotAllowed)
                {
                    return Unauthorized(
                        AuthApiResponse<object>.Fail(
                            "login_not_allowed",
                            "Login is not allowed. Confirm the account and try again.",
                            HttpContext.TraceIdentifier));
                }

                return InvalidCredentials();
            }

            var response = await CreateAuthResponseAsync(
                user,
                cancellationToken);

            Response.Headers.CacheControl = "no-store";

            return Ok(
                AuthApiResponse<AuthTokenResponseDto>.Ok(
                    response,
                    HttpContext.TraceIdentifier));
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(
            typeof(AuthApiResponse<AuthTokenResponseDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(AuthApiResponse<object>),
            StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshTokenRequestDto request,
            CancellationToken cancellationToken)
        {
            var rotation =
                await _refreshTokenService.RotateAsync(
                    request.RefreshToken,
                    GetClientIp(),
                    Request.Headers.UserAgent.ToString(),
                    cancellationToken);

            if (rotation is null)
            {
                return Unauthorized(
                    AuthApiResponse<object>.Fail(
                        "invalid_refresh_token",
                        "The refresh token is invalid, expired, or revoked.",
                        HttpContext.TraceIdentifier));
            }

            var accessToken =
                await _jwtTokenService.CreateAccessTokenAsync(
                    rotation.User,
                    cancellationToken);

            var user = await CreateCurrentUserAsync(
                rotation.User);

            var response = new AuthTokenResponseDto(
                "Bearer",
                accessToken.AccessToken,
                accessToken.ExpiresAtUtc,
                rotation.RawToken,
                rotation.ExpiresAtUtc,
                user);

            Response.Headers.CacheControl = "no-store";

            return Ok(
                AuthApiResponse<AuthTokenResponseDto>.Ok(
                    response,
                    HttpContext.TraceIdentifier));
        }

        [HttpPost("logout")]
        [Authorize(
            AuthenticationSchemes =
                JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            typeof(AuthApiResponse<object>),
            StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(
            [FromBody] RefreshTokenRequestDto request,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(
                    AuthApiResponse<object>.Fail(
                        "unauthorized",
                        "A valid bearer access token is required.",
                        HttpContext.TraceIdentifier));
            }

            await _refreshTokenService.RevokeAsync(
                request.RefreshToken,
                userId,
                GetClientIp(),
                cancellationToken);

            return NoContent();
        }

        [HttpGet("me")]
        [Authorize(
            AuthenticationSchemes =
                JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(
            typeof(AuthApiResponse<CurrentUserDto>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(
                userId);

            if (user is null)
            {
                return Unauthorized();
            }

            var response = await CreateCurrentUserAsync(user);

            return Ok(
                AuthApiResponse<CurrentUserDto>.Ok(
                    response,
                    HttpContext.TraceIdentifier));
        }

        private async Task<AuthTokenResponseDto>
            CreateAuthResponseAsync(
                ApplicationUser user,
                CancellationToken cancellationToken)
        {
            var accessToken =
                await _jwtTokenService.CreateAccessTokenAsync(
                    user,
                    cancellationToken);

            var refreshToken =
                await _refreshTokenService.CreateAsync(
                    user.Id,
                    GetClientIp(),
                    Request.Headers.UserAgent.ToString(),
                    cancellationToken);

            var currentUser = await CreateCurrentUserAsync(user);

            return new AuthTokenResponseDto(
                "Bearer",
                accessToken.AccessToken,
                accessToken.ExpiresAtUtc,
                refreshToken.RawToken,
                refreshToken.ExpiresAtUtc,
                currentUser);
        }

        private async Task<CurrentUserDto>
            CreateCurrentUserAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new CurrentUserDto(
                user.Id,
                user.UserName,
                user.Email,
                user.EmailConfirmed,
                roles.OrderBy(x => x).ToArray());
        }

        private IActionResult InvalidCredentials()
        {
            return Unauthorized(
                AuthApiResponse<object>.Fail(
                    "invalid_credentials",
                    "The email address or password is incorrect.",
                    HttpContext.TraceIdentifier));
        }

        private string? GetClientIp()
        {
            return HttpContext.Connection
                .RemoteIpAddress?
                .ToString();
        }
    }
}
