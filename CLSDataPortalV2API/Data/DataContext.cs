using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CLSDataPortalV2API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CLSDataPortalV2API.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<AppUser> Users { get; set; }
    }
}