﻿using System.Text;
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
using EngineAgnosticLayerDbAccess;
using SobekCM.Engine_Library.Database;
using System.Data;

namespace SimileTimeline
{
    public class simileDate
    {
        public int yearnum;
        public int monthnum;
        public int daynum;
    }

    public class SimileTimeline_ResultsViewer: abstract_ResultsViewer
    {
        public static Boolean debug=false;
        private string source_url;
        
        /// <summary> Constructor for a new instance of the SimilineTimeline_ResultsViewer class </summary>
        public SimileTimeline_ResultsViewer() : base()
        {
            // Do nothing, but the base class constructor does stuff

            String path_log;

            if (Dns.GetHostName() == "SOB-EXHIBIT01")
            {
                path_log = @"D:\rbernard\Dropbox\SimileTimeline-ResultsViewer.log.txt";
            }
            else
            {
                //path_log = @"C:\Users\rbernard\Dropbox\SimileTimeline-ResultsViewer.log.txt";
                path_log=Path.GetTempPath() + @"\SimileTimeline-ResultsViewer.log.txt";
            }

            File.Delete(path_log);

            String path_debug = @"D:\WebRoot\plugins\SimileTimeline\debug.txt";

            if (File.Exists(path_debug))
            {
                debug = true;
            }
            else
            {
                debug = false;
            }
        }
        
        /*
        public override bool Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            logme("The Write_Within_HTML_Head method within SimileTimeline_ResultsViewer was called.");
            Tracer.Add_Trace("SimileTimeline_ResultsViewer.Write_Within_HTML_Head","RRB adding to default head with this newly added function within abstract_ResultsViewer...");

            // RRB

            //Tracer.Add_Trace("Html_Mainwriter.Write_Within_HTML_Head","RRB - extra temporary for SOAS project.");

            String base_url;
            base_url= "test.richardbernardy.com";
            //base_url = "localhost:52468/";

            Output.WriteLine("<link rel=\"stylesheet\" href=\"http://" + base_url + "/plugins/Timeline/css/SimileTimeline.css\" type=\"text/css\">");

            Output.WriteLine("<link rel=\"stylesheet\" href=\"http://yui.yahooapis.com/2.7.0/build/reset-fonts-grids/reset-fonts-grids.css\" type = \"text/css\">");
            Output.WriteLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"http://yui.yahooapis.com/2.7.0/build/base/base-min.css\">");
            Output.WriteLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"http://" + base_url + "/plugins/Timeline/css/simile-widgets-org_timeline_examples_styles.css\">");

            Output.WriteLine("<script type=\"text/javascript\">");
            Output.WriteLine("Timeline_ajax_url='http://" + base_url + "/plugins/Timeline/js/timeline_2.3.0/timeline_ajax/simile-ajax-api.js';");
            Output.WriteLine("Timeline_urlPrefix='http://" + base_url + "/plugins/Timeline/js/timeline_2.3.0/timeline_js/';");
            Output.WriteLine("Timeline_parameters='bundle=true';");
            Output.WriteLine("</script>");

            Output.WriteLine("<script src=\"http://" + base_url + "/plugins/Timeline/js/timeline_2.3.0/timeline_js/timeline-api.js?bundle=true\" type=\"text/javascript\"></script>");

            // additional controls
            Output.WriteLine("<script src=\"http://" + base_url + "/plugins/Timeline/js/simile-widgets-org_timeline_examples.js\" type=\"text/javascript\"></script>");

            Output.WriteLine("<script src=\"http://" + base_url + "/plugins/Timeline/js/simile-widgets-org_timeline_customization.js\" type=\"text/javascript\"></script>");

            //Tracer.Add_Trace("Html_Mainwriter.Write_Within_HTML_Head","RRB - end of temporary for SOAS project.");
            Tracer.Add_Trace("Html_Mainwriter.Write_Within_HTML_Head", "end of write_within_html_head for timeline.");

            // end rrb
            
            Output.WriteLine("<!-- RRB writing within head -->");

            return false;
        }
        */

        public static int RoundUp(int value)
        {
            return 10 * ((value + 9) / 10);
        }

