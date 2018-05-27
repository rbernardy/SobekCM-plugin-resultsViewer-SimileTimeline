using System.Text;
using System.Net;
using System.Web.UI.WebControls;
using SobekCM.Core.FileSystems;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.ResultsViewer;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data;

namespace SimileTimeline
{
    public class SimileTimeline_ResultsViewer: abstract_ResultsViewer
    {
        public static Boolean debug=false;
        private string source_url;
        private static string path_log;
        private static bool Verify_Thumbnail_Files = false;
        private static readonly string timeline_version = "20180527.1743";

        /// <summary> Constructor for a new instance of the SimilineTimeline_ResultsViewer class </summary>
        public SimileTimeline_ResultsViewer() : base()
        {
            if (Dns.GetHostName() == "SOB-EXHIBIT01")
            {
                // custom path for dev/test
                path_log = @"D:\rbernard\Dropbox\SimileTimeline-ResultsViewer.log.txt";
            }
            else
            {
                path_log=Path.GetTempPath() + @"\SimileTimeline-ResultsViewer.log.txt";
            }

            // If the file exists, delete it for this next request
            if (File.Exists(path_log))
            {
                try
                {
                    File.Delete(path_log);
                }
                catch ( Exception ee )
                {

                }
            }

            // if debug.txt file exists in plugin folder massive debug statements will be written
            String path_debug = HttpContext.Current.Server.MapPath("~") + @"\plugins\Timeline\debug.txt";
            
            if (File.Exists(path_debug))
            {
                debug = true;
            }
            else
            {
                debug = false;
            }
        }

        /// <summary> Flag indicates if this result view is sortable </summary>
        /// <value>This value can be override by child classes, but by default this TRUE </value>
        public override bool Sortable { get { return false; } }

        public static int RoundUp(int value)
        {
            return 10 * ((value + 9) / 10);
        }

        public static int RoundDown(int value, Custom_Tracer tracer)
        {
            int myvalue = value - value % 10;
            tracer.Add_Trace("timeline", "RoundDown: " + value + " down to " + myvalue + ".");
            return myvalue;
        }

        /// <summary> Adds the controls for this result viewer to the place holder on the main form </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the the bulk of the result viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Sorted tree with the results in hierarchical structure with volumes and issues under the titles and sorted by serial hierarchy </returns>
        public override void Add_HTML(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("SimileTimeline_ResultsViewer", "timeline version = " + timeline_version);

            logme("Add_HTML is called...");
     
            SimileDate sd;

            string dir_resource = null, mydate, mymonth, myday, myyear, path;
            string msg = null, packageid = null, myAbstract = "", mySubjects="",bibid,vid;
            string SortDateString = "";
            List<int> yearsRepresented = new List<int>();
            List<int> yearsRepresentedDistinct = new List<int>();
            List<int> decades = new List<int>();
            List<int> decadesDistinct = new List<int>();
            int count_missing_date = 0, pagedresults_itemcount=0,titleresult_itemcount=0;
            int count_total = 0;

            Dictionary<int, DateTime> earliest_time_by_decade = new Dictionary<int, DateTime>();
            Dictionary<int, int> decades_calculated = new Dictionary<int, int>();

            List<SimileDate> dateList = new List<SimileDate>();

            Literal mainLiteral = null;

            Random rand = new Random();
            Boolean hasMissingDates = false;
            
            int tlsn = rand.Next(1, 999999);
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            DateTime convertedDate, dateFromZero;

            int i = 0, yearnum, monthnum, daynum;
            
            if (Tracer != null)
            {
                Tracer.Add_Trace("SimileTimeline_ResultsViewer.Add_HTML", "Rendering results in timeline view");
            }

            // Start this table
            StringBuilder resultsBldr = new StringBuilder(5000);

            // If results are null, or no results, return empty string
            if ((PagedResults == null) || (ResultsStats == null) || (ResultsStats.Total_Items <= 0))
                return;

            StringBuilder datajs = new StringBuilder();

            if (debug) logme("PagedResults count=[" + PagedResults.Count + "].");

            datajs.Append("var timeline_data = {");
            datajs.Append("'dateTimeFormat': 'iso8601',");
            datajs.Append("'wikiURL': \"http://simile.mit.edu/shelf/\",");
            datajs.Append("'wikiSection': \"Simile Timeline\",");
            datajs.Append("\r\n");
            datajs.Append("'events' : [");

            // Get the text search redirect stem and (writer-adjusted) base url 
            string textRedirectStem = Text_Redirect_Stem;
            string base_url = RequestSpecificValues.Current_Mode.Base_URL;

            if (RequestSpecificValues.Current_Mode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn)
                base_url = RequestSpecificValues.Current_Mode.Base_URL + "l/";

            // Should the publication date be shown?
            bool showDate = RequestSpecificValues.Current_Mode.Sort >= 10;

            if (debug) logme("RequestSpecificValues.Current_Mode.Sort=[" + RequestSpecificValues.Current_Mode.Sort + "].");

            resultsBldr.AppendLine("<!-- RequestSpecificValues.Current_Mode.Sort=[" + RequestSpecificValues.Current_Mode.Sort + "]. -->");

            resultsBldr.AppendLine("<script type=\"text/javascript\">");
            resultsBldr.AppendLine("  jQuery('#itemNavForm').prop('action','').submit(function(event){ event.preventDefault(); });");
            resultsBldr.AppendLine("</script>");

            //Add the necessary JavaScript, CSS files
            resultsBldr.AppendLine("  <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Sobekcm_Thumb_Results_Js + "\"></script>");

            iSearch_Item_Result itemResult;

            pagedresults_itemcount=PagedResults.Count;

            Tracer.Add_Trace("SimileTimeline_ResultsViewer", "PagedResults.Count=[" + PagedResults.Count + "].");

            foreach (iSearch_Title_Result titleResult in PagedResults)
            {
                titleresult_itemcount = titleResult.Item_Count;
               
                bool multiple_title = titleResult.Item_Count > 1;
                if (debug) resultsBldr.AppendLine("<!-- titleResult.Item_Count=[" + titleResult.Item_Count + "].-->");

                // Get the first item
                itemResult = titleResult.Get_Item(0);

                bibid = titleResult.BibID;
                vid = itemResult.VID;
        
                // Determine the internal link to the first (possibly only) item
                string internal_link = base_url + bibid + "/" + vid + textRedirectStem;
                packageid = bibid + "/" + vid;

                //resultsBldr.AppendLine("<!-- internal_link=[" + internal_link + "].-->");

                // For browses, just point to the title
                if ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) && (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Browse_Info))
                    internal_link = base_url + titleResult.BibID + textRedirectStem;

