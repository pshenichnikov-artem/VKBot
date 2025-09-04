using System.ComponentModel.DataAnnotations;

namespace VKBot.Features.Core.Domain.Entities
{
    public class Group
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Cohort { get; set; } = string.Empty;
        [Required]
        public short GroupNumber { get; set; }
        public List<User> Users { get; set; } = new List<User>();
        public ICollection<MessageGroup> MessageGroups { get; set; } = new List<MessageGroup>();
    }
}
