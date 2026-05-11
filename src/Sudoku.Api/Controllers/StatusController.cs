using Microsoft.AspNetCore.Mvc;
using Sudoku.Api.Contracts;

namespace Sudoku.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiStatusResponse> Get()
    {
        return Ok(new ApiStatusResponse("ok", DateTimeOffset.UtcNow));
    }
}
