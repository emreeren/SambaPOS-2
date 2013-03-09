using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Samba.Domain.Models.Users;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Users
{
    

    public class TimeCardEntry : IEntity
    {
        public enum TimeCardActionEnum
        {
            None = 0,
            ClockIn = 1,
            ClockOut = 2
        };
        public TimeCardEntry()
            : this(0, 0)
         {
             //Name = Settings.Terminal.DefaultTerminal.Name;
             //Id = 0;
             //User_Id = 0;
             //DateTime = DateTime.Now;
             //Action = 0;
         }

         public TimeCardEntry(TimeCardActionEnum action, int userId)
         {
             Action = (int)action;
           //  _user = user;
             DateTime = DateTime.Now;
             Name = "";
             Id = 0;
             User_Id = userId;
         }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Action { get; set; }
        public DateTime DateTime { get; set; }
        public int User_Id { get; set; }

    //    private User _user;
     //   public virtual User User
    //    {
     //       get { return _user; }
     //       set { _user = value; }
     //   }      
    }
}