        /// <summary> Adds the controls for this result viewer to the place holder on the main form </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the the bulk of the result viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Sorted tree with the results in hierarchical structure with volumes and issues under the titles and sorted by serial hierarchy </returns>
        public override void Add_HTML(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            logme("Add_HTML is called...");

            DataSet tempSet = null;
            DataTable metadataTable;
            simileDate sd;

            string dir_resource = null, mydate, mymonth, myday, myyear, path;
            string msg = null, packageid = null, myAbstract = "", mySubjects="",bibid,vid;
            string SortDateString = "";
            List<int> yearsRepresented = new List<int>();
            List<int> yearsRepresentedDistinct = new List<int>();
            List<int> decades = new List<int>();
            List<int> decadesDistinct = new List<int>();
            int count_missing_date = 0, pagedresults_itemcount=0,titleresult_itemcount=0;
            int count_total = 0;

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
            
            string datajs = null;

            if (debug) logme("PagedResults count=[" + PagedResults.Count + "].");

            datajs+="var timeline_data = {  // save as a global variable";
            datajs+="'dateTimeFormat': 'iso8601',";
            datajs+="'wikiURL': \"http://simile.mit.edu/shelf/\",";
            datajs+="'wikiSection': \"Simile Cubism Timeline\",";
            datajs += "\r\n";
            datajs += "'events' : [";

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

            //resultsBldr.AppendLine("<!-- base_url=[" + base_url + "].-->");
            //resultsBldr.AppendLine("<!-- RequestSpecificValues.Current_Mode.Sort=[" + RequestSpecificValues.Current_Mode.Sort + "].-->");

            //Add the necessary JavaScript, CSS files
            resultsBldr.AppendLine("  <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Sobekcm_Thumb_Results_Js + "\"></script>");

            //My_Write_Within_HTML_Head(ref resultsBldr);

            // Start this table
            //resultsBldr.AppendLine("<div style=\"width:80%;height:800px;margin-left:auto;margin-right:auto\">");
            //resultsBldr.AppendLine("<h2>Timeline View</h2>");

            // Originally was iSearch_Title_Result titleResult in PagedResults
            // iSearch_Item_Result doesn't have bibid!

            iSearch_Item_Result itemResult;

            pagedresults_itemcount=PagedResults.Count;

            foreach (iSearch_Title_Result titleResult in PagedResults)
            {
                titleresult_itemcount = titleResult.Item_Count;
                
                bool multiple_title = titleResult.Item_Count > 1;
                resultsBldr.AppendLine("<!-- titleResult.Item_Count=[" + titleResult.Item_Count + "].-->");

                bibid = titleResult.BibID;
                vid = titleResult.Get_Item(0).VID;

                /*
                for (int j = 0; j < titleResult.Item_Count; j++)
                {
                */

                    //ResultsStats.Metadata_Labels.Count
                    // Always get the first item for things like the main link and thumbnail
                    //iSearch_Item_Result firstItemResult = titleResult.Get_Item(0);
                    //itemResult = titleResult.Get_Item(j);
                    
                    // Determine the internal link to the first (possibly only) item
                    string internal_link = base_url + bibid + "/" + vid + textRedirectStem;
                    packageid = bibid + "/" + vid;

                    //resultsBldr.AppendLine("<!-- internal_link=[" + internal_link + "].-->");

                    // For browses, just point to the title
                    if ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) && (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Browse_Info))
                        internal_link = base_url + titleResult.BibID + textRedirectStem;

                    resultsBldr.AppendLine("<!-- internal_link=[" + internal_link + "].-->");
                    //resultsBldr.AppendLine("<!-- snippet=[" + titleResult.Snippet + "].-->");

                    dir_resource = SobekFileSystem.Resource_Network_Uri(bibid,vid);
                    //resultsBldr.AppendLine("<!-- dir_resource=[" + dir_resource + "].-->");

                    source_url = UI_ApplicationCache_Gateway.Settings.Servers.Image_URL + SobekFileSystem.AssociFilePath(bibid,vid).Replace("\\", "/");
                    //resultsBldr.AppendLine("<!-- source_url=[" + source_url + "].-->");

                    string title;

                    /*
                    if (multiple_title)
                    {
                        //resultsBldr.AppendLine("<!-- is multiple_title -->");

                        // Determine term to use
                        string multi_term = "volume";
                        if (titleResult.MaterialType.ToUpper() == "NEWSPAPER")
                        {
                            multi_term = titleResult.Item_Count > 1 ? "issues" : "issue";
                        }
                        else
                        {
                            if (titleResult.Item_Count > 1)
                                multi_term = "volumes";
                        }

                        if ((showDate))
                        {
                            if (firstItemResult.PubDate.Length > 0)
                            {
                                title = "[" + firstItemResult.PubDate + "] " + titleResult.GroupTitle;
                            }
                            else
                            {
                                title = titleResult.GroupTitle;
                            }
                        }
                        else
                        {
                            title = titleResult.GroupTitle + "<br />( " + titleResult.Item_Count + " " + multi_term + " )";
                        }
                    }
                    else
                    {
                        //resultsBldr.AppendLine("<!-- is NOT multiple_title -->");

                        if (showDate)
                        {
                            //resultsBldr.AppendLine("<!-- is showDate -->");

                            if (firstItemResult.PubDate.Length > 0)
                            {
                                title = "[" + firstItemResult.PubDate + "] " + firstItemResult.Title;
                            }
                            else
                            {
                                title = firstItemResult.Title;
                            }
                        }
                        else
                        {
                            //resultsBldr.AppendLine("<!-- is NOT showDate -->");

                            title = firstItemResult.Title;
                        }
                    }

                */

