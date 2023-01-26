using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CLSDataPortalV2API.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public int UserRoleid { get; set; }
        public int Active { get; set; }
        public DateTime LastUpdatedOn { get; set; }

    }
}