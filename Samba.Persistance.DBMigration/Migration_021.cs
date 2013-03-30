using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
	[Migration(21)]
	public class Migration_021 : Migration
	{
		public override void Up()
		{

            Create.Column("ButtonColor").OnTable("MenuItemPropertyGroups").AsString(128).Nullable();
            Execute.Sql("Update MenuItemPropertyGroups set ButtonColor='LightGray'");

		    Create.Table("TimeCardEntries")
		          .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                  .WithColumn("Name").AsString(128).Nullable()
		          .WithColumn("UserId").AsInt32().WithDefaultValue(0)
		          .WithColumn("Action").AsInt32().WithDefaultValue(0)
		          .WithColumn("DateTime").AsDateTime().WithDefaultValue(new DateTime(2000, 1, 1));

		    Create.Column("ContactPhone").OnTable("Users").AsString(128).Nullable();
		    Create.Column("EmergencyPhone").OnTable("Users").AsString(128).Nullable();
		    Create.Column("DateOfBirth").OnTable("Users").AsString(128).Nullable();
            
		  //  Create.Column("TimeCardAction").OnTable("Users").AsInt32().WithDefaultValue(0);
            

        //         public string ContactPhone { get; set; }
        //public string EmergencyPhone { get; set; }
        //public string DateOfBirth { get; set; }
        //public int TimeCardAction { get; set; } 

//            var script = @"
//SET ANSI_NULLS ON
//GO
//
//SET QUOTED_IDENTIFIER ON
//GO
//
//CREATE TABLE [dbo].[TimeCardEntries](
//	[Id] [int] IDENTITY(1,1) NOT NULL,
//	[User_Id] [int] NOT NULL,
//	[Action] [int] NOT NULL,
//	[DateTime] [datetime] NOT NULL,
//	[Name] [nvarchar](50) NULL,
// CONSTRAINT [PK_TimeCardEntries] PRIMARY KEY CLUSTERED 
//(
//	[Id] ASC
//)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
//) ON [PRIMARY]
//
//GO";
//            Execute.EmbeddedScript(script);
		}

		public override void Down()
		{
			//do nothing
		}
	}
}
