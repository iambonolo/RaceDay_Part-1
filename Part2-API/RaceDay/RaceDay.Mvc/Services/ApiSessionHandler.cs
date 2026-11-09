using System.Net.Http;

namespace RaceDay.Mvc.Services;

public class ApiSessionHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiSessionHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;

        if (context != null)
        {
            var cookie = context.Session.GetString("ApiSessionCookie");

            if (!string.IsNullOrEmpty(cookie))
            {
                request.Headers.Remove("Cookie");
                request.Headers.TryAddWithoutValidation("Cookie", cookie);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (context != null &&
            response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var sessionCookie = cookies.FirstOrDefault(c =>
                c.StartsWith(".RaceDay.Session=", StringComparison.OrdinalIgnoreCase));

            if (sessionCookie != null)
            {
                var cookieValue = sessionCookie.Split(';')[0];
                context.Session.SetString("ApiSessionCookie", cookieValue);
            }
        }

        return response;
    }
}