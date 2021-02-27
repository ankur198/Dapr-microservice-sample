using System.ComponentModel.DataAnnotations;
using static UserService.Models.User;

namespace UserService.ViewModels
{
    public class UserCreationVM
    {
        [Required]
        public string? Name { get; set; }
        [Required]
        public UserType Type { get; set; }
    }
}