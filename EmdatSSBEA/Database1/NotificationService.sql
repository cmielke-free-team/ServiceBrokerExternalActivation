CREATE SERVICE [NotificationService]
	ON QUEUE [dbo].[NotificationQueue]
	(
		[http://schemas.microsoft.com/SQL/Notifications/PostEventNotification]
	)