                    // metadata Title

                    title = titleResult.GroupTitle;
                    title = Regex.Replace(title, @"<[^>]+>|&nbsp;", "").Trim();

                    //resultsBldr.AppendLine("<!-- metadata display values length=[" + titleResult.Metadata_Display_Values.Length + "]. -->");
                    //resultsBldr.AppendLine("<!-- Link=[" + firstItemResult.Link + "].-->");

                    // Add the title
                    // path = source_url + "/" + firstItemResult.MainThumbnail;
                    // that doesn't work when granting access through a router / local lan
                    if (debug) logme("source_url=[" + source_url + "].");

                    path = "http://" + getMyIP() + "/" + source_url.Substring(source_url.IndexOf("content")) + "/" + titleResult.GroupThumbnail;
                    if (debug) logme("path to thumbnail=[" + path + "].");

                    resultsBldr.AppendLine("<!-- path=[" + path + "]. -->");

                    //resultsBldr.AppendLine("<a href=\"" + internal_link + "\">" + title + " <img src=\"" + path + "\"/></a><br />");
                    //resultsBldr.Append("<ul>");

                    //for (i = 0; i < titleResult.Metadata_Display_Values.Length; i++)
                    //{
                    //    if (titleResult.Metadata_Display_Values[i].ToString().Trim().Length > 0)
                    //    {
                    //resultsBldr.AppendLine("<li>" + ResultsStats.Metadata_Labels[i] + ": " + titleResult.Metadata_Display_Values[i].ToString() + "</li>");
                    //    }
                    //}

                    //resultsBldr.Append("</ul>");

                    //mydate=getMetadata(titleResult,ResultsStats,"Publication_Date").Trim().Replace("|","").Trim();
                    
                    SortDateString = getMetadata(titleResult, ResultsStats, "SortDate").Trim();
                    if (debug) logme(packageid + ": SortDateString=[" + SortDateString + "].");

                    if (SortDateString == "-1")
                    {
                        if (debug) logme(packageid + ": returned sort date string was -1.");
                        SortDateString = "0";
                    }
                    else
                    {
                        if (debug) logme(packageid + ": returned sort date was [" + SortDateString + "].");
                    }

                    RequestSpecificValues.Tracer.Add_Trace("SimileTimeline_ResultsViewer", "The returned SortDateString is [" + SortDateString + "].");

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

                    if (debug) logme(packageid + ": mydate=[" + mydate + "], title=[" + title + "].");

                    //resultsBldr.Append("<!-- firstItemResult.PubDate=[" + firstItemResult.PubDate + "]. -->");
                    //resultsBldr.Append("<!-- mydate=[" + mydate + "]. -->");

                    try
                    {
                        convertedDate = DateTime.Parse(mydate);
                    }
                    catch (Exception e)
                    {
                        if (debug) logme(packageid + ": exception trying to parse date [" + mydate + "].");
                        convertedDate = DateTime.Parse("0001-01-01");
                    }

                    if (debug) logme(packageid + ": convertedDate=[" + convertedDate.ToString("yyyy-MM-dd") + "].");

