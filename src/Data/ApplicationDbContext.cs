using Microsoft.EntityFrameworkCore;
using FileSharing.Models;

namespace FileSharing.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> option) : base(option) 
        { 
        }

        public DbSet<Entry> Entry { get; set; }
    }
}
