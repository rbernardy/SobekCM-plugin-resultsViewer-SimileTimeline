/*

#depracated - was for old legacy search users

update [SobekCM_Item_Aggregation]
set Browse_Results_Display_SQL='select S.ItemID, S.Publication_Date, S.Creator, S.[Publisher.Display], S.Format, S.Edition, S.Material, S.Measurements, S.Style_Period, S.Technique, S.[Subjects.Display], S.Source_Institution, S.Donor, S.Abstract, convert(varchar,S.SortDate) as SortDate from SobekCM_Metadata_Basic_Search_Table S, @itemtable T where S.ItemID = T.ItemID order by T.RowNumber;'

*/