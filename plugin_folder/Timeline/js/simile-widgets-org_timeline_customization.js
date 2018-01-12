
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

            // #controls > table > tbody > tr:nth-child(2) > td:nth-child(1)

            //$("#controls > table > tbody > tr:nth-child(2) > td:nth-child(1)").insert("<td>joe</td>").css("background-color","pink");

            // #controls > table > tbody > tr:nth-child(2) > td:nth-child(1)
            $("<td>joe</td>").insertAfter( $("#controls > table > tbody > tr:nth-child(2) > td:nth-child(1)") );

            toggleControls();

            
             
        });

        function noEvent()
        {
            console.log("There is no event...");
        }

        function toggleControls()
        {
        	if ($("div#controls").is(":visible"))
        	{
        		$("div#controls").hide();
        		$("#buttonControls").html("Show Controls");
        	}
        	else
        	{
        		$("div#controls").show();
        		$("#buttonControls").html("Hide Controls");
        	}
        }