                    try
                    {
                        myyear = mydate.Substring(0, 4);
                        mymonth = mydate.Substring(5, 2);
                        myday = mydate.Substring(8, 2);
                    }
                    catch (Exception e)
                    {
                        if (debug) logme("Exception trying to get date elements for [" + packageid + "].\r\n");

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

                    if (debug) logme(packageid + ": myyear=[" + myyear + "], mymonth=[" + mymonth + "], myday=[" + myday + "].");
                    if (debug) logme(packageid + ": yearnum=[" + yearnum + "], monthnum=[" + monthnum + "], daynum=[" + daynum + "].");

                    if (yearnum == 1)
                    {
                        hasMissingDates = true;
                    }
                    else
                    {
                        yearsRepresented.Add(yearnum);
                    }

                    // metadata **********************************************************************************************************************
                    // metadata Abstract
                    
                    myAbstract = getMetadata(titleResult, ResultsStats, "Abstract").Trim().Replace("|", "").Trim();

                    if (myAbstract.Length == 0)
                    {
                        myAbstract = "No abstract is available. ";
                    }
                    else
                    {
                        myAbstract = myAbstract.Replace("  " + "\n", " ");

                        if (myAbstract.Length > 100)
                        {
                            myAbstract = myAbstract.Substring(0, 100) + " ...";
                        }
                    }

                    myAbstract = Regex.Replace(myAbstract, @"<[^>]+>|&nbsp;", "").Trim();
                    //myAbstract += ". temp:SortDateString=(" + SortDateString + ").";

                    // *******************************************************************************************************************************
                    // metadata Subjects.Display

                    mySubjects = getMetadata(titleResult, ResultsStats, "Subjects.Display").Trim();

                    if (mySubjects.Length > 0)
                    {
                        mySubjects = Regex.Replace(mySubjects, @"<[^>]+>|&nbsp;", "").Trim();

                        myAbstract += " Subjects: " + mySubjects + ".";
                    }

                    // *******************************************************************************************************************************

                    if (yearnum == 1 && monthnum == 1 && daynum == 1)
                    {
                        count_missing_date++;
                        if (debug) logme(packageid + " was missing a date, skipping. count_missing_date=" + count_missing_date + ".");
                    }
                    else
                    {

                        if (debug) logme(packageid + " being added as an event.");

                        /*
                        datajs += "{";
                        datajs += "'start': '" + yearnum + "',";
                        datajs += "'end': '" + yearnum + "',";
                        datajs += "'title': '" + title.Replace("'", "&apos;") + "',";
                        datajs += "'description': '" + myAbstract.Replace("'", "&apos;") + "',";
                        datajs += "'image': '" + path + "',";
                        datajs += "'link': '/" + titleResult.BibID + "/" + itemResult.VID + "',";
                        // earlier had isDuration
                        datajs += "'durationEvent' : false,";
                        datajs += "'icon' : \"http://" + getMyIP() + "/plugins/Timeline/images/dark-red-circle.png\",";
                        datajs += "'color' : 'red',";
                        datajs += "'textColor' : 'green'},\r\n";
                        */

                        count_total++;
                        addToDataJS(ref datajs, yearnum, title, myAbstract, bibid, vid, path);
                        if (debug) logme("Added event, count_total=" + count_total);
                    }

                /*
                }
                */
                // end of item loop

                // process items

                if (debug) logme("There are [" + titleResult.Item_Count + "] items for [" + bibid + "].");

                if (titleResult.Item_Count > 1)
                {
                    for (int j = 1; j < titleResult.Item_Count; j++)
                    {
                        itemResult = titleResult.Get_Item(j);
                        vid = itemResult.VID;
                        title = itemResult.Title;
                        packageid = bibid + "_" + vid;

                        if (debug) logme("item #" + j + ". " + packageid + "{{{" + title + "}}}.");

                        // SobekCM_Metadata_By_Bib_Vid

                        EalDbParameter[] paramList = new EalDbParameter[25];

                        paramList[0] = new EalDbParameter("@aggregationcode", null);
                        paramList[1] = new EalDbParameter("@bibid1", bibid);
                        paramList[2] = new EalDbParameter("@vid1", vid);
                        paramList[3] = new EalDbParameter("@bibid2", null);
                        paramList[4] = new EalDbParameter("@vid2", null);
                        paramList[5] = new EalDbParameter("@bibid3", null);
                        paramList[6] = new EalDbParameter("@vid3", null);
                        paramList[7] = new EalDbParameter("@bibid4", null);
                        paramList[8] = new EalDbParameter("@vid4", null);
                        paramList[9] = new EalDbParameter("@bibid5", null);
                        paramList[10] = new EalDbParameter("@vid5", null);
                        paramList[11] = new EalDbParameter("@bibid6", null);
                        paramList[12] = new EalDbParameter("@vid6", null);
                        paramList[13] = new EalDbParameter("@bibid7", null);
                        paramList[14] = new EalDbParameter("@vid7", null);
                        paramList[15] = new EalDbParameter("@bibid8", null);
                        paramList[16] = new EalDbParameter("@vid8", null);
                        paramList[17] = new EalDbParameter("@bibid9", null);
                        paramList[18] = new EalDbParameter("@vid9", null);
                        paramList[19] = new EalDbParameter("@bibid10", null);
                        paramList[20] = new EalDbParameter("@vid10", null);

                        //titleResult.Metadata_Display_Values;
                        //itemResult;
                        
                        try
                        {
                            tempSet = EalDbAccess.ExecuteDataset(EalDbTypeEnum.MSSQL, Engine_Database.Connection_String, CommandType.StoredProcedure, "SobekCM_Metadata_By_Bib_Vid", paramList);

                            metadataTable = tempSet.Tables[1];
                            if (debug) logme("metadatatable (1) has " + metadataTable.Rows.Count + " rows.");

                            IEnumerable<DataRow> query =
                                from metadatarow in metadataTable.AsEnumerable()
                                select metadatarow;

                            Dictionary<String,int> itemIDtoVID = new Dictionary<String,int>();
                            
                            foreach (DataRow p in query)
                            {
                                itemIDtoVID.Add(p.Field<String>("VID"),p.Field<int>("ItemID"));
                            }

                            int myItemID = itemIDtoVID[vid];

                            if (debug) logme("tempSet has " + tempSet.Tables.Count + " tables, tempSet table 2 has " + tempSet.Tables[2].Rows.Count + " rows -->");

                            metadataTable = tempSet.Tables[2];

                            query =
                                from metadatarow in metadataTable.AsEnumerable()
                                select metadatarow;

                            //IEnumerable<DataRow> thisagg =
                            //    query.Where(p => p.Field<string>("code").Equals(code));

                            // was in thisagg
                            foreach (DataRow p in query)
                            {
                                //Response.Output.WriteLine("<counts code=\"" + p.Field<String>("code") + "\">");
                                //mask = p.Field<short>("mask");

                                if (p.Field<int>("ItemID") == myItemID)
                                {
                                    logme("Found vid [" + vid + "] = itemid=[" + myItemID + "].");

                                    myAbstract = stripHTMLtags(p.Field<String>("Abstract"));

                                    if (myAbstract.Trim().Length == 0)
                                    {
                                        myAbstract = "Abstract is N/A";
                                    }

                                    mySubjects = stripHTMLtags(p.Field<String>("Subjects.Display"));

                                    if (mySubjects.Trim().Length > 0)
                                    {
                                        myAbstract += "; Subjects: " + mySubjects;
                                    }

                                    SortDateString = stripHTMLtags(p.Field<String>("SortDate"));
                                    sd = getSimileDateFrom(SortDateString);

                                    if (debug) logme("Item #" + j + ". " + bibid + "_" + vid + ", myAbstract=[" + myAbstract + "], mySubjects=[" + mySubjects + "], SortDateString=[" + SortDateString + "], sd.yearnum=[" + sd.yearnum + "], sd.monthnum=[" + sd.monthnum + "], sd.daynum=[" + sd.daynum + "].");

                                    if (sd.yearnum == 1)
                                    {
                                        hasMissingDates = true;
                                        count_missing_date++;
                                    }
                                    else
                                    {
                                        yearsRepresented.Add(sd.yearnum);
                                        source_url = UI_ApplicationCache_Gateway.Settings.Servers.Image_URL + SobekFileSystem.AssociFilePath(bibid, vid).Replace("\\", "/");
                                        path = path = "http://" + getMyIP() + "/" + source_url.Substring(source_url.IndexOf("content")) + itemResult.MainThumbnail;

                                        count_total++;
                                        addToDataJS(ref datajs, sd.yearnum, title, myAbstract, bibid, vid, path);
                                    }

                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logme("Exception in new item processing section [" + e.Message + "].");
                        }

                        if (debug) logme("Done processing item vid=[" + vid + "].");
                    }
                }
                
                if (debug) logme("Done processing bibid=[" + bibid + "], vid=[" + vid + "].\r\n\r\n");
                if (debug) logme("Total count added=" + count_total + ", missing count date=" + count_missing_date);
            }

            if (debug) logme("main processing loop completed.");

            if (debug) logme("datajs length = " + datajs.Length + ".");

            // end of titleResults loop

            datajs = datajs.Substring(0, datajs.Length - 3);
            datajs += "]}";

            if (debug) logme("webroot=[" + HttpContext.Current.Server.MapPath("~") + "].");
            File.WriteAllText(HttpContext.Current.Server.MapPath("~") + @"\temp\" + tlsn + "-" + unixTimestamp + ".js", datajs);

            resultsBldr.AppendLine("<script src=\"" + @"/temp/" + tlsn + "-" + unixTimestamp + ".js" + "\" type=\"text/javascript\"></script>");

            if (debug) logme("count of yearsRepresented=[" + yearsRepresented.Count + "].");

            if (count_total==0)
            {
                // number.ToString("#,##0");
                resultsBldr.AppendLine("<br/><p id=\"warningzero\">Note: For the timeline, out of " + pagedresults_itemcount.ToString("#,##0") + " results in this page all were missing dates and were skipped. Proceed to the next page (if any).</p>");
                if (debug) logme("All results in this page were missing dates and were skipped, returning.");

                mainLiteral = new Literal { Text = resultsBldr.ToString() };
                MainPlaceHolder.Controls.Add(mainLiteral);

                return;
            }

            yearsRepresented.Sort();
            int mymin = yearsRepresented[i];
            int mymax = yearsRepresented[yearsRepresented.Count - 1];
            int sum = 0;

            var g = yearsRepresented.GroupBy(ig => ig);

            //resultsBldr.AppendLine("<ul>");

            int key, value;
            
            foreach (var grp in g)
            {
                key = grp.Key;
                value = grp.Count();

                //resultsBldr.AppendLine("<li>key=" + key + ", rounded=" + RoundUp(key) + ", count=" + value + "</li>");
                decades.Add(RoundUp(key));
            }

            //resultsBldr.AppendLine("</ul>");

            foreach (int myvalue in yearsRepresented)
            {
                sum += myvalue;
            }

            int myavg = sum / yearsRepresented.Count;

            if (debug) logme("min of yearsRepresented=" + mymin);
            if (debug) logme("max of yearsRepresented=" + mymax);
            if (debug) logme("average of yearsRepresented=" + myavg);

            //resultsBldr.AppendLine("<p>Range of dates = " + mymin + " - " + mymax + ", average=" + myavg + ".</p>");

            if (hasMissingDates)
            {
                //resultsBldr.AppendLine("<p>Has missing dates, those are set to January 1, 1.</p>");
            }

            // End this div
            //resultsBldr.AppendLine("</div>");
            //resultsBldr.AppendLine("<hr/><br/><br/><br/>");

            // put controls on top

            if (debug) logme("Adding controls.");

            resultsBldr.AppendLine("<button id=\"buttonControls\" class=\"btn\" onclick=\"javascript:toggleControls();\">Show Controls</button>");
            resultsBldr.AppendLine("\t\t\t <div class=\"controls\" id=\"controls\"></div>");

            resultsBldr.Append("<p>Jump: ");
            
            // <a href=\"javascript:centerSimileAjax('1,1,1')\">No date</a> ");
            
            try
            {
                decadesDistinct = decades.Distinct<int>().ToList<int>();
            }
            catch (Exception e)
            {
                if (debug) logme("exception trying to get decadesDistinct [" + e.Message + "].");
            }

            foreach (int decade in decadesDistinct)
            {
                resultsBldr.Append("<a href=\"javascript:centerSimileAjax('1 1, " + decade + "')\">" + decade + "</a> ");
            }
            
            resultsBldr.Append("</p>");

            resultsBldr.AppendLine("<div id = \"doc3\" class=\"yui-t7\">");

            // 2017-10-28 - SOAS asked that whitespace be removed, etc.

            //resultsBldr.AppendLine("\t<div id = \"hd\" role=\"banner\">");
            //resultsBldr.AppendLine("<p id=\"mytitle\">Grab timeline to browse by date</p>");
            //resultsBldr.AppendLine("\t</div>");

            resultsBldr.AppendLine("\t<div id = \"bd\" role= \"main\">");
            resultsBldr.AppendLine("\t\t<div class=\"yui-g\">");
            resultsBldr.AppendLine("\t\t\t<div id = 'tl'></div>");
            resultsBldr.AppendLine("\t\t\t</div>");
            
            resultsBldr.AppendLine("\t\t</div>");
            //resultsBldr.AppendLine("\t\t<div id = \"ft\" role=\"contentinfo\">");
            resultsBldr.AppendLine("\t\t\t<p style=\"display:none;\">Thanks to the <a href=''>Simile Timeline project</a>. Timeline version <span id='tl_ver'>");
            resultsBldr.AppendLine("\t\t\t<script>Timeline.writeVersion('tl_ver');</script></span></p>");
            //resultsBldr.AppendLine("\t\t</span></p>");
            //resultsBldr.AppendLine("\t\t</div>");
            resultsBldr.AppendLine("</div>");

            if (debug) logme("Adding onLoad function...");

            resultsBldr.AppendLine("<script type=\"text/javascript\">");
            resultsBldr.AppendLine("var tl;");
            resultsBldr.AppendLine("function onLoad() {");
            resultsBldr.AppendLine("console.log(\"onload function...\");");
            resultsBldr.AppendLine("var tl_el = document.getElementById(\"tl\");");
            resultsBldr.AppendLine("var eventSource1 = new Timeline.DefaultEventSource();");
            resultsBldr.AppendLine("var theme1 = Timeline.ClassicTheme.create();");
            resultsBldr.AppendLine("theme1.autoWidth = true; // Set the Timeline's \"width\" automatically.");
            resultsBldr.AppendLine("// Set autoWidth on the Timeline's first band's theme,");
            resultsBldr.AppendLine("// will affect all bands.");

            // 2017-10-28 - Related to a SOAS request not to include events with a missing date - resetting the start to the mymin

            //if (hasMissingDates)
            //{
            //    resultsBldr.AppendLine("theme1.timeline_start = new Date(Date.UTC(-100, 0, 0));");
            //}
            //else
            //{
                resultsBldr.AppendLine("theme1.timeline_start = new Date(Date.UTC(" + (Math.Abs(mymin) - 10) + ",0,0));");
            //}

            resultsBldr.AppendLine("theme1.timeline_stop = new Date(Date.UTC(" + (Math.Abs(mymax) + 10) + ", 0, 1));");

            resultsBldr.AppendLine("var d = Timeline.DateTime.parseGregorianDateTime(\"" + myavg + "\")");
            resultsBldr.AppendLine("console.log(\"createBandInfo...\");");
            resultsBldr.AppendLine("var bandInfos = [");
            resultsBldr.AppendLine("Timeline.createBandInfo({");
            resultsBldr.AppendLine("width: 45, // set to a minimum, autoWidth will then adjust");
            resultsBldr.AppendLine("intervalUnit: Timeline.DateTime.DECADE,");
            resultsBldr.AppendLine("intervalPixels: 200,");
            resultsBldr.AppendLine("eventSource: eventSource1,");

            // zoom
            resultsBldr.AppendLine("zoomIndex: 8,");
            resultsBldr.AppendLine("zoomSteps: new Array(");
            //resultsBldr.AppendLine("{pixelsPerInterval: 280,  unit: Timeline.DateTime.HOUR},");
            //resultsBldr.AppendLine("{ pixelsPerInterval: 140,  unit: Timeline.DateTime.HOUR},");
            //resultsBldr.AppendLine("{ pixelsPerInterval: 70,  unit: Timeline.DateTime.HOUR},");
            //resultsBldr.AppendLine("{ pixelsPerInterval: 35,  unit: Timeline.DateTime.HOUR},");
            resultsBldr.AppendLine("{ pixelsPerInterval: 400,  unit: Timeline.DateTime.DAY},");
            resultsBldr.AppendLine("{ pixelsPerInterval: 200,  unit: Timeline.DateTime.DAY},");
            resultsBldr.AppendLine("{ pixelsPerInterval: 100,  unit: Timeline.DateTime.DAY},");
            resultsBldr.AppendLine("{ pixelsPerInterval: 50,  unit: Timeline.DateTime.DAY},");
            resultsBldr.AppendLine("{ pixelsPerInterval: 400,  unit: Timeline.DateTime.MONTH},");
            resultsBldr.AppendLine("{ pixelsPerInterval: 200,  unit: Timeline.DateTime.MONTH},");
            resultsBldr.AppendLine("{ pixelsPerInterval: 100,  unit: Timeline.DateTime.MONTH}, // DEFAULT zoomIndex");
            resultsBldr.AppendLine("{ pixelsPerInterval: 400, unit: Timeline.DateTime.DECADE},");
            resultsBldr.AppendLine("{ pixelsPerInterval: 200, unit: Timeline.DateTime.DECADE},");
            resultsBldr.AppendLine("),");

            resultsBldr.AppendLine("date: d,");
            resultsBldr.AppendLine("theme: theme1,");
            resultsBldr.AppendLine("layout: 'original'  // original, overview, detailed");
            resultsBldr.AppendLine("})");
            resultsBldr.AppendLine("];");

            resultsBldr.AppendLine("console.log(\"the bandInfos.\");");
            resultsBldr.AppendLine("console.log(bandInfos);");

            // band
            // since I only have one band maybe I don't need this.
            //resultsBldr.AppendLine("bandInfos[0].syncWith = 0;");
            //resultsBldr.AppendLine("bandInfos[0].highlight = true;");

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
            resultsBldr.AppendLine("console.log(\"setup the controls.\");");
            //resultsBldr.AppendLine("var theme = Timeline.ClassicTheme.create();");
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
            resultsBldr.AppendLine("centerSimileAjax('1,1," + mymin + "');");
            resultsBldr.AppendLine("$(\".sbkPrsw_ResultsPanel\").css('width','95%');");
            resultsBldr.AppendLine("});");
            resultsBldr.AppendLine("</script>");

            if (hasMissingDates && count_missing_date > 0)
            {
                resultsBldr.AppendLine("<div id=\"exclusiondiv\"><p>Note: " + count_missing_date + " items were excluded from the timeilne  because they were missing dates. Click <a href=\"/all/brief?o=10\">here</a> to see them included in the brief results view. <span style=\"color:red;\">[PagedResults count=" + pagedresults_itemcount + ", item count=" + count_total + "].</span></p></div>");
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

            // Add this to the html table
            mainLiteral = new Literal { Text = resultsBldr.ToString() };
            MainPlaceHolder.Controls.Add(mainLiteral);

            if (debug) logme("Done with Add_HTML...");
        }

        public static void addToDataJS(ref string datajs,int yearnum,string title,string myAbstract,string bibid,string vid,String path)
        {
            if (debug) logme("addToDataJS: " + bibid + "_" + vid);

            if (debug) logme("addToDataJS: datajs length before=" + datajs.Length);

            datajs += "{";
            datajs += "'start': '" + yearnum + "',";
            datajs += "'end': '" + yearnum + "',";
            datajs += "'title': '" + title.Replace("'", "&apos;") + "',";
            datajs += "'description': '" + myAbstract.Replace("'", "&apos;") + "',";
            datajs += "'image': '" + path + "',";
            //datajs += "'link': '/" + titleResult.BibID + "/" + itemResult.VID + "',";
            datajs += "'link': '/" + bibid + "/" + vid + "',";
            // earlier had isDuration
            datajs += "'durationEvent' : false,";
            datajs += "'icon' : \"http://" + getMyIP() + "/plugins/Timeline/images/pastel-pink-circle.png\",";
            datajs += "'color' : 'red',";
            datajs += "'textColor' : 'green'},\r\n";

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

        /*
        public static void My_Write_Within_HTML_Head(ref StringBuilder resultsBldr)
        {
            String base_url = getMyIP();

            resultsBldr.AppendLine("<link rel=\"stylesheet\" href=\"" + base_url + "/plugins/Timeline/css/SimileTimeline.css\" type=\"text/css\">");

            resultsBldr.AppendLine("<link rel=\"stylesheet\" href=\"http://yui.yahooapis.com/2.7.0/build/reset-fonts-grids/reset-fonts-grids.css\" type = \"text/css\">");
            resultsBldr.AppendLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"http://yui.yahooapis.com/2.7.0/build/base/base-min.css\">");
            
            resultsBldr.AppendLine("<script type=\"text/javascript\">");
            resultsBldr.AppendLine("Timeline_ajax_url='http://" + base_url + "/plugins/Timeline/js/timeline_2.3.0/timeline_ajax/simile-ajax-api.js';");
            resultsBldr.AppendLine("Timeline_urlPrefix='http://" + base_url + "/plugins/Timeline/js/timeline_2.3.0/timeline_js/';");
            resultsBldr.AppendLine("Timeline_parameters='bundle=true';");
            resultsBldr.AppendLine("</script>");

            resultsBldr.AppendLine("<script src=\"http://" + base_url + "/plugins/Timeline/js/timeline_2.3.0/timeline_js/timeline-api.js?bundle=true\" type=\"text/javascript\"></script>");
        }
        
        */

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
            string path_log;

            if (Dns.GetHostName() == "SOB-EXHIBIT01")
            {
                path_log = @"D:\rbernard\Dropbox\SimileTimeline-ResultsViewer.log.txt";
            }
            else
            {
                path_log = @"C:\Users\rbernard\Dropbox\SimileTimeline-ResultsViewer.log.txt";
            }

            File.AppendAllText(path_log, DateTime.Now.ToUniversalTime() + " (" + Dns.GetHostName() + "): " + msg + "\r\n\r\n");
        }

        public static string getMyIP()
        {
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName()); // `Dns.Resolve()` method is deprecated.
            //IPAddress ipAddress = ipHostInfo.AddressList[0];
            //logme("getMyIP: returning [" + ipAddress.ToString() + "].");
            //return ipAddress.ToString();

            //string myIP=Engine_ApplicationCache_Gateway.Settings.Servers.SobekCM_Web_Server_IP;

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

        public static simileDate getSimileDateFrom(String SortDateString)
        {
            DateTime dateFromZero,convertedDate;
            String mydate,myyear,mymonth,myday;
            int yearnum, monthnum, daynum;
            simileDate sd = new simileDate();

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

            //if (debug) logme(packageid + ": mydate=[" + mydate + "], title=[" + title + "].");

            //resultsBldr.Append("<!-- firstItemResult.PubDate=[" + firstItemResult.PubDate + "]. -->");
            //resultsBldr.Append("<!-- mydate=[" + mydate + "]. -->");

            try
            {
                convertedDate = DateTime.Parse(mydate);
            }
            catch (Exception e)
            {
                //if (debug) logme(packageid + ": exception trying to parse date [" + mydate + "].");
                convertedDate = DateTime.Parse("0001-01-01");
            }

            //logme(packageid + ": convertedDate=[" + convertedDate.ToString("yyyy-MM-dd") + "].");

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