using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FakeItEasy;
using TechNews.Auth.Api.Controllers;
using TechNews.Auth.Api.Data;
using TechNews.Auth.Api.Services.Cryptography;
using TechNews.Auth.Api.Services.KeyRetrievers;

namespace TechNews.Auth.Api.Tests;

public class AuthControllerTests : IClassFixture<TestsFixture>
{
    private TestsFixture _testsFixture { get; set; }

    public AuthControllerTests(TestsFixture testsFixture)
    {
        _testsFixture = testsFixture;
    }

    [Fact(DisplayName = "ShouldReturnBadRequest_WhenUserNotFound")]
    [Trait("Login User", "")]
    public async void LoginAsync_ShouldReturnBadRequest_WhenUserNotFound()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var signInManagerFake = A.Fake<SignInManager<User>>();
        var cryptographicKeyRetrieverFake = A.Fake<ICryptographicKeyRetriever>();
        var controller = new AccountController(userManagerFake, signInManagerFake, cryptographicKeyRetrieverFake);

        var requestFake = _testsFixture.GetValidLoginRequestModel();

        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._))
            .Returns(Task.FromResult<User?>(null));

        // Act
        var response = await controller.LoginAsync(requestFake);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.BadRequest, objectResult?.StatusCode);
        Assert.Null(apiResponse?.Data);
        Assert.True(apiResponse?.Errors?.Count > 0);
        Assert.True(apiResponse?.Errors?.Any(x => x.ErrorCode == "InvalidRequest"));
        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
    }

    [Fact(DisplayName = "ShouldReturnBadRequest_WhenUserHasNoUserName")]
    [Trait("Login User", "")]
    public async void LoginAsync_ShouldReturnBadRequest_WhenUserHasNoUserName()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var signInManagerFake = A.Fake<SignInManager<User>>();
        var cryptographicKeyRetrieverFake = A.Fake<ICryptographicKeyRetriever>();
        var controller = new AccountController(userManagerFake, signInManagerFake, cryptographicKeyRetrieverFake);

        var requestFake = _testsFixture.GetValidLoginRequestModel();

        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._))
            .Returns(Task.FromResult<User?>(new User(Guid.NewGuid(), requestFake.Email, null)));

        // Act
        var response = await controller.LoginAsync(requestFake);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.BadRequest, objectResult?.StatusCode);
        Assert.Null(apiResponse?.Data);
        Assert.True(apiResponse?.Errors?.Count > 0);
        Assert.True(apiResponse?.Errors?.Any(x => x.ErrorCode == "InvalidRequest"));
        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
    }

    [Fact(DisplayName = "ShouldReturnForbidden_WhenUserLockedOut")]
    [Trait("Login User", "")]
    public async void LoginAsync_ShouldReturnForbidden_WhenUserLockedOut()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var signInManagerFake = A.Fake<SignInManager<User>>();
        var cryptographicKeyRetrieverFake = A.Fake<ICryptographicKeyRetriever>();
        var controller = new AccountController(userManagerFake, signInManagerFake, cryptographicKeyRetrieverFake);

        var requestFake = _testsFixture.GetValidLoginRequestModel();

        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._))
            .Returns(Task.FromResult<User?>(_testsFixture.GetFakeUser()));

        A.CallTo(() => signInManagerFake.PasswordSignInAsync(A<string>._, A<string>._, false, true))
            .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.LockedOut));

        // Act
        var response = await controller.LoginAsync(requestFake);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.Forbidden, objectResult?.StatusCode);
        Assert.Null(apiResponse?.Data);
        Assert.True(apiResponse?.Errors?.Count > 0);
        Assert.True(apiResponse?.Errors?.Any(x => x.ErrorCode == "LockedUser"));
        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
        A.CallTo(() => signInManagerFake.PasswordSignInAsync(A<string>._, A<string>._, false, true)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
    }

    [Fact(DisplayName = "ShouldReturnBadRequest_WhenSignInFails")]
    [Trait("Login User", "")]
    public async void LoginAsync_ShouldReturnBadRequest_WhenSignInFails()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var signInManagerFake = A.Fake<SignInManager<User>>();
        var cryptographicKeyRetrieverFake = A.Fake<ICryptographicKeyRetriever>();
        var controller = new AccountController(userManagerFake, signInManagerFake, cryptographicKeyRetrieverFake);

        var requestFake = _testsFixture.GetValidLoginRequestModel();

        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._))
            .Returns(Task.FromResult<User?>(_testsFixture.GetFakeUser()));

        A.CallTo(() => signInManagerFake.PasswordSignInAsync(A<string>._, A<string>._, false, true))
            .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Failed));

        // Act
        var response = await controller.LoginAsync(requestFake);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.BadRequest, objectResult?.StatusCode);
        Assert.Null(apiResponse?.Data);
        Assert.True(apiResponse?.Errors?.Count > 0);
        Assert.True(apiResponse?.Errors?.Any(x => x.ErrorCode == "InvalidRequest"));
        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
        A.CallTo(() => signInManagerFake.PasswordSignInAsync(A<string>._, A<string>._, false, true)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
    }

    [Fact(DisplayName = "ShouldReturnInternalError_WhenNoCryptoKeyAvailable")]
    [Trait("Login User", "")]
    public async void LoginAsync_ShouldReturnInternalError_WhenNoCryptoKeyAvailable()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var signInManagerFake = A.Fake<SignInManager<User>>();
        var cryptographicKeyRetrieverFake = A.Fake<ICryptographicKeyRetriever>();
        var controller = new AccountController(userManagerFake, signInManagerFake, cryptographicKeyRetrieverFake);

        var requestFake = _testsFixture.GetValidLoginRequestModel();

        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._))
            .Returns(Task.FromResult<User?>(_testsFixture.GetFakeUser()));

        A.CallTo(() => signInManagerFake.PasswordSignInAsync(A<string>._, A<string>._, false, true))
            .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Success));

        A.CallTo(() => cryptographicKeyRetrieverFake.GetExistingKeyAsync())
           .Returns(Task.FromResult<ICryptographicKey?>(null));

        // Act
        var response = await controller.LoginAsync(requestFake);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.InternalServerError, objectResult?.StatusCode);
        Assert.Null(apiResponse?.Data);
        Assert.True(apiResponse?.Errors?.Count > 0);
        Assert.True(apiResponse?.Errors?.Any(x => x.ErrorCode == "InternalError"));
        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
        A.CallTo(() => signInManagerFake.PasswordSignInAsync(A<string>._, A<string>._, false, true)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
    }

    [Fact(DisplayName = "ShouldReturnOkWithToken_WhenRequestIsValid")]
    [Trait("Login User", "")]
    public async void LoginAsync_ShouldReturnOkWithToken_WhenRequestIsValid()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var signInManagerFake = A.Fake<SignInManager<User>>();
        var cryptographicKeyRetrieverFake = A.Fake<ICryptographicKeyRetriever>();
        var cryptoKeyFake = A.Fake<ICryptographicKey>();
        var httpContextFake = A.Fake<HttpContext>();
        var requestFake = _testsFixture.GetValidLoginRequestModel();

        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._))
            .Returns(Task.FromResult<User?>(_testsFixture.GetFakeUser()));

        A.CallTo(() => signInManagerFake.PasswordSignInAsync(A<string>._, A<string>._, false, true))
            .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Success));

        A.CallTo(() => cryptographicKeyRetrieverFake.GetExistingKeyAsync())
           .Returns(Task.FromResult<ICryptographicKey?>(cryptoKeyFake));

        A.CallTo(() => cryptoKeyFake.GetSigningCredentials())
           .Returns(_testsFixture.GetRsaSigningCredentials());

        A.CallTo(() => httpContextFake.Request.Scheme).Returns("https");
        A.CallTo(() => httpContextFake.Request.Host).Returns(new HostString("localhost:5000"));

        var controller = new AccountController(userManagerFake, signInManagerFake, cryptographicKeyRetrieverFake)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = httpContextFake
            }
        };

        // Act
        var response = await controller.LoginAsync(requestFake);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);
        var accessToken = _testsFixture.GetAccessTokenFromApiResponse(apiResponse);

        Assert.Equal((int)HttpStatusCode.OK, objectResult?.StatusCode);
        Assert.NotNull(apiResponse?.Data);
        Assert.Null(apiResponse?.Errors);
        Assert.True(accessToken?.ExpiresInSeconds > 0);
        Assert.Equal("at+jwt", accessToken?.TokenType);
        Assert.True(!string.IsNullOrWhiteSpace(accessToken?.AccessToken));
        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
        A.CallTo(() => signInManagerFake.PasswordSignInAsync(A<string>._, A<string>._, false, true)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
    }
    
    [Fact(DisplayName = "ShouldReturnTokenWithAllClaims_WhenUserHasClaimsOrRoles")]
    [Trait("Login User", "")]
    public async void LoginAsync_ShouldReturnTokenWithAllClaims_WhenUserHasClaimsOrRoles()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var signInManagerFake = A.Fake<SignInManager<User>>();
        var cryptographicKeyRetrieverFake = A.Fake<ICryptographicKeyRetriever>();
        var cryptoKeyFake = A.Fake<ICryptographicKey>();
        var httpContextFake = A.Fake<HttpContext>();
        var requestFake = _testsFixture.GetValidLoginRequestModel();

        var userFake = _testsFixture.GetFakeUser();

        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._))
           .Returns(Task.FromResult<User?>(userFake));

        A.CallTo(() => signInManagerFake.PasswordSignInAsync(A<string>._, A<string>._, false, true))
            .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Success));
        
        A.CallTo(() => cryptographicKeyRetrieverFake.GetExistingKeyAsync())
           .Returns(Task.FromResult<ICryptographicKey?>(cryptoKeyFake));
        
        A.CallTo(() => cryptoKeyFake.GetSigningCredentials())
           .Returns(_testsFixture.GetRsaSigningCredentials());
        
        A.CallTo(() => httpContextFake.Request.Scheme).Returns("https");
        A.CallTo(() => httpContextFake.Request.Host).Returns(new HostString("localhost:5000"));
        
        A.CallTo(() => userManagerFake.GetClaimsAsync(userFake))
           .Returns(Task.FromResult(_testsFixture.GetFakeClaims()));
        
        A.CallTo(() => userManagerFake.GetRolesAsync(userFake))
           .Returns(Task.FromResult(_testsFixture.GetFakeRoles()));

        var controller = new AccountController(userManagerFake, signInManagerFake, cryptographicKeyRetrieverFake)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = httpContextFake
            }
        };

        // Act
        var response = await controller.LoginAsync(requestFake);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);
        var accessToken = _testsFixture.GetAccessTokenFromApiResponse(apiResponse);

        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(accessToken?.AccessToken) as JwtSecurityToken;

        Assert.Equal((int)HttpStatusCode.OK, objectResult?.StatusCode);
        Assert.NotNull(apiResponse?.Data);
        Assert.Null(apiResponse?.Errors);
        Assert.True(accessToken?.ExpiresInSeconds > 0);
        Assert.Equal("at+jwt", accessToken?.TokenType);
        Assert.True(!string.IsNullOrWhiteSpace(accessToken?.AccessToken));

        Assert.True(jsonToken?.Claims.Any(c => c.Type == "claim1" && c.Value == "testClaim1"));
        Assert.True(jsonToken?.Claims.Any(c => c.Type == "claim2" && c.Value == "testClaim2"));
        Assert.True(jsonToken?.Claims.Any(c => c.Type == "role" && c.Value == "testRole1"));
        Assert.True(jsonToken?.Claims.Any(c => c.Type == "role" && c.Value == "testRole2"));
        Assert.True(jsonToken?.Claims.Any(c => c.Type == "sub"));
        Assert.True(jsonToken?.Claims.Any(c => c.Type == "email"));
        Assert.True(jsonToken?.Claims.Any(c => c.Type == "name"));
        Assert.True(jsonToken?.Claims.Any(c => c.Type == "jti"));
        Assert.True(jsonToken?.Claims.Any(c => c.Type == "nbf"));
        Assert.True(jsonToken?.Claims.Any(c => c.Type == "iat"));
        Assert.True(jsonToken?.Claims.Any(c => c.Type == "iss"));
        Assert.True(jsonToken?.Claims.Any(c => c.Type == "exp"));

        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
        A.CallTo(() => signInManagerFake.PasswordSignInAsync(A<string>._, A<string>._, A<bool>._, A<bool>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
    }
}