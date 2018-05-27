
        var oldFillInfoBubble = Timeline.DefaultEventSource.Event.prototype.fillInfoBubble;

        console.log(Timeline.DefaultEventSource.Event.prototype);

        console.log("getEventID=[" + this._eventID + "].");

        Timeline.DefaultEventSource.Event.prototype.fillInfoBubble = function(elmt, theme, labeller) 
        {
            oldFillInfoBubble.call(this, elmt, theme, labeller);

            var eventObject = this;

            console.log("Timeline.DefaultEventSource.Event.prototype.fillInfoBubble. the this object");
            console.log(this);

            console.log("elmt object");
            console.log(elmt);

            elmt.childNodes[1].firstChild.target="_blank";

/*
            var div = document.createElement("div");

            div.innerHTML = "<p>Additional...<p>";

            div.onhover = function() 
            {
                console.log("the item is hovered..");
            }

            elmt.appendChild(div);
*/
        };

        $(document).ready(function()
        {
            console.log("simile widgets timeline customization ready...");

            var bandInfos;

            // #controls > table > tbody > tr:nth-child(2) > td:nth-child(1)

            //$("#controls > table > tbody > tr:nth-child(2) > td:nth-child(1)").insert("<td>joe</td>").css("background-color","pink");

            // #controls > table > tbody > tr:nth-child(2) > td:nth-child(1)
            $("<td>joe</td>").insertAfter( $("#controls > table > tbody > tr:nth-child(2) > td:nth-child(1)") );

            // 2018-02-02 - Group requested that the controls are open by default (a switch)
            //toggleControls();

            var p=$("div#main-content");
            var position=p.position();

            $("div#main-content").scrollTop();

           	console.log("top of main content=[" + position.top + "].");

            console.log("document.documentElement.clientHeight=[" + document.documentElement.clientHeight + "].");
            console.log("window.innerHeight=[" + window.innerHeight + "].");
            console.log("jQuery(window).height()=[" + jQuery(window).height() + "].");

            var myheight=window.innerHeight - 200;
            console.log("myheight=[" + myheight + "].");
            $("#bd").height(myheight);
            $("#bd").css("overflow","scroll").css("overflow-x","hidden");

            // #sbkPrsw_ButtonsTable > tbody:nth-child(1) > tr:nth-child(1) > td:nth-child(4)

            //$("#sbkPrsw_ButtonsTable > tbody:nth-child(1) > tr:nth-child(1) > td:nth-child(4)").append("<img id=\"tlloadinggif\" src=\"/plugins/timeline/images/SkinnySeveralAsianlion.gif\"/>");

            

            console.log("end of doc ready.");
        });

        function noEvent()
        {
            console.log("There is no event...");
        }

        function toggleControls(e)
        {
            e = e || window.event;

            // To prevent original Similie ajax code from closing the controls when the return
            // is pressed in the filter/highlight input

            if (e.explicitOriginalTarget.className=="controlinput")
            {
                console.log("was controlinput.");
                return;
            }
            else
            {
                console.log("was not controlinput.");
            }

        	if ($("div#controls").is(":visible"))
        	{
                console.log("toggleControls: hiding...");
        		$("div#controls").hide();
        		$("#buttonControls").html("Show Controls");
                $("#buttonControls").css("margin-bottom","10px");
        	}
        	else
        	{
                console.log("toggleControls: showing...");
        		$("div#controls").show();
        		$("#buttonControls").html("Hide Controls");
                $("#buttonControls").css("margin-bottom","0px");
        	}
        }

        function adjustSliderControls(initialDate)
        {
            console.log("adjustSliderControls...");

            console.log("initialDate=[" + initialDate + "].");
            
            //$("div.yui-t7").prepend("<div id='tlb0lscroll'>&#x25c0;</div><div id='tlb0rscroll'>&#x25b6;</div><div id='tlb1lscroll'>&#x25c0;</div><div id='tlb1rscroll'>&#x25b6;</div><div id='tlb2lscroll'>&#x25c0;</div><div id='tlb2rscroll'>&#x25b6;</div>");

            $("div#controls").append("<div id='leftbandcontrols'>");
            
            $("div#leftbandcontrols").append("<button id='band0leftscroll' title='scroll decade band back in time' class='bandcontrolbuttons' type='button'>Decades &#x25c0;</button>");
            $("div#leftbandcontrols").append("<button id='band1leftscroll' title='scroll year band back in time' class='bandcontrolbuttons' type='button'>Years &#x25c0;</button>");
            $("div#leftbandcontrols").append("<button id='band2leftscroll' title='scroll month band back in time' class='bandcontrolbuttons' type='button'>Months &#x25c0;</button>");
            $("div#leftbandcontrols").append("<button id='band3leftscroll' title='scroll day band back in time' class='bandcontrolbuttons' type='button'>Days &#x25c0;</button>");
            
            $("</div>");

            $("div#controls").append("<div id='rightbandcontrols'>");

            $("div#rightbandcontrols").append("<button id='band0rightscroll' title='scroll decade band forward in time' class='bandcontrolbuttons' type='button'>&#x25b6;</button>");
            $("div#rightbandcontrols").append("<button id='band1rightscroll' title='scroll year band forward in time' class='bandcontrolbuttons' type='button'>&#x25b6;</button>");
            $("div#rightbandcontrols").append("<button id='band2rightscroll' title='scroll month band forward in time' class='bandcontrolbuttons' type='button'>&#x25b6;</button>");
            $("div#rightbandcontrols").append("<button id='band3rightscroll' title='scroll day band forward in time' class='bandcontrolbuttons' type='button'>&#x25b6;</button>");

            $("<div#controls>").append("</div>");

            $("<div#controls>").append("</div>");


            $("button#band0rightscroll").on("click",function()
            {
            	// was 50
                console.log("band#band0rightscroll was clicked...");
                scrollBand(0,-50);
            });

            $("button#band0leftscroll").on("click",function()
            {
            	// was -50
                console.log("button#band0leftscroll was clicked...");
                scrollBand(0,50);
            });


            $("button#band1rightscroll").on("click",function()
            {
            	// was 50
                console.log("button#band1rightscoll was clicked...");
                scrollBand(1,-50);
            });

            $("button#band1leftscroll").on("click",function()
            {
            	// was -50
                console.log("button#band1leftscroll was clicked...");
                scrollBand(1,50);
            });


            $("button#band2rightscroll").on("click",function()
            {
            	// was 50
                console.log("button#band2rightscroll was clicked...");
                scrollBand(2,-100);
            });

            $("button#band2leftscroll").on("click",function()
            {
            	// was =50
                console.log("button#band2leftscroll was clicked...");
                scrollBand(2,100);
            });


            $("button#band3rightscroll").on("click",function()
            {
            	// was 150
                console.log("button#band3rightscroll was clicked...");
                scrollBand(3,-150);
            });

            $("button#band3leftscroll").on("click",function()
            {
            	// was -=150
                console.log("button#band3leftscroll was clicked...");
                scrollBand(3,150);
            });

            $("div#mydecade").css("display","block").css("position","relative").css("left","50px").css("top","-37px").css("z-index","999").css("font-weight","bolder");
        
            /*
            for (i=1; i<=3; i++)
            {
                var elem = $("div#timeline-band-" + i);

                console.log(i + ". original top=[" + $("div#timeline-band-" + i).css("top") + "].");
                var newtop=Math.abs($("div#timeline-band-" + i).css("top").replace("px","")) + Math.abs(i*15);
                console.log(i + ". newtop=[" + newtop + "].");

                var height=$("div#timeline-band-" + i).css("height");
                console.log("height=[" + height + "].");

                var left=$("div#timeline-band-" + i).css("left");
                console.log("left=[" + left + "].");

                var width=$("div#timeline-band-" + i).css("width");
                console.log("width=[" + width + "].");

                $("div#timeline-band-" + i).removeProp("style");
                $("div#timeline-band-" + i).removeAttr("style");
                $("div#timeline-band-" + i).css("top",newtop + "px").css("height",height).css("left",left).css("width",width);

                console.log(i + ". final top=[" + $("div#timeline-band-" + i).css("top") + "].");
                console.log("");
            }
            */

            centerSimileAjax(initialDate);
            centerSimileAjax(initialDate);

            $("#tlloadingmsg").hide();

            console.log("end of adjustSliderControls...");
        }

        function scrollBand(bandnum,move_amt)
        {
        	console.log("scrollBand called, bandnum=[" + bandnum + "].");
        
			tl.getBand(bandnum)._moveEther(move_amt);
        }

        function selectedgotolink(selected)
        {
        	console.log("gotolinksselect: [" + selected.value + "].");
            
            // javascript:centerSimileAjax('" + firstDate.monthnum + "," + firstDate.daynum + "," + firstDate.yearnum + "')\">" + theDecade + "</a>&nbsp;&nbsp;&nbsp;");

            var parts=selected.value.split("-");
            var year=parts[0];
            var month=parts[2];
            var day=parts[1];
            console.log(year + "-" + month + "-" + day);

            centerSimileAjax("'" + month + "," + day + "," + year + "'");
        }