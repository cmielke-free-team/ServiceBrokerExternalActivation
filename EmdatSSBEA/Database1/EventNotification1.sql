CREATE EVENT NOTIFICATION [EventNotification1]
	ON QUEUE [dbo].[Queue1]
	FOR QUEUE_ACTIVATION
	TO SERVICE 'NotificationService', 'current database'
