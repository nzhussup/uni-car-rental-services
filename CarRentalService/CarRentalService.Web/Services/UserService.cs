using AutoMapper;
using CarRentalService.Models.DTOs;
using Keycloak.AuthServices.Sdk;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using Microsoft.Extensions.Options;

namespace CarRentalService.Services;

public class UserService(IKeycloakUserClient keycloakClient, IOptions<KeycloakAdminClientOptions> options, IMapper mapper) : IUserService
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
            return null;
        }
    }
}