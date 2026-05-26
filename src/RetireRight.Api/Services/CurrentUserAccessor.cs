using Microsoft.IdentityModel.JsonWebTokens;

namespace RetireRight.Api.Services;

public interface ICurrentUserAccessor
{
    /// <summary>The authenticated user's id from the JWT sub claim. Throws if no user.</summary>
    int RequireUserId();
    int? UserIdOrNull();
    bool IsAdmin { get; }
    string? Email { get; }
}

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _ctxAccessor;

    public CurrentUserAccessor(IHttpContextAccessor ctxAccessor)
    {
        _ctxAccessor = ctxAccessor;
    }

    public int RequireUserId()
    {
        var id = UserIdOrNull();
        if (id is null) throw new UnauthorizedAccessException("No authenticated user.");
        return id.Value;
    }

    public int? UserIdOrNull()
    {
        var sub = _ctxAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }

    public bool IsAdmin =>
        _ctxAccessor.HttpContext?.User?.FindFirst("is_admin")?.Value == "true";

    public string? Email =>
        _ctxAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
}
