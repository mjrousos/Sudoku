using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Sudoku.Api.Contracts;

namespace Sudoku.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AntiforgeryController(IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("token")]
    public ActionResult<AntiforgeryTokenResponse> GetToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        var requestToken = tokens.RequestToken
            ?? throw new InvalidOperationException("Antiforgery did not issue a request token.");

        Response.Cookies.Append(
            "XSRF-TOKEN",
            requestToken,
            new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

        return Ok(new AntiforgeryTokenResponse(requestToken));
    }
}
