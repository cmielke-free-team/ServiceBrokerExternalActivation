﻿CREATE PROCEDURE [dbo].[Receive_Messages_Queue1]
	@Wait_For_Milliseconds int = 500,
	@Message_Count int = 1
AS
SET NOCOUNT ON;

WAITFOR
(
	RECEIVE TOP(@Message_Count) *
	FROM	dbo.Queue1
), TIMEOUT @Wait_For_Milliseconds

SET NOCOUNT OFF;
