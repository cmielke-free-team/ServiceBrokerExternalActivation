CREATE EVENT NOTIFICATION [EventNotification2]
	ON QUEUE [dbo].[Queue2]
	FOR QUEUE_ACTIVATION
	TO SERVICE 'NotificationService', 'current database'
