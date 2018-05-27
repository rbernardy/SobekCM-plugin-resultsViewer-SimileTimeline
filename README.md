# SobekCM-plugin-resultsViewer-SimileTimeline
A timeline results viewer for SobekCM based on the Similie Widgets Timeline project - http://www.simile-widgets.org/. Some customization of a few files of the timeline_source_v2.3.0 have been made.

This project was originally funded by SOAS Digital Collection - Unversity of London (https://digital.soas.ac.uk/) who generously supported an open-source release after completion of the original project.

The build in the released plugin folder was created against the latest version of SobekCM (v4.11.0) as of 2017-05-27. Note: this was done prior to the offical v4.11.0 release of SobekCM.

<h3>Setup instructions:</h3>

<ol>
  <li>Install the timeline plugin folder in the instance web root/plugins folder. Enable the plugin.</li>
  <li>Execute the sql update script - SimileTimeline_SobekCM_Settings-insert_script.sql - against your database. This creates the 'Use Timeline Bundle' system-wide setting. If set to true the plugin uses the official v2.3.0 Similie Widget Timeline library bundle. Otherwise it uses the timeline_source, allowing customization, as in releases here.</li>
  <li>Execute the sql update script - SimileTimeline_ResultsViewer-install_script.sql - against your database. This script adds the timeline results type in the SobekCM_Item_Aggregation_Results_Types table.</li>
  <li>You must edit each aggregation that you wish the timeline viewer to be available for. Visit Aggregation Management to edit your aggregation - add the Timeline View in the Results tab in the Result Views section.</li>
  </ol>

<h2>CREDITS</h2>

SOAS Digital Collections - University of London. SOAS funded the original project. SOAS Digital Collections staff provided a tremendous amount of direction, guidance, suggestions, testing, and feedback - Erich Kesse, Claudia Mendias, Simon Baron, and Catherine Buxton.

Mark V. Sullivan (SobekCM's lead developer, CIO & Application Architect - Sobek Digital Hosting & Consulting, LLC) - Mark made significant code contrubtions to the timeline plugin project, along with changes to SobeKCM's code base in support of the results viewer.

Simile Widgets - Timeline: An open-source 'spin-off' from the original SIMILE project at MIT. http://www.simile-widgets.org/. This results viewer project was based on the timeline widget (v2.3.0).

<hr/>

Richard Bernardy - rbernard@usf.edu - 05/27/2018.

I'd appreciate a courtesy notification by email if you find this plugin useful and are using it in your SobekCM-based respository.
