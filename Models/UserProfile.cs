using System;

namespace BrettBot.Models
{
    public class UserProfile
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CallbackTime { get; set; }
        public string PhoneNumber { get; set; }
        public string Bug { get; set; }
    }
}