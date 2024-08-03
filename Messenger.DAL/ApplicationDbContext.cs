using Messenger.Domain.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Messenger.DAL
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Profile> Profiles { get; set; } = null!;
        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options) 
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Contact>()
                .HasOne(c => c.User)
                .WithMany(u => u.UserContacts)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Contact>()
                .HasOne(c => c.ContactUser)
                .WithMany(u => u.ContactContacts)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SenderMessages)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Recipient)
                .WithMany(u => u.RecipientMessages)
                .OnDelete(DeleteBehavior.SetNull);

        }
    }
}