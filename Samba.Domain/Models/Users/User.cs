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
       // public decimal Wages { get; set; }

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

        public TimeCardEntry CreateTimeCardEntry(int timeCardAction)
        {
            return TimeCardEntry.Create(timeCardAction, Id);
        }

        public bool ShouldCreateCardEntry(TimeCardEntry currentCardEntry, int timeCardAction)
        {
            var result = false;
            
            if (currentCardEntry != null )
            {
                //any entry today
                if (DateTime.Compare(currentCardEntry.DateTime, DateTime.Today) > 0)
                {
                    if (currentCardEntry.Action != timeCardAction)
                    {
                        result = true;

                    }
                }else if (timeCardAction == 1) //previous entry exist and did not clock out, allow to enter next entry if clock in
                {
                    result = true;
                }
            }else if ( timeCardAction == 1) //Clock In
            {
                result = true;
            }

            return result;
        }
    }
}
