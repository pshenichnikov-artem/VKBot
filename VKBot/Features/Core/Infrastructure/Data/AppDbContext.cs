using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Domain.Entities;

namespace VKBot.Features.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageGroup> MessageGroups { get; set; }
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
            .OnDelete(DeleteBehavior.Restrict);
        
        
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Recipient)
            .WithMany(u => u.RereceivedMessages)
            .HasForeignKey(m => m.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<MessageGroup>()
            .HasKey(mg => new {mg.MessageId, mg.GroupId});
        
        
        modelBuilder.Entity<MessageGroup>()
            .HasOne(mg => mg.Message)
            .WithMany(m => m.TargetList)
            .HasForeignKey(mg => mg.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<MessageGroup>()
            .HasOne(mg => mg.Group)
            .WithMany(g => g.MessageGroups)
            .HasForeignKey(mg => mg.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        
        
        modelBuilder.Entity<MessageDelivery>()
            .HasKey (md => new {md.MessageId, md.UserId});
        
        modelBuilder.Entity<MessageDelivery>()
            .HasOne(md => md.Message)
            .WithMany(m => m.MessageDeliveries)
            .HasForeignKey(md => md.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<MessageDelivery>()
            .HasOne(md => md.User)
            .WithMany(u => u.MessageDeliveries)
            .HasForeignKey(md => md.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}