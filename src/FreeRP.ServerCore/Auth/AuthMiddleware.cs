using FreeRP.FrpServices;
using Microsoft.AspNetCore.Http;

namespace FreeRP.ServerCore.Auth
{
    public class AuthMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext httpContext, IFrpAuthService authService)
        {
            if (httpContext.User.Identity != null && httpContext.User.Identity.IsAuthenticated)
            {
                if(authService is FrpAuthService s)
                    await s.SetUserAsync(httpContext.User);
            }
            await _next(httpContext);
        }
    }
}
