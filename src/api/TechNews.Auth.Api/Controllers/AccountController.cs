using System.Net;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TechNews.Auth.Api.Data;
using TechNews.Auth.Api.Models;
using TechNews.Auth.Api.Configurations;
using TechNews.Common.Library.Models;
using TechNews.Auth.Api.Services.KeyRetrievers;

namespace TechNews.Auth.Api.Controllers;

[Route("api/auth/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ICryptographicKeyRetriever _cryptographicKeyRetriever;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, ICryptographicKeyRetriever cryptographicKeyRetriever)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _cryptographicKeyRetriever = cryptographicKeyRetriever;
    }

    /// <summary>
    /// Confirm the user email
    /// </summary>
    /// <param name="requestModel">The confirm email information</param>
    /// <response code="200">Account email confirmed successfully</response>
    /// <response code="400">There is a problem with the request</response>
    /// <response code="404">The user could not be found</response>
    /// <response code="500">There was an internal problem</response>
    [HttpPost("confirmation")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.InternalServerError)] //TODO: fazer testes
    public async Task<IActionResult> ConfirmEmailAsync([FromBody] ConfirmEmailRequestModel requestModel)
    {
        var registeredUserResult = await _userManager.FindByEmailAsync(requestModel.Email);

        if (registeredUserResult is null)
            return NotFound(new ApiResponse(error: new ErrorResponse("invalid_request", "UserNotFound", "The user was not found")));

        if (string.IsNullOrEmpty(requestModel.Token) || string.IsNullOrWhiteSpace(requestModel.Token))
            return BadRequest(new ApiResponse(error: new ErrorResponse("invalid_request", "TokenRequired", "The confirmation token is required")));

        var result = await _userManager.ConfirmEmailAsync(registeredUserResult, requestModel.Token);

        if (!result.Succeeded)
            return BadRequest(new ApiResponse(errors: result.Errors.ToList().ConvertAll(x => new ErrorResponse("invalid_request", x.Code, x.Description))));

        return Ok(new ApiResponse());
    }

    /// <summary>
    /// Login an user
    /// </summary>
    /// <param name="user">The user to be logged in</param>
    /// <response code="201">Returns the user login data</response>
    /// <response code="400">There is a problem with the request</response>
    /// <response code="404">The informed user was not found</response>
    /// <response code="500">There was an internal problem</response>
    [HttpPost("login")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestModel user)
    {
        var registeredUserResult = await _userManager.FindByEmailAsync(user.Email);

        if (registeredUserResult?.UserName is null)
            return BadRequest(new ApiResponse(error: new ErrorResponse("invalid_request", "InvalidRequest", "User or password are invalid")));

        if (!registeredUserResult.EmailConfirmed)
            return StatusCode((int)HttpStatusCode.Forbidden, new ApiResponse(error: new ErrorResponse("unauthorized_client", "EmailNotConfirmed", "User email not confirmed")));

        var signInResult = await _signInManager.PasswordSignInAsync(registeredUserResult.UserName, user.Password, false, true);

        if (signInResult.IsLockedOut)
            return StatusCode((int)HttpStatusCode.Forbidden, new ApiResponse(error: new ErrorResponse("unauthorized_client", "LockedUser", "User temporary blocked for invalid attempts")));

        if (!signInResult.Succeeded)
            return BadRequest(new ApiResponse(error: new ErrorResponse("invalid_request", "InvalidRequest", "User or password are invalid")));

        var claims = await GetUserClaims(registeredUserResult);

        var token = await GetTokenAsync(claims, registeredUserResult);

        if (token is null)
            return StatusCode((int)HttpStatusCode.InternalServerError, new ApiResponse(error: new ErrorResponse("server_error", "InternalError", "There was an unexpected error with the application. Please contact support!")));

        return Ok(new ApiResponse(data: token));
    }

    private async Task<AccessTokenResponse?> GetTokenAsync(ClaimsIdentity claims, User user)
    {
        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()),
            new(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64)
        };

        claims.AddClaims(tokenClaims);

        var key = await _cryptographicKeyRetriever.GetExistingKeyAsync();

        if (key is null)
            return null;

        var tokenType = "at+jwt";

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = EnvironmentVariables.IssuerName,
            Subject = claims,
            Expires = DateTime.UtcNow.AddMinutes(EnvironmentVariables.TokenExpirationInMinutes),
            TokenType = tokenType,
            SigningCredentials = key.GetSigningCredentials()
        });

        var jwt = tokenHandler.WriteToken(token);

        return new AccessTokenResponse()
        {
            AccessToken = jwt,
            TokenType = tokenType,
            ExpiresInSeconds = EnvironmentVariables.TokenExpirationInMinutes * 60
        };
    }

    private static long ToUnixEpochDate(DateTime date)
    {
        return (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
    }

    private async Task<ClaimsIdentity> GetUserClaims(User registeredUserResult)
    {
        var claims = new ClaimsIdentity();
        var userClaims = await _userManager.GetClaimsAsync(registeredUserResult);
        var userRoles = await _userManager.GetRolesAsync(registeredUserResult);

        claims.AddClaims(userClaims);
        foreach (var userRole in userRoles)
        {
            claims.AddClaim(new Claim("role", userRole));
        }

        return claims;
    }
}