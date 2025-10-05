
using Microsoft.EntityFrameworkCore;

namespace RegisterForm.Data
{
    public class RegisterFormDbContext : DbContext
    {
        public RegisterFormDbContext(DbContextOptions<RegisterFormDbContext> options) : base(options)
        {
        }
        public virtual DbSet<Users> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Users>()
                .HasIndex(u => u.Email)
                .IsUnique();
            builder.Entity<Users>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();
            builder.Entity<Users>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}
