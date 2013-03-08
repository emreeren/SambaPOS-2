using System;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Users
{
    public class PinData
    {
        public string PinCode { get; set; }
        public int TimeCardAction { get; set; } // 0 None, 1 ClockIn, 2 ClockOut
    }

    public class User : IEntity
    {
        public User()
        {

        }

        public User(string name, string pinCode)
        {
            Name = name;
            PinCode = pinCode;
            _userRole = UserRole.Empty;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] LastUpdateTime { get; set; }
        public string PinCode { get; set; }
        public string Address { get; set; }
        public string ContactPhone { get; set; }
        public string EmergencyPhone { get; set; }
        public string DateOfBirth { get; set; }
        public int TimeCardAction { get; set; }  // 0 None, 1 ClockIn, 2 ClockOut
        
        private UserRole _userRole;
        public virtual UserRole UserRole
        {
            get { return _userRole; }
            set { _userRole = value; }
        }

        private static readonly User _nobody = new User("*", "");
        public static User Nobody { get { return _nobody; } }

        public string UserString
        {
            get { return Name; }
        }

        public TimeCardEntry CreateTimeCardEntry()
        {
            return TimeCardEntry.Crate(TimeCardAction, Id);
        }

        public bool ShouldCreateCardEntry(TimeCardEntry currentCardEntry)
        {
            var result = false;

            if (currentCardEntry != null && (DateTime.Compare(currentCardEntry.DateTime, DateTime.Today) > 0))
            {
                if (currentCardEntry.Action != TimeCardAction)
                {
                    result = true;
                }
            }

            if (currentCardEntry == null && TimeCardAction == 1) //Clock In
            {
                result = true;
            }

            return result;
        }
    }
}
