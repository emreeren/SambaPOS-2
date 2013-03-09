using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Samba.Domain.Models.Users;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Users
{
    public class TimeCardEntry 
    {
        public static TimeCardEntry Crate(int action, int userId)
        {
            return new TimeCardEntry
                {
                    Action = action,
                    DateTime = DateTime.Now,
                    UserId = userId
                };
        }

        public int Id { get; set; }
        public int Action { get; set; }
        public DateTime DateTime { get; set; }
        public int UserId { get; set; }

        public bool ShouldCreateCardEntry(User user)
        {
            if ((DateTime.Compare(DateTime, DateTime.Today) > 0))
            {
                if (Action != user.TimeCardAction)
                {
                    return true;
                }
            }

            return user.TimeCardAction == 1;
        }
    }
}
