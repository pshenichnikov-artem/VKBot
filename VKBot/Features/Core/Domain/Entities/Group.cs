using System.ComponentModel.DataAnnotations;

namespace VKBot.Features.Core.Domain.Entities
{
    public class Group
    {
        [Key]
        public short Id { get; set; }
        [Required]
        public string Name { get; set; } //Название группы (ИС/б-22-1-о
        [Required]
        public string Cohort {  get; set; } // Поток (ИС/б-22-з) // Вычисляемое
        [Required]
        public short GroupNumber { get; set; } //Вычисляемое
        public List<User> Users { get; set; } = new List<User>();
    }
}
