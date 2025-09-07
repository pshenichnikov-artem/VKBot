using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageDelivery> MessageDeliveries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
       base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>()
            .HasKey(u => u.VkUserId);
        
        modelBuilder.Entity<User>()
            .HasOne(u => u.Group)
            .WithMany(g => g.Users)
            .HasForeignKey(u => u.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        
        
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
        

        
        modelBuilder.Entity<MessageDelivery>()
            .HasKey (md => new {md.MessageId, md.RecipientId});
        
        modelBuilder.Entity<MessageDelivery>()
            .HasOne(md => md.Message)
            .WithMany(m => m.MessageDeliveries)
            .HasForeignKey(md => md.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<MessageDelivery>()
            .HasOne(md => md.Recipient)
            .WithMany(u => u.MessageDeliveries)
            .HasForeignKey(md => md.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Захардкоженные админы
        modelBuilder.Entity<User>().HasData(
            new User
            {
                VkUserId = 651565729,
                FullName = "Admin 1",
                Role = UserRole.Admin.ToString(),
                IsConfirmed = true,
                GroupId = null
            },
            new User
            {
                VkUserId = 562436407,
                FullName = "Admin 2",
                Role = UserRole.Admin.ToString(),
                IsConfirmed = true,
                GroupId = null
            }
        );
    }
}