using System.Net;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FakeItEasy;
using TechNews.Auth.Api.Controllers;
using TechNews.Auth.Api.Data;
using TechNews.Auth.Api.Models;
using TechNews.Auth.Api.Services.Cryptography;
using TechNews.Auth.Api.Services.KeyRetrievers;
using TechNews.Common.Library.MessageBus;
using TechNews.Common.Library.Services;

namespace TechNews.Auth.Api.Tests;

public class UserControllerTests : IClassFixture<TestsFixture>
{
    private TestsFixture _testsFixture { get; set; }

    public UserControllerTests(TestsFixture testsFixture)
    {
        _testsFixture = testsFixture;
    }

    [Fact(DisplayName = "ShouldReturnBadRequest_WhenUserAlreadyExists")]
    [Trait("Register User", "")]
    public async void RegisterUserAsync_ShouldReturnBadRequest_WhenUserAlreadyExists()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var messageBusFake = A.Fake<IMessageBus>();
        var eventStoreFake = A.Fake<IEventStoreService>();
        var controller = new UserController(userManagerFake, messageBusFake, eventStoreFake);
        var requestFake = _testsFixture.GetValidRegisterUserRequestModel();

        A.CallTo(() => userManagerFake.FindByIdAsync(A<string>._))
        .Returns(Task.FromResult<User?>(_testsFixture.GetFakeUser()));

