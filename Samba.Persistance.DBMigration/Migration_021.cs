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
            var script = @"
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TimeCardEntries](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[User_Id] [int] NOT NULL,
	[Action] [int] NOT NULL,
	[DateTime] [datetime] NOT NULL,
	[Name] [nvarchar](50) NULL,
 CONSTRAINT [PK_TimeCardEntries] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO";
            Execute.EmbeddedScript(script);
        }

        public override void Down()
        {
            //do nothing
        }
    }
}
