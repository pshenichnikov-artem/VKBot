using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.Xml;

namespace VKBot.Features.Core.Domain.Entities
{
    public class Group
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public string Name { get; set; } //Название группы (ИС/б-22-1-о
        [Required]
        public string Cohort {  get; set; } // Поток (ИС/б-22-з) // Вычисляемое
        [Required]
        public short GroupNumber { get; set; } //Вычисляемое
        public List<User> Users { get; set; } = new List<User>();
        public ICollection<MessageGroup> MessageGroups { get; set; } = new List<MessageGroup>();
    }
}
