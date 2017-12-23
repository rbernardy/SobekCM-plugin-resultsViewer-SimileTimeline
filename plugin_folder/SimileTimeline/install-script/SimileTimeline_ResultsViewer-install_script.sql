-- Add the TIMELINE view as a viewer type
if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType = 'SIMILETIMELINE' ))
begin
	
	insert into SobekCM_Item_Aggregation_Result_Types ( ResultType, DefaultOrder, DefaultView )
	values ( 'SIMILETIMELINE', 15, 'true' );

end;
GO