                if (debug) resultsBldr.AppendLine("<!-- internal_link=[" + internal_link + "].-->");
                //resultsBldr.AppendLine("<!-- snippet=[" + titleResult.Snippet + "].-->");

                dir_resource = SobekFileSystem.Resource_Network_Uri(bibid, vid);
                if (debug) logme("dir_resource (SobekFileSystem.Resource_Network_Uri)=[" + dir_resource + "].");

                source_url = UI_ApplicationCache_Gateway.Settings.Servers.Image_URL + SobekFileSystem.AssociFilePath(bibid, vid).Replace("\\", "/");
                if (debug) logme("source_url (UI_ApplicationCache_Gateway.Settings.Server.Image_URL=[" + source_url + "].");

                string title;

                // metadata Title

                title = itemResult.Title; ;
                title = Regex.Replace(title, @"<[^>]+>|&nbsp;", "").Trim();

                // Add the title
                // path = source_url + "/" + firstItemResult.MainThumbnail;
                // that doesn't work when granting access through a router / local lan
                if (debug) logme("source_url=[" + source_url + "].");

                // if (debug) logme("titleResult.GroupThumbnail=[" + titleResult.GroupThumbnail + "].");
                if (debug) logme("thumbnail from item[" + itemResult.MainThumbnail + "].");

