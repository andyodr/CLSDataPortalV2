using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CLSDataPortalV2API.Entities;

namespace CLSDataPortalV2API.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(AppUser user);
    }
}