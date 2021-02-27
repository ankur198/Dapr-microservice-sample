using System;
using System.ComponentModel.DataAnnotations;

namespace UserService.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        [Required]
        public UserType Type { get; set; }
        public enum UserType
        {
            Consumer, Professional, Admin
        }
    }

}