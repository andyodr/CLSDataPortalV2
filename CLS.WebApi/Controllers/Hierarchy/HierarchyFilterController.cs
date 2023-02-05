using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.Hierarchy;

[Route("api/hierarchy/[controller]")]
[Authorize]
[ApiController]
public class FilterController : ControllerBase
{
	// GET: api/values
	[HttpGet]
	public void Get() {
	}
}
