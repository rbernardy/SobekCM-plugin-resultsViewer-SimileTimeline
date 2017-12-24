-- Add the TIMELINE view as a viewer type
if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType = 'TIMELINE' ))
begin
	
	insert into SobekCM_Item_Aggregation_Result_Types ( ResultType, DefaultOrder, DefaultView )
	values ( 'TIMELINE', 10, 'true' );

end;
GO