                // Check if the thumbnail exists, if the flag is set for that
                if ((Verify_Thumbnail_Files) && (!File.Exists(dir_resource + @"\" + itemResult.MainThumbnail)))
                {
                    if (debug) logme("Thumbnail DOESN'T exist=[" + dir_resource + @"\" + itemResult.MainThumbnail + "].");
                    path = "http://" + getMyIP() + "/default/images/misc/nothumb.jpg";
                }
                else
                {
                    if (debug) logme("Thumbnail EXISTS=[" + dir_resource + @"\" + itemResult.MainThumbnail + "].");
                    // thumbnail filename was titleResult.GroupThumbnail
                    // path = "http://" + getMyIP() + "/" + source_url.Substring(source_url.IndexOf("content")) + itemResult.MainThumbnail;

                    string thumb = titleResult.BibID.Substring(0, 2) + "/" + titleResult.BibID.Substring(2, 2) + "/" + titleResult.BibID.Substring(4, 2) + "/" + titleResult.BibID.Substring(6, 2) + "/" + titleResult.BibID.Substring(8) + "/" + itemResult.VID + "/" + (itemResult.MainThumbnail).Replace("\\", "/").Replace("//", "/");

                    path = UI_ApplicationCache_Gateway.Settings.Servers.Image_URL + thumb;
                }

                if (debug) logme("URL (path) to thumbnail=[" + path + "].");
                
                SortDateString = getMetadata(titleResult, ResultsStats, "Timeline Date").Trim();
                
                if (debug) logme(msg);
                
                if (SortDateString.Length > 0 && !SortDateString.Contains("N/A"))
                {
                    convertedDate = DateTime.Parse(SortDateString);

                    if (debug) logme(packageid + ": convertedDate=[" + convertedDate.ToString("yyyy-MM-dd") + "].");

                    yearnum = convertedDate.Year;
                    monthnum = convertedDate.Month;
                    daynum = convertedDate.Day;

                    yearsRepresented.Add(yearnum);

                    // Get the decade here
                    int decade = RoundDown(yearnum, Tracer);

                    // Does this decade already exist?
                    if ( decades_calculated.ContainsKey(decade))
                    {
                        // Then need to see if this is earlier than the last first date
                        if (earliest_time_by_decade[decade] > convertedDate)
                            earliest_time_by_decade[decade] = convertedDate;

                        // Also, increment the number of dates in this decade
                        decades_calculated[decade] = decades_calculated[decade] + 1;
                    }
                    else
                    {
                        // New decade found
                        decades_calculated[decade] = 1;
                        earliest_time_by_decade[decade] = convertedDate;
                    }

                    // Create this simile date object
                    sd = new SimileDate();
                    sd.yearnum = yearnum;
                    sd.monthnum = monthnum;
                    sd.daynum = daynum;

                    dateList.Add(sd);
                    
                    //msg = packageid + ": Good date: SortDateString=[" + SortDateString + "].";
                    //Tracer.Add_Trace("SimileTimeline_ResultsViewer", msg);
                }
                else
                {
                    msg = packageid + ": Bad/no date: SortDateString=[" + SortDateString + "].";
                    Tracer.Add_Trace("SimileTimeline_ResultsViewer", msg);

                    yearnum = -1;
                    monthnum = -1;
                    daynum = -1;
                    hasMissingDates = true;
                }
                
                if (debug) logme(packageid + ": yearnum=[" + yearnum + "], monthnum=[" + monthnum + "], daynum=[" + daynum + "].");

                // metadata **********************************************************************************************************************
                // metadata Abstract

                // Get the description for this item
                const string VARIES_STRING = "<span style=\"color:Gray\">( varies )</span>";
                StringBuilder singleResultBldr = new StringBuilder();
                singleResultBldr.Append("<div style=\"text-align:left\"><dl class=\"sbkBrv_SingleResultDescList\">");
                if ((RequestSpecificValues.Current_User != null) && (RequestSpecificValues.Current_User.LoggedOn) && (RequestSpecificValues.Current_User.Is_Internal_User))
                {
                    singleResultBldr.Append("<dt>BibID:</dt><dd>" + titleResult.BibID + "</dd>");

                    if (titleResult.OPAC_Number > 1)
                    {
                        singleResultBldr.Append("<dt>OPAC:</dt><dd>" + titleResult.OPAC_Number + "</dd>");
                    }

                    if (titleResult.OCLC_Number > 1)
                    {
                        singleResultBldr.Append("<dt>OCLC:</dt><dd>" + titleResult.OCLC_Number + "</dd>");
                    }
                }

                for (int j = 0; j < ResultsStats.Metadata_Labels.Count; j++)
                {
                    string field = ResultsStats.Metadata_Labels[j];

                    // Somehow the metadata for this item did not fully save in the database.  Break out, rather than
                    // throw the exception
                    if ((titleResult.Metadata_Display_Values == null) || (titleResult.Metadata_Display_Values.Length <= j))
                        break;

                    string metadata_value = titleResult.Metadata_Display_Values[j];
                    SobekCM.Core.Search.Metadata_Search_Field thisField = UI_ApplicationCache_Gateway.Settings.Metadata_Search_Field_By_Name(field);
                    string display_field = string.Empty;

                    if (thisField != null)
                        display_field = thisField.Display_Term;

                    if (display_field.Length == 0)
                        display_field = field.Replace("_", " ");

                    if (metadata_value == "*")
                    {
                        singleResultBldr.Append("<dt>" + UI_ApplicationCache_Gateway.Translation.Get_Translation(display_field, RequestSpecificValues.Current_Mode.Language) + ":</dt><dd>" + HttpUtility.HtmlDecode(VARIES_STRING) + "</dd>");
                    }
                    else if (metadata_value.Trim().Length > 0)
                    {
                        if (metadata_value.IndexOf("|") > 0)
                        {
                            bool value_found = false;
                            string[] value_split = metadata_value.Split("|".ToCharArray());

                            foreach (string thisValue in value_split)
                            {
                                if (thisValue.Trim().Trim().Length > 0)
                                {
                                    if (!value_found)
                                    {
                                        singleResultBldr.Append("<dt>" + UI_ApplicationCache_Gateway.Translation.Get_Translation(display_field, RequestSpecificValues.Current_Mode.Language) + ":</dt>");
                                        value_found = true;
                                    }
                                    singleResultBldr.Append("<dd>" + HttpUtility.HtmlDecode(thisValue) + "</dd>");
                                }
                            }
                        }
                        else
                        {
                            singleResultBldr.Append("<dt>" + UI_ApplicationCache_Gateway.Translation.Get_Translation(display_field, RequestSpecificValues.Current_Mode.Language) + ":</dt><dd>" + HttpUtility.HtmlDecode(metadata_value) + "</dd>");
                        }
                    }
                }

                singleResultBldr.Append("</dl></div>");
                myAbstract = singleResultBldr.ToString();

                // *******************************************************************************************************************************

                if (yearnum == -1 && monthnum == -1 && daynum == -1)
                {
                    count_missing_date++;
                    if (debug) logme(packageid + " was missing a date, skipping. count_missing_date=" + count_missing_date + ".");
                }
                else
                {
                    if (debug) logme(packageid + " being added as an event.");

                    count_total++;
                    addToDataJS(datajs, yearnum, monthnum, daynum, title, myAbstract, bibid, vid, path);
                    if (debug) logme("Added event, count_total=" + count_total);
                }
                
                // end of item loop

                if (debug) logme("There are [" + titleResult.Item_Count + "] items for [" + bibid + "].");
                if (debug) logme("Done processing bibid=[" + bibid + "], vid=[" + vid + "].\r\n\r\n");
                if (debug) logme("Total count added=" + count_total + ", missing count date=" + count_missing_date);

               // Tracer.Add_Trace("SimileTimeline_ResultsViewer.Add_HTML", "Done processing bibid=[" + bibid + "], vid=[" + vid + "]");
            }

            if (debug) logme("main processing loop completed.");
            if (debug) logme("datajs length = " + datajs.Length + ".");

            // end of titleResults loop
            datajs.Remove(datajs.Length - 3, 3);
           // datajs = datajs.Substring(0, datajs.Length - 3);
            datajs.Append("]}");

            msg = "webroot=[" + HttpContext.Current.Server.MapPath("~") + "].";
            if (debug) logme(msg);
            Tracer.Add_Trace("SimileTimeline_ResultsViewer", msg);

            File.WriteAllText(HttpContext.Current.Server.MapPath("~") + @"\temp\" + tlsn + "-" + unixTimestamp + ".js", datajs.ToString());

            resultsBldr.AppendLine("<script src=\"" + @"/temp/" + tlsn + "-" + unixTimestamp + ".js" + "\" type=\"text/javascript\"></script>");

            if (debug) logme("count of yearsRepresented=[" + yearsRepresented.Count + "].");

            if (count_total==0)
            {
                resultsBldr.AppendLine("<br/><p id=\"warningzero\">Note: For the timeline, out of " + pagedresults_itemcount.ToString("#,##0") + " results in this page all were missing dates and were skipped. Proceed to the next page (if any).</p>");
                msg = "All results in this page were missing dates and were skipped, returning.";
                Tracer.Add_Trace("SimileTimeline_ResultsViewer", msg);
                if (debug) logme(msg);

                mainLiteral = new Literal { Text = resultsBldr.ToString() };
                MainPlaceHolder.Controls.Add(mainLiteral);

                return;
            }

            Tracer.Add_Trace("timeline", "sorting yearsRepresented");

            yearsRepresented.Sort();
            int mymin = yearsRepresented[i];
            int mymax = yearsRepresented[yearsRepresented.Count - 1];
            int sum = 0;

            var g = yearsRepresented.GroupBy(ig => ig);

            Tracer.Add_Trace("timeline", "count in yearsRepresented=" + g.Count());

            // Get the middle point
            foreach (int myvalue in yearsRepresented)
            {
                sum += myvalue;
            }

            int myavg = sum / yearsRepresented.Count;

            Tracer.Add_Trace("timeline","min of yearsRepresented=" + mymin);
            Tracer.Add_Trace("timeline","max of yearsRepresented=" + mymax);
            Tracer.Add_Trace("timeline","average of yearsRepresented=" + myavg);

            if (debug) logme("Adding controls.");

            resultsBldr.AppendLine("<button id=\"buttonControls\" class=\"btn\" onclick=\"javascript:toggleControls(event);\">Hide Controls</button>");
            resultsBldr.AppendLine("<div id=\"tlloadingmsg\">Loading...</div>");
            resultsBldr.AppendLine("\t\t\t <div class=\"controls\" id=\"controls\">");
            
            try
            {
                decadesDistinct = decades_calculated.Keys.ToList();
                decadesDistinct.Sort();

                foreach (int mydecade in decadesDistinct)
                {
                    Tracer.Add_Trace("timeline", "decadesDistinct=" + mydecade);
                }
            }
            catch (Exception e)
            {
                if (debug) logme("exception trying to get decadesDistinct [" + e.Message + "].");
            }

            // sort dateList by year, month, date

            var dateListSorted = from mydates in dateList
                                 orderby mydates.yearnum, mydates.monthnum, mydates.daynum
                                 select mydates;

            foreach (SimileDate mydate2 in dateList)
            {
                Tracer.Add_Trace("timeline","dateList: " + mydate2.yearnum + "-" + mydate2.monthnum + "-" + mydate2.daynum);
            }

            // jump
      
            resultsBldr.Append("<p id=\"gotolinks\">Go to: ");

            int theDecade,idx=0;
            
            foreach (int decade in decadesDistinct)
            {
                idx++;
 
                if (decade > mymax)
                {
                    theDecade = mymax;
                }
                else
                {
                    theDecade = decade;
                }

                DateTime decades_earliest_date = earliest_time_by_decade[decade];
                SimileDate firstDate = new SimileDate(decades_earliest_date);
      
                resultsBldr.AppendLine("<a href=\"javascript:centerSimileAjax('" + firstDate.monthnum + "," + firstDate.daynum + "," + firstDate.yearnum + "')\">" + theDecade + "</a>&nbsp;&nbsp;&nbsp;");       
            }

            resultsBldr.AppendLine("</p>\r\n\r\n");
         
            // end jump

            resultsBldr.AppendLine("</div> <!-- end controls div -->\r\n");

            resultsBldr.AppendLine("<div id = \"doc3\" class=\"yui-t7\">");
            
            resultsBldr.AppendLine("\t<div id = \"bd\" role= \"main\">");
            resultsBldr.AppendLine("\t\t<div class=\"yui-g\">");
            resultsBldr.AppendLine("\t\t\t<div id = 'tl'>");
            resultsBldr.AppendLine("\t\t\t</div> <!-- end of tl -->");
            resultsBldr.AppendLine("\t\t</div> <!-- end of yui-g -->");
   
            resultsBldr.AppendLine("\t\t\t<p style=\"display:none;\">Thanks to the <a href=''>Simile Timeline project</a>. Timeline version <span id='tl_ver'>");
            resultsBldr.AppendLine("\t\t\t<script>Timeline.writeVersion('tl_ver');</script></span></p>");
  
            resultsBldr.AppendLine("\t</div> <!-- end of bd -->");

            if (debug) logme("Adding onLoad function...");

            resultsBldr.AppendLine("<script type=\"text/javascript\">");
            resultsBldr.AppendLine("var tl;");
            resultsBldr.AppendLine("function onLoad() {");
            resultsBldr.AppendLine("console.log(\"onload function...\");");
            resultsBldr.AppendLine("var tl_el = document.getElementById(\"tl\");");
            resultsBldr.AppendLine("var eventSource1 = new Timeline.DefaultEventSource();");

            // theme

            resultsBldr.AppendLine("console.log(\"create theme...\");");
             
            resultsBldr.AppendLine("var theme1 = Timeline.ClassicTheme.create();");
            resultsBldr.AppendLine("theme1.autoWidth = true; // Set the Timeline's \"width\" automatically.");
            resultsBldr.AppendLine("// Set autoWidth on the Timeline's first band's theme,");
            resultsBldr.AppendLine("// will affect all bands.");
            
            resultsBldr.AppendLine("theme1.timeline_start = new Date(Date.UTC(" + (Math.Abs(mymin) - 100) + ",0,0));");
          
            resultsBldr.AppendLine("theme1.timeline_stop = new Date(Date.UTC(" + (Math.Abs(mymax) + 100) + ", 0, 1));");
            resultsBldr.AppendLine("theme1.mouseWheel='scroll';");
            resultsBldr.AppendLine("theme1.event.bubble.width = 450;");
        
            resultsBldr.AppendLine("console.log(\"theme1 object\");");
            resultsBldr.AppendLine("console.log(theme1);");

            resultsBldr.AppendLine("var d = Timeline.DateTime.parseGregorianDateTime(\"" + myavg + "\")");

            // bands

            resultsBldr.AppendLine("console.log(\"createBandInfo...\");");

            // need it global, removing var
            resultsBldr.AppendLine("bandInfos = [");

            // Decade 

            resultsBldr.AppendLine("Timeline.createBandInfo({");
            resultsBldr.AppendLine("width:\"10%\",");
            resultsBldr.AppendLine("intervalUnit: Timeline.DateTime.DECADE,");
            resultsBldr.AppendLine("intervalPixels: 50,");
            resultsBldr.AppendLine("eventSource: eventSource1,");
            resultsBldr.AppendLine("date: d,");
            resultsBldr.AppendLine("theme: theme1,");
            resultsBldr.AppendLine("layout: 'overview'  // original, overview, detailed");
            resultsBldr.AppendLine("}),");

            // Year

            resultsBldr.AppendLine("Timeline.createBandInfo({");
            resultsBldr.AppendLine("width: \"10%\",");
            resultsBldr.AppendLine("intervalUnit: Timeline.DateTime.YEAR,");
            resultsBldr.AppendLine("intervalPixels: 50,");
            resultsBldr.AppendLine("eventSource: eventSource1,");
            resultsBldr.AppendLine("date: d,");
            resultsBldr.AppendLine("theme: theme1,");
            resultsBldr.AppendLine("layout: 'overview'  // original, overview, detailed");
            resultsBldr.AppendLine("}),");

            // Month

            resultsBldr.AppendLine("Timeline.createBandInfo({");
            resultsBldr.AppendLine("width:\"10%\",");
            resultsBldr.AppendLine("intervalUnit: Timeline.DateTime.MONTH,");
            resultsBldr.AppendLine("intervalPixels: 100,");
            resultsBldr.AppendLine("eventSource: eventSource1,");
            resultsBldr.AppendLine("date: d,");
            resultsBldr.AppendLine("theme: theme1,");
            resultsBldr.AppendLine("layout: 'overview'  // original, overview, detailed");
            resultsBldr.AppendLine("}),");

            // Day

            resultsBldr.AppendLine("Timeline.createBandInfo({");
            resultsBldr.AppendLine("width: \"70% \", ");
            resultsBldr.AppendLine("intervalUnit: Timeline.DateTime.DAY, ");
            resultsBldr.AppendLine("intervalPixels: 150,");
            resultsBldr.AppendLine("eventSource: eventSource1,");
            resultsBldr.AppendLine("date: d,");
            resultsBldr.AppendLine("theme: theme1,");
            resultsBldr.AppendLine("layout: 'original'  // original, overview, detailed");
            resultsBldr.AppendLine("}),");
       
            resultsBldr.AppendLine("];");

            resultsBldr.AppendLine("bandInfos[0].syncWith = 3;");
            resultsBldr.AppendLine("bandInfos[0].highlight = true;");
            resultsBldr.AppendLine("bandInfos[1].syncWith = 3;");
            resultsBldr.AppendLine("bandInfos[1].highlight = true;");
            resultsBldr.AppendLine("bandInfos[2].syncWith = 3;");
            resultsBldr.AppendLine("bandInfos[2].highlight = true;");
            resultsBldr.AppendLine("bandInfos[3].highlight = true;");
     
            // end of band

            resultsBldr.AppendLine("console.log(\"the bandInfos.\");");
            resultsBldr.AppendLine("console.log(bandInfos);");

            resultsBldr.AppendLine("console.log(\"create the timeline.\");");
            resultsBldr.AppendLine("// create the Timeline");
            resultsBldr.AppendLine("tl = Timeline.create(tl_el, bandInfos, Timeline.HORIZONTAL);");

            resultsBldr.AppendLine("var url = '.'; // The base url for image, icon and background image");
            resultsBldr.AppendLine("console.log(\"load the json data from [\" + url + \"].\");");
            resultsBldr.AppendLine("// references in the data");
            resultsBldr.AppendLine("eventSource1.loadJSON(timeline_data, url); // The data was stored into the");
            resultsBldr.AppendLine("// timeline_data variable.");

            resultsBldr.AppendLine("console.log(\"display the timeline.\");");
            resultsBldr.AppendLine("tl.layout(); // display the Timeline");

            // setup the controls
            resultsBldr.AppendLine("console.log(\"setup the filter/highlight controls.\");");
            //resultsBldr.AppendLine("var theme = Timeline.ClassicTheme.create();");

            resultsBldr.AppendLine("theme1.mouseWheel='scroll';");

            resultsBldr.AppendLine("setupFilterHighlightControls(document.getElementById(\"controls\"), tl, [0,0], theme1);");
            //
            resultsBldr.AppendLine("}");
            resultsBldr.AppendLine("");
            resultsBldr.AppendLine("var resizeTimerID = null;");
            resultsBldr.AppendLine("function onResize() {");
            resultsBldr.AppendLine("console.log(\"onresize...\");");
            resultsBldr.AppendLine("if (resizeTimerID == null)");
            resultsBldr.AppendLine("{");
            resultsBldr.AppendLine("resizeTimerID = window.setTimeout(function() {");
            resultsBldr.AppendLine("resizeTimerID = null;");
            resultsBldr.AppendLine("tl.layout();");
            resultsBldr.AppendLine("}, 500);");
            resultsBldr.AppendLine("}");
            resultsBldr.AppendLine("}");
            resultsBldr.AppendLine("</script>");

            //< body onload = "onLoad();" onresize = "onResize();" >

            if (debug) logme("Adding document ready...");

            resultsBldr.AppendLine("<script>");
            resultsBldr.AppendLine("$(document).ready(function()");
            resultsBldr.AppendLine("{");
            resultsBldr.AppendLine("\tconsole.log(\"ready!\");");
            resultsBldr.AppendLine("\tconsole.log(\"calling onload...\");");
            resultsBldr.AppendLine("\tonLoad();");
            resultsBldr.AppendLine("\tconsole.log(\"returned from calling onload...\");");
            resultsBldr.AppendLine("\t$(\"body\").attr(\"onresize\",\"onResize();\")");
            resultsBldr.AppendLine("\t$(\"#controls > table > tbody > tr > td:nth-child(2):first\").attr('colspan','4').css('text-align','left').css('padding-left','9%');");
            // .timeline-event-bubble-title > a:nth-child(1)

            resultsBldr.AppendLine("console.log(\"count of titles > links=[\" + $(\".timeline-event-bubble-title > a:nth-child(1)\").length + \"].\");");
            resultsBldr.AppendLine("\t$(\".timeline-event-bubble-title > a:nth-child(1)\").attr(\"target\",\"_blank\");");
            resultsBldr.AppendLine("\tconsole.log(\"timeline_data variable...\");");
            resultsBldr.AppendLine("\tconsole.log(timeline_data);");
            resultsBldr.AppendLine("\t$(\"span#tl_ver\").text('[' + Timeline.writeVersion('tl_ver') + ']');");

            DateTime earliest_date = earliest_time_by_decade[decadesDistinct[0]];
            SimileDate initialDate = new SimileDate(earliest_date);

            //SimileDate initialDate = getEarliestDateByDecade(ref dateList, mymin, Tracer);
            Tracer.Add_Trace("timeline", "initial date=" + initialDate.monthnum + "-" + initialDate.daynum + "-" + initialDate.yearnum);
            //resultsBldr.AppendLine("javascript:centerSimileAjax('" + initialDate.monthnum + "," + initialDate.daynum + "," + mymin + "');");

            resultsBldr.AppendLine("$(\".sbkPrsw_ResultsPanel\").css('width','95%');");
            //resultsBldr.AppendLine("document.getElementById(\"timeline-band-0\").removeEventListener(\"DOMMouseScroll\",arguments.callee,false);");
            resultsBldr.AppendLine("console.log(\"displaying timrline-band-0 object.\");");
            resultsBldr.AppendLine("var joe=document.getElementById(\"timeline-band-0\");");
            resultsBldr.AppendLine("console.log(joe);");
            resultsBldr.AppendLine("console.log($._data( $('#timeline-band-0')[0], 'events' ));");

            resultsBldr.AppendLine("adjustSliderControls('" + initialDate.monthnum + "," + initialDate.daynum + "," + initialDate.yearnum + "');");

            resultsBldr.AppendLine("}); <!-- end of doc ready -->");

            resultsBldr.AppendLine("</script>");

            if (hasMissingDates && count_missing_date > 0)
            {
                resultsBldr.AppendLine("<div id=\"exclusiondiv\"><p>Note: " + count_missing_date + " items were excluded from the timeilne because they were missing dates. <a href=\"/all/brief?o=10\">Click here</a> to see them included in the brief results view.");

                // ability to access query string from request url?
                if (debug) resultsBldr.AppendLine("<span style=\"color:red;\">[PagedResults count=" + pagedresults_itemcount + ", item count=" + count_total + "].</span>");
                
                resultsBldr.AppendLine("</p>");
                resultsBldr.AppendLine("</div> <!-- end of exclusionsdiv -->");
            }

            if (debug) logme("Total item count=[" + count_total + "].");
            if (debug) logme("Total missing date count=[" + count_missing_date + "].");

            if (debug) logme("Adding temp debug data...");

            resultsBldr.AppendLine("<!-- ");
            resultsBldr.AppendLine("<br/><br/><br/><p>temp: <a target=\"_blank\" href=\"http://" + getMyIP() + "/temp/" + tlsn + "-" + unixTimestamp + ".js\">data js file</a></p>");
            resultsBldr.AppendLine("<p>temp: id=[" + tlsn + "-" + unixTimestamp + "].</p>");
            resultsBldr.AppendLine("<p>temp: User IP (?): " + getUserIP() + "</p>");

            resultsBldr.AppendLine("<p>temp: RequestSpecificValues.Current_Mode.Result_Display_Type=[" + RequestSpecificValues.Current_Mode.Result_Display_Type + "].</p>");
            resultsBldr.AppendLine("<p>temp: PagedResults count=[" + PagedResults.Count + "].</p>");
            resultsBldr.AppendLine("<p>temp: ResultsStats.Total_Items=[" + ResultsStats.Total_Items + "].");
            resultsBldr.AppendLine("<p>temp: ResultsStats.Total_Titles=[" + ResultsStats.Total_Titles + "].");
            resultsBldr.AppendLine("<p>temp: Results_Per_Page=[" + Results_Per_Page + "].");
            resultsBldr.AppendLine("<p>temp: total_results=[" + Total_Results + "].");
            resultsBldr.AppendLine("-->");

            resultsBldr.AppendLine("</div> <!-- end of doc3 -->");

            // Add this to the html table
            mainLiteral = new Literal { Text = resultsBldr.ToString() };
            MainPlaceHolder.Controls.Add(mainLiteral);

            logme("Done with Add_HTML...");
        }

        public static void addToDataJS(StringBuilder datajs,int yearnum,int monthnum,int daynum,string title,string myAbstract,string bibid,string vid,String path)
        {
            String title_final = title.Replace("'", "&apos;");

            if (debug) logme("addToDataJS: " + bibid + "_" + vid);

            if (debug) logme("addToDataJS: datajs length before=" + datajs.Length);

            datajs.Append("{");
            datajs.Append("'start': '" + yearnum + "-" + monthnum.ToString("D2") + "-" + daynum.ToString("D2") + "',");
            datajs.Append("'durationEvent':false,");
            //datajs += "'end': '" + yearnum + "-" + monthnum.ToString("D2") + "-" + daynum.ToString("D2") + "',";

            if (title_final.Length>20)
            {
                title_final = title_final.Substring(0, 20) + "...";
            }

            datajs.Append("'title': '" + title_final + "',");
            datajs.Append("'description': '" + myAbstract.Replace("'", "&apos;") + "',");
            datajs.Append("'image': '" + path + "',");
            //datajs += "'link': '/" + titleResult.BibID + "/" + itemResult.VID + "',";
            datajs.Append("'link': '/" + bibid + "/" + vid + "',");
            // earlier had isDuration
            datajs.Append("'durationEvent' : false,");
            datajs.Append("'icon' : \"http://" + getMyIP() + "/plugins/Timeline/images/black-circle.png\",");
            datajs.Append("'color' : 'red',");
            datajs.Append("'textColor' : 'green'},\r\n");

            if (debug) logme("dataToDataJS: datajs length after=" + datajs.Length);
        }

        public static System.String getMetadata(iSearch_Title_Result titleResult, Search_Results_Statistics ResultsStats, string fieldname)
        {
            int i = 0;
            string msg = "";

            msg = "getMetadata for [" + fieldname + "].\r\n";
            if (debug) logme(msg);

            for (i = 0; i < titleResult.Metadata_Display_Values.Length; i++)
            {
                if (ResultsStats.Metadata_Labels[i].Equals(fieldname))
                {
                    msg = "getMetadata: Found [" + fieldname + "] at [" + i + "], returning [" + titleResult.Metadata_Display_Values[i].ToString().Trim() + "].\r\n";
                    if (debug) logme(msg);

                    return titleResult.Metadata_Display_Values[i].ToString().Trim();
                }
                else
                {
                    //msg = "\tOther found at " + i + ". [" + ResultsStats.Metadata_Labels[i] + "]=[" + titleResult.Metadata_Display_Values[i].ToString().Trim() + "].\r\n";
                    //if (debug) logme(msg);
                }
            }

            msg = "getMetadata: Done, fell through, couldn't find [" + fieldname + "].\r\n";
            if (debug) logme(msg);

            return fieldname + " is N/A";
        }
        
        public static DateTime getDateFromZero(double dszero)
        {
            if (debug) logme("getDateFromZero: entered [" + dszero + "].");

            DateTime mydate;

            try
            {
                mydate = new DateTime(1, 1, 1);
                mydate = mydate.AddDays(dszero);
            }
            catch (Exception e)
            {
                logme("getDateFromZero: exception trying to add days [" + e.Message + "].");
                return new DateTime(1, 1, 1);
            }

            if (debug) logme("getDateFromZero: Date from [" + dszero + "] is [" + mydate.ToString("yyyy-MM-dd") + "].");
            if (debug) logme("getDateFromZero: Month=[" + mydate.Month + "].");
            if (debug) logme("getDateFromZero: day=[" + mydate.Day + "].");
            if (debug) logme("getDateFromZero: Year=[" + mydate.Year + "].");

            return mydate;
        }

        public static void logme(string msg)
        {
            File.AppendAllText(path_log, DateTime.Now.ToUniversalTime() + " (" + Dns.GetHostName() + "): " + msg + "\r\n\r\n");
        }

        public static string getMyIP()
        {
            string myIP;
            Uri MyUrl = HttpContext.Current.Request.Url;
            
            myIP = MyUrl.ToString().Substring(MyUrl.ToString().IndexOf("portal=") + 7);

            if (debug) logme("getMyIP: returning [" + myIP + "].");

            return myIP;
        }

        public static string getUserIP()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;
            string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string[] addresses = ipAddress.Split(',');

                if (addresses.Length != 0)
                {
                    if (debug) logme("getUserIP: returning [" + addresses[0] + "].");
                    return addresses[0];
                }
            }

            if (debug) logme("getUserIP: returning [" + context.Request.ServerVariables["REMOTE_ADDR"] + "].");

            return context.Request.ServerVariables["REMOTE_ADDR"];
        }

        public static string getIPv4FromIPv6(String ip)
        {
            return "none";
        }

        public static string stripHTMLtags(String data)
        {
            return Regex.Replace(data, @"<[^>]+>|&nbsp;", "").Trim();
        }
        
        internal static SimileDate getSimileDateFrom(String SortDateString)
        {
            DateTime dateFromZero,convertedDate;
            String mydate,myyear,mymonth,myday;
            int yearnum, monthnum, daynum;
            SimileDate sd = new SimileDate();

            try
            {
                dateFromZero = getDateFromZero(Double.Parse(SortDateString));
                //mydate = dateFromZero.Year + "-" + dateFromZero.Month + "-" + dateFromZero.Day;
            }
            catch (Exception e)
            {
                dateFromZero = getDateFromZero(Double.Parse("0"));
            }

            mydate = dateFromZero.ToString("yyyy-MM-dd");

            try
            {
                convertedDate = DateTime.Parse(mydate);
            }
            catch (Exception e)
            {
                //if (debug) logme(packageid + ": exception trying to parse date [" + mydate + "].");
                convertedDate = DateTime.Parse("0001-01-01");
            }

            try
            {
                myyear = mydate.Substring(0, 4);
                mymonth = mydate.Substring(5, 2);
                myday = mydate.Substring(8, 2);
            }
            catch (Exception e)
            {
                //if (debug) logme("Exception trying to get date elements for [" + packageid + "].\r\n");

                myyear = "0001";
                mymonth = "01";
                myday = "01";
            }

            try
            {
                yearnum = int.Parse(myyear);
                monthnum = int.Parse(mymonth);
                daynum = int.Parse(myday);
            }
            catch (Exception e)
            {
                if (debug) logme("Exception trying to parse integers from date element strings.\r\n");

                yearnum = 1;
                monthnum = 1;
                daynum = 1;
            }

            sd.yearnum = yearnum;
            sd.monthnum = monthnum;
            sd.daynum = daynum;

            return sd;
        }
    }
}