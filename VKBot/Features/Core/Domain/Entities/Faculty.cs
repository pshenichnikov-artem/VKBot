using System.ComponentModel.DataAnnotations;

namespace VKBot.Features.Core.Domain.Entities;

public class Faculty
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public ICollection<Group> Groups { get; set; } = new List<Group>();
}