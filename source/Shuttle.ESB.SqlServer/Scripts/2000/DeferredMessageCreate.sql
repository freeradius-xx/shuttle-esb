CREATE TABLE [dbo].[DeferredMessage](
	[SequenceId] [int] IDENTITY(1,1) NOT NULL,
	[DeferTillDate] [datetime] NOT NULL,
	[tMessageBody] [image] NOT NULL,
 CONSTRAINT [PK_DeferredMessage] PRIMARY KEY CLUSTERED 
(
	[SequenceId] ASC
) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
