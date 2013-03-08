using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Samba.Domain.Models.Users;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Users
{
    public enum TimeCardActionEnum
    {
        None = 0,
        ClockIn = 1,
        ClockOut = 2
    };

    public class TimeCardEntry : IEntity
    {
        public static TimeCardEntry Crate(TimeCardActionEnum action, int userId)
        {
            return new TimeCardEntry
                {
                    Action = (int)action,
                    DateTime = DateTime.Now,
                    Name = "",
                    UserId = userId
                };
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Action { get; set; }
        public DateTime DateTime { get; set; }
        public int UserId { get; set; }
    }
}
