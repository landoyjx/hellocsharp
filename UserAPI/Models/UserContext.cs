using Microsoft.EntityFrameworkCore;

namespace UserAPI.Models
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<BlackItem> BlackLists { get; set; }
    }
}