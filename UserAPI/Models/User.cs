using System;
using System.ComponentModel.DataAnnotations;


namespace UserAPI.Models
{
    public class User
    {
        [MaxLength(256)]
        public string Uid { get; set; }
        [Key]
        [MaxLength(256)]
        public string Username { get; set; }
        [MaxLength(256)]
        public string Name { get; set; } // display name
        [MaxLength(256)]
        public string Password { get; set; }
    }
}
