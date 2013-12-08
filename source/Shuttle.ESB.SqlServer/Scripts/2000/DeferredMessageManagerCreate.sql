CREATE TABLE [dbo].[DeferredMessage](
	[DeferTillDate] [datetime] NOT NULL,
	[TransportMessage] [image] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_DeferredMessage] ON [dbo].[DeferredMessage] 
(
	[DeferTillDate] ASC
) ON [PRIMARY]
GO
