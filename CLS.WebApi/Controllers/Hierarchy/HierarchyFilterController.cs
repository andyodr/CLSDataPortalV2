using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.Hierarchy;

[ApiController]
[Route("api/hierarchy/[controller]")]
[Authorize]
public class FilterController : ControllerBase
{
	[HttpGet]
	public void Get() {
	}
}
