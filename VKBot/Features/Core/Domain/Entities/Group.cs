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
        public string Cohort {  get; set; } // Поток (ИС/б-22-з)
        [Required]
        short GroupNumber { get; set; }

    }
}
