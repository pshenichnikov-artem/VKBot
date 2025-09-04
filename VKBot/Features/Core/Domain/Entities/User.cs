using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Domain.Entities
{
    public class User
    {
        [Key]
        public long VkUserId { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Role { get; set; } = UserRole.Student.ToString().ToLower();
        [Required]
        public bool IsConfirmed { get; set; } = false;
        public bool IsBlocked { get; set; } = false;
        //Группа
        [Required]
        public short GroupId { get; set; }
        public Group? Group { get; set; } = null;
    }
}
