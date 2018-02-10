-- Add the TIMELINE SYSTEM WIDE SETTINGS
if ( not exists ( select 1 from SobekCM_Settings where Setting_Key='Use Timeline Bundle' ))
begin
	insert into SobekCM_Setting ( Setting_Key,Setting_Value,TabPage,Heading,Hidden,Reserved,Help,Options,Dimensions )
	values ( 'Use Timeline Bundle',false,'System / Server Settings','Timeline',0,0,'If true the Timeline JS bundle will be used, if false the source JS will be used','true|false',null );
end;
GO