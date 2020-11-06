using System;
using System.ComponentModel.DataAnnotations;


namespace UserAPI.Models
{
    public class BlackItem
    {
        [Key]
        public string Ip {set; get;}

        public DateTime LastTime {set; get;}
    }
}
