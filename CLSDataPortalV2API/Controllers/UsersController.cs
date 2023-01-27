using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CLSDataPortalV2API.Data;
using CLSDataPortalV2API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLSDataPortalV2API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // GET /api/users

    public class UsersController : ControllerBase
    {
        private readonly DataContext _context;

        public UsersController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return users;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AppUser>> GetUser(int id)
        {
            return await _context.Users.FindAsync(id);

        }
    }
}