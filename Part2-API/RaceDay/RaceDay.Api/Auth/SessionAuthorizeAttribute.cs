using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Auth;

// Use [SessionAuthorize] for "any logged-in user" or [SessionAuthorize("Organiser")] for a specific role.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetUserId() is null)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = 401,
                Title = "Unauthorized",
                Detail = "You must log in first."
            })
            { StatusCode = 401 };
            return;
        }

        var role = session.GetRole() ?? string.Empty;
        if (_roles.Length > 0 && !_roles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = 403,
                Title = "Forbidden",
                Detail = "Your role is not allowed to use this endpoint."
            })
            { StatusCode = 403 };
        }
    }
}