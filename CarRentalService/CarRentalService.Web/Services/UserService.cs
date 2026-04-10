using AutoMapper;
using CarRentalService.Models.DTOs;
using Keycloak.AuthServices.Sdk;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CarRentalService.Services;

public class UserService(
    IKeycloakUserClient keycloakClient,
    IOptions<KeycloakAdminClientOptions> options,
    IMapper mapper,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<UserService> logger) : IUserService
{
    private readonly string _realm = options.Value.Realm;

    public async Task<UserDto> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var user = await keycloakClient.GetUserAsync(_realm, userId.ToString());
            return mapper.Map<UserDto>(user);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Keycloak SDK user lookup failed for user {UserId}", userId);
        }

        var fallbackUser = await TryGetUserViaBootstrapAdminAsync(userId);
        if (fallbackUser is not null)
        {
            return fallbackUser;
        }

        logger.LogWarning("Returning fallback user payload for user {UserId} because all Keycloak lookups failed.", userId);
        return new UserDto
        {
            Id = userId,
            FirstName = "Unknown",
            LastName = string.Empty,
            Email = string.Empty
        };
    }

    private async Task<UserDto?> TryGetUserViaBootstrapAdminAsync(Guid userId)
    {
        var authServerUrl = configuration["Keycloak:auth-server-url"] ?? configuration["Keycloak:authServerUrl"];
        var realm = configuration["Keycloak:realm"];
        var adminUsername = configuration["KeycloakBootstrapAdmin:Username"];
        var adminPassword = configuration["KeycloakBootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(authServerUrl) ||
            string.IsNullOrWhiteSpace(realm) ||
            string.IsNullOrWhiteSpace(adminUsername) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Bootstrap admin fallback is not configured (missing Keycloak URL/realm/admin credentials).");
            return null;
        }

        var baseUrl = authServerUrl.TrimEnd('/');
        var client = httpClientFactory.CreateClient();

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = adminUsername,
            ["password"] = adminPassword
        });

        var tokenResponse = await client.PostAsync($"{baseUrl}/realms/master/protocol/openid-connect/token", tokenRequest);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Bootstrap admin token request failed with status {StatusCode}", tokenResponse.StatusCode);
            return null;
        }

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        using var tokenDoc = JsonDocument.Parse(tokenJson);
        if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
        {
            logger.LogWarning("Bootstrap admin token response did not contain access_token");
            return null;
        }

        var accessToken = accessTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning("Bootstrap admin access token was empty");
            return null;
        }

        var userRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/admin/realms/{realm}/users/{userId}");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userResponse = await client.SendAsync(userRequest);
        if (!userResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Bootstrap admin user lookup failed for user {UserId} with status {StatusCode}", userId, userResponse.StatusCode);
            return null;
        }

        var userJson = await userResponse.Content.ReadAsStringAsync();
        using var userDoc = JsonDocument.Parse(userJson);
        var root = userDoc.RootElement;

        var firstName = root.TryGetProperty("firstName", out var firstNameElement) ? firstNameElement.GetString() : null;
        var lastName = root.TryGetProperty("lastName", out var lastNameElement) ? lastNameElement.GetString() : null;
        var email = root.TryGetProperty("email", out var emailElement) ? emailElement.GetString() : null;
        var username = root.TryGetProperty("username", out var usernameElement) ? usernameElement.GetString() : null;

        return new UserDto
        {
            Id = userId,
            FirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName! : (username ?? "Unknown"),
            LastName = lastName ?? string.Empty,
            Email = email ?? string.Empty
        };
    }
}
