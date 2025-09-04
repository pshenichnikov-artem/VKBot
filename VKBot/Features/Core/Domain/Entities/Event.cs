using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VKBot.Features.Core.Domain.Enums;

namespace VKBot.Features.Core.Domain.Entities
{
    public class Event
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public long MessageId { get; set; } //?
        public long AdminId { get; set; } //?
        public User? Admin { get; set; } = null;
        [Required]
        public string Type { get; set; } = TargetType.All.ToString().ToLower();
        public string? TargetGroup { get; set; } = null;
    }
}
