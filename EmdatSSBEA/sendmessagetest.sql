declare @ int = 100
while @ > 0
begin
	declare @ch1 uniqueidentifier
	begin dialog conversation @ch1
		from service [Service1]
		to service 'Service1'
		on contract [DEFAULT]
		with encryption = off;

	send on conversation @ch1
	message type [DEFAULT]
	(
		convert(varbinary(max), 'this is a test message for queue1')
	);

	declare @ch2 uniqueidentifier
	begin dialog conversation @ch2
		from service [Service2]
		to service 'Service2'
		on contract [DEFAULT]
		with encryption = off;

	send on conversation @ch2
	message type [DEFAULT]
	(
		convert(varbinary(max), 'this is a test message for queue2')
	);

	set @ = @ - 1
end

select * from Queue1 with (nolock)
select * from notificationQueue with (nolock)
select * from sys.transmission_queue
--Service Broker received an error message on this conversation. Service Broker will not transmit the message; it will be held until the application ends the conversation.

select * from sys.conversation_endpoints

;receive top(0) * from queue1











