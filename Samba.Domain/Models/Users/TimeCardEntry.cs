using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Samba.Domain.Models.Users;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Users
{
    public class TimeCardEntry:IEntity
    {
        public static TimeCardEntry Create(int action, int userId)
        {
            return new TimeCardEntry
                {
                    Action = action,
                    Name = "",
                    DateTime = DateTime.Now,
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
