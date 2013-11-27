using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Users
{
    public class EmpScheduleEntry : IEntity
    {
        public static EmpScheduleEntry Create(int userId,  DateTime startTime, DateTime endTime)
        {
            return new EmpScheduleEntry
            {
                Name = "",
                UserId = userId,
                StartTime = startTime,
                EndTime  = endTime
            };
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int UserId { get; set; }
    }
}
