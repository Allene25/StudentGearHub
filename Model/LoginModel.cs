using System.ComponentModel.DataAnnotations;

namespace StudentGearHub.API.Model
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Username is incorrect.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is incorrect.")]
        public string Password { get; set; }
    }
}