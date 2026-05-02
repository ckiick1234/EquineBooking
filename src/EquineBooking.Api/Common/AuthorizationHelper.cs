using EquineBooking.Api.Models;
using EquineBooking.Core.Interfaces;
using EquineBooking.Core.Models;

namespace EquineBooking.Api.Common;

/// <summary>
/// Resolves the caller's persisted <see cref="UserProfile"/> and answers role questions
/// against it. Functions only need the JWT for identity; authorisation level (admin vs.
/// client) is sourced from the user record so it can be revoked without re-issuing tokens.
/// </summary>
public sealed class AuthorizationHelper
{
    private readonly IUserRepository _userRepository;

    public AuthorizationHelper(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfile?> GetProfileAsync(UserContext caller, CancellationToken cancellationToken)
        => await _userRepository.GetByIdAsync(caller.ObjectId, cancellationToken);

    public async Task<bool> IsAdminAsync(UserContext caller, CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(caller, cancellationToken);
        return profile is { Role: UserRole.Admin, IsActive: true };
    }
}