        // Act
        var response = await controller.RegisterUserAsync(requestFake);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.BadRequest, objectResult?.StatusCode);
        Assert.Null(apiResponse?.Data);
        Assert.True(apiResponse?.Errors?.Count > 0);
        Assert.True(apiResponse?.Errors?.Any(x => x.ErrorCode == "UserAlreadyExists"));
        A.CallTo(() => userManagerFake.FindByIdAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
    }

    [Fact(DisplayName = "ShouldReturnBadRequest_WhenUserCreationDoesNotSucceed")]
    [Trait("Register User", "")]
    public async void RegisterUserAsync_ShouldReturnBadRequest_WhenUserCreationDoesNotSucceed()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var messageBusFake = A.Fake<IMessageBus>();
        var eventStoreFake = A.Fake<IEventStoreService>();
        var controller = new UserController(userManagerFake, messageBusFake, eventStoreFake);

        var requestFake = _testsFixture.GetValidRegisterUserRequestModel();

        if (requestFake.Id is null)
        {
            Assert.Fail($"Arrange not configured correctly. Property {nameof(requestFake.Id)} should not be null.");
        }

        A.CallTo(() => userManagerFake.FindByIdAsync(A<string>._))
            .Returns(Task.FromResult<User?>(null));

        A.CallTo(() => userManagerFake.CreateAsync(A<User>._, A<string>._))
           .Returns(Task.FromResult(_testsFixture.GetDefaultIdentityFailure()));

        // Act
        var response = await controller.RegisterUserAsync(requestFake);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.BadRequest, objectResult?.StatusCode);
        Assert.Null(apiResponse?.Data);
        Assert.True(apiResponse?.Errors?.Count > 0);
        A.CallTo(() => userManagerFake.FindByIdAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
        A.CallTo(() => userManagerFake.CreateAsync(A<User>._, A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
    }

    [Fact(DisplayName = "ShouldReturnInternalError_WhenUserCreatedIsNotFound")]
    [Trait("Register User", "")]
    public async void RegisterUserAsync_ShouldReturnInternalError_WhenUserCreatedIsNotFound()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var messageBusFake = A.Fake<IMessageBus>();
        var eventStoreFake = A.Fake<IEventStoreService>();
        var controller = new UserController(userManagerFake, messageBusFake, eventStoreFake);

        var requestFake = _testsFixture.GetValidRegisterUserRequestModel();

        if (requestFake.Id is null)
        {
            Assert.Fail($"Arrange not configured correctly. Property {nameof(requestFake.Id)} should not be null.");
        }

        A.CallTo(() => userManagerFake.FindByIdAsync(A<string>._))
            .Returns(Task.FromResult<User?>(null));

        A.CallTo(() => userManagerFake.CreateAsync(A<User>._, A<string>._))
           .Returns(Task.FromResult(IdentityResult.Success));

        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._))
           .Returns(Task.FromResult<User?>(null));

        // Act
        var response = await controller.RegisterUserAsync(requestFake);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.InternalServerError, objectResult?.StatusCode);
        Assert.Null(apiResponse?.Data);
        Assert.True(apiResponse?.Errors?.Count > 0);
        Assert.True(apiResponse?.Errors?.Any(x => x.ErrorCode == "InternalError"));
        A.CallTo(() => userManagerFake.FindByIdAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
        A.CallTo(() => userManagerFake.CreateAsync(A<User>._, A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
    }

    [Fact(DisplayName = "ShouldReturnCreated_WhenRequestIsValid")]
    [Trait("Register User", "")]
    public async void RegisterUserAsync_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var messageBusFake = A.Fake<IMessageBus>();
        var eventStoreFake = A.Fake<IEventStoreService>();
        var cryptographicKeyRetrieverFake = A.Fake<ICryptographicKeyRetriever>();
        var cryptoKeyFake = A.Fake<ICryptographicKey>();
        var httpContextFake = A.Fake<HttpContext>();
        var requestFake = _testsFixture.GetValidRegisterUserRequestModel();

        if (requestFake.Id is null)
        {
            Assert.Fail($"Arrange not configured correctly. Property {nameof(requestFake.Id)} should not be null.");
        }

        var createdUserFake = new User(requestFake.Id.Value, requestFake.Email, requestFake.UserName);

        A.CallTo(() => userManagerFake.FindByIdAsync(A<string>._))
            .Returns(Task.FromResult<User?>(null));

        A.CallTo(() => userManagerFake.CreateAsync(A<User>._, A<string>._))
           .Returns(Task.FromResult(IdentityResult.Success));

        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._))
           .Returns(Task.FromResult<User?>(createdUserFake));

        A.CallTo(() => cryptographicKeyRetrieverFake.GetExistingKeyAsync())
           .Returns(Task.FromResult<ICryptographicKey?>(cryptoKeyFake));

        A.CallTo(() => cryptoKeyFake.GetSigningCredentials())
           .Returns(_testsFixture.GetRsaSigningCredentials());

        A.CallTo(() => httpContextFake.Request.Scheme).Returns("https");
        A.CallTo(() => httpContextFake.Request.Host).Returns(new HostString("localhost:5000"));

        var controller = new UserController(userManagerFake, messageBusFake, eventStoreFake)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = httpContextFake
            }
        };

        // Act
        var response = await controller.RegisterUserAsync(requestFake);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.Created, objectResult?.StatusCode);
        Assert.Null(apiResponse?.Errors);
        A.CallTo(() => userManagerFake.FindByIdAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
        A.CallTo(() => userManagerFake.CreateAsync(A<User>._, A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
        A.CallTo(() => userManagerFake.FindByEmailAsync(A<string>._)).MustHaveHappened(numberOfTimes: 1, timesOption: Times.Exactly);
    }

    [Fact(DisplayName = "ShouldReturnBadRequest_WhenGuidEmpty")]
    [Trait("Get User", "")]
    public async void GetUser_ShouldReturnBadRequest_WhenGuidEmpty()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var messageBusFake = A.Fake<IMessageBus>();
        var eventStoreFake = A.Fake<IEventStoreService>();
        var controller = new UserController(userManagerFake, messageBusFake, eventStoreFake);

        // Act
        var response = await controller.GetUser(Guid.Empty);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.BadRequest, objectResult?.StatusCode);
        Assert.Null(apiResponse?.Data);
        Assert.True(apiResponse?.Errors?.Count > 0);
        Assert.True(apiResponse?.Errors?.Any(x => x.ErrorCode == "InvalidUser"));
    }

    [Fact(DisplayName = "ShouldReturnNotFound_WhenUserNotFound")]
    [Trait("Get User", "")]
    public async void GetUser_ShouldReturnNotFound_WhenUserNotFound()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var messageBusFake = A.Fake<IMessageBus>();
        var eventStoreFake = A.Fake<IEventStoreService>();
        var controller = new UserController(userManagerFake, messageBusFake, eventStoreFake);

        A.CallTo(() => userManagerFake.FindByIdAsync(A<string>._))
        .Returns(Task.FromResult<User?>(null));

        // Act
        var response = await controller.GetUser(Guid.NewGuid());

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.NotFound, objectResult?.StatusCode);
        Assert.Null(apiResponse?.Data);
        Assert.True(apiResponse?.Errors?.Count > 0);
        Assert.True(apiResponse?.Errors?.Any(x => x.ErrorCode == "UserNotFound"));
    }

    [Fact(DisplayName = "ShouldReturnOk_WhenUserFound")]
    [Trait("Get User", "")]
    public async void GetUser_ShouldReturnOk_WhenUserFound()
    {
        // Arrange
        var userManagerFake = A.Fake<UserManager<User>>();
        var messageBusFake = A.Fake<IMessageBus>();
        var eventStoreFake = A.Fake<IEventStoreService>();
        var controller = new UserController(userManagerFake, messageBusFake, eventStoreFake);
        var fakeUser = _testsFixture.GetFakeUser();

        A.CallTo(() => userManagerFake.FindByIdAsync(A<string>._))
        .Returns(Task.FromResult<User?>(fakeUser));

        // Act
        var response = await controller.GetUser(fakeUser.Id);

        // Assert
        var objectResult = (ObjectResult?)response;
        var apiResponse = _testsFixture.GetApiResponseFromObjectResult(objectResult);

        Assert.Equal((int)HttpStatusCode.OK, objectResult?.StatusCode);
        Assert.NotNull(apiResponse?.Data);
        Assert.Null(apiResponse?.Errors);
        Assert.Equal(fakeUser.Id, ((GetUserResponseModel?)apiResponse?.Data)?.Id);
    }
}