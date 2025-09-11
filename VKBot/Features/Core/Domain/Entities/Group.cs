using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        
        [ForeignKey("Faculty")]
        public int FacultyId { get; set; }
        public Faculty Faculty { get; set; } = null!;
        
        public List<User> Users { get; set; } = new List<User>();
    }
}
