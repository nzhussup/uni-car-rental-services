using System.Net;
using System.Text;
using AutoMapper;
using CarRentalService.Mappings;
using CarRentalService.Services;
using CarRentalService.Models.DTOs;
using FluentAssertions;
using Keycloak.AuthServices.Sdk;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace CarRentalService.Tests.Services;

public class UserServiceTests
{
    private readonly IMapper _mapper;

    public UserServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), new NullLoggerFactory());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnMappedUser_WhenKeycloakSdkSucceeds()
    {
        var userId = Guid.NewGuid();
        var keycloakClient = new Mock<IKeycloakUserClient>();
        keycloakClient
            .Setup(client => client.GetUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRepresentation
            {
                Id = userId.ToString(),
                FirstName = "Alice",
                LastName = "Admin",
                Email = "alice.admin@example.com"
            });

        var service = CreateService(
            keycloakClient.Object,
            BuildConfiguration(new Dictionary<string, string?>()),
            CreateHttpClientFactory(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))));

        var result = await service.GetUserByIdAsync(userId);

        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.FirstName.Should().Be("Alice");
        result.LastName.Should().Be("Admin");
        result.Email.Should().Be("alice.admin@example.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldUseBootstrapAdminFallback_WhenKeycloakSdkFails()
    {
        var userId = Guid.NewGuid();
        var keycloakClient = new Mock<IKeycloakUserClient>();
        keycloakClient
            .Setup(client => client.GetUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("sdk-failure"));

        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Keycloak:auth-server-url"] = "http://keycloak.local",
            ["Keycloak:realm"] = "car-rental-dev",
            ["KeycloakBootstrapAdmin:Username"] = "root",
            ["KeycloakBootstrapAdmin:Password"] = "root"
        });

        var handler = new StubHttpMessageHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.EndsWith("/realms/master/protocol/openid-connect/token", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, """{"access_token":"bootstrap-token"}""");
            }

            if (path.EndsWith($"/admin/realms/car-rental-dev/users/{userId}", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, """
                {
                  "firstName": "Fallback",
                  "lastName": "User",
                  "email": "fallback.user@example.com",
                  "username": "fallback.user"
                }
                """);
            }

            return Json(HttpStatusCode.NotFound, """{}""");
        });

        var service = CreateService(
            keycloakClient.Object,
            configuration,
            CreateHttpClientFactory(handler));

        var result = await service.GetUserByIdAsync(userId);

        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.FirstName.Should().Be("Fallback");
        result.LastName.Should().Be("User");
        result.Email.Should().Be("fallback.user@example.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUnknownFallback_WhenAllLookupsFail()
    {
        var userId = Guid.NewGuid();
        var keycloakClient = new Mock<IKeycloakUserClient>();
        keycloakClient
            .Setup(client => client.GetUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("sdk-failure"));

        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Keycloak:auth-server-url"] = "http://keycloak.local",
            ["Keycloak:realm"] = "car-rental-dev",
            ["KeycloakBootstrapAdmin:Username"] = "root",
            ["KeycloakBootstrapAdmin:Password"] = "root"
        });

        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.Forbidden, """{"error":"forbidden"}"""));

        var service = CreateService(
            keycloakClient.Object,
            configuration,
            CreateHttpClientFactory(handler));

        var result = await service.GetUserByIdAsync(userId);

        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.FirstName.Should().Be("Unknown");
        result.LastName.Should().BeEmpty();
        result.Email.Should().BeEmpty();
    }

    private UserService CreateService(
        IKeycloakUserClient keycloakClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        var options = Options.Create(new KeycloakAdminClientOptions
        {
            Realm = "car-rental-dev"
        });

        return new UserService(
            keycloakClient,
            options,
            _mapper,
            httpClientFactory,
            configuration,
            NullLogger<UserService>.Instance);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static IHttpClientFactory CreateHttpClientFactory(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler));
        return factory.Object;
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
