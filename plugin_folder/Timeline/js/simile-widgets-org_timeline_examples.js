var debug=false;

function centerSimileAjax(date) 
{
	console.log("centerSimileAjax called[" + date + "].");
	console.log("centerSimileAjax parsed date=[" + SimileAjax.DateTime.parseGregorianDateTime(date) + "].");
    tl.getBand(3).setCenterVisibleDate(SimileAjax.DateTime.parseGregorianDateTime(date));
}

function setupFilterHighlightControls(div, timeline, bandIndices, theme) {
    var table = document.createElement("table");
    
    // RRB
    table.width="100%";

    var tr = table.insertRow(0);
    var td = tr.insertCell(0);
 
    td.innerHTML = "Filter (search/limit)";
    td = tr.insertCell(1);
    td.innerHTML = "Highlight";
    
    var handler = function(elmt, evt, target) 
    {
        onKeyPress(timeline, bandIndices, table);
    };
    
    tr = table.insertRow(1);
    tr.style.verticalAlign = "top";
    
    td = tr.insertCell(0);
    
    var input = document.createElement("input");
    input.type = "text";

    var att = document.createAttribute("class");       // Create a "class" attribute
    att.value = "controlinput";                           // Set the value of the class attribute
    input.setAttributeNode(att);

    att = document.createAttribute("id");
    att.value = "filtercontrol";
    input.setAttributeNode(att);     

    SimileAjax.DOM.registerEvent(input, "keypress", handler);
    td.appendChild(input);

    td = tr.insertCell(1);
    td.innerHTML="&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
    
    // RRB
    if (debug) console.log("RRB: setupFilterHighlightControls : theme.event.highlightColors.length=[" + theme.event.highlightColors.length + "].");

    for (var i=0; i< theme.event.highlightColors.length; i++)
    {
        console.log("RRB: theme.event.highlightColors[" + i + "]=[" + theme.event.highlightColors[i] + "].");
    }

    for (var i = 0; i < theme.event.highlightColors.length-1; i++) 
    {
        // was i+1
        td = tr.insertCell(i + 2);
        
        input = document.createElement("input");
        input.type = "text";

        var att = document.createAttribute("class");       // Create a "class" attribute
        att.value = "controlinput";                           // Set the value of the class attribute
        input.setAttributeNode(att);
        // was i
        input.style.background = theme.event.highlightColors[i+1];

        SimileAjax.DOM.registerEvent(input, "keypress", handler);
    
        td.appendChild(input);
        
        var divColor = document.createElement("div");
        divColor.style.height = "0.5em";
        divColor.style.background = theme.event.highlightColors[i+1];
        
        console.log("setup: " + i + "=[" + theme.event.highlightColors[i] + "].");
        //td.appendChild(divColor);
    }
    
    td = tr.insertCell(tr.cells.length);
    var button = document.createElement("button");
    button.innerHTML = "Clear All";

    SimileAjax.DOM.registerEvent(button, "click", function() 
    {
        clearAll(timeline, bandIndices, table);
    });

    td.appendChild(button);
    
    div.appendChild(table);
}

var timerID = null;

function onKeyPress(timeline, bandIndices, table) 
{
    if (timerID != null) 
    {
    	if (debug) console.log("onKeyPress - timerID is not null [" + timerID + "], setting window clearTimeout wth timerID.");
        window.clearTimeout(timerID);
    }
    else
    {
    	if (debug) console.log("onKeyPress - timerID is null.");
    }

    timerID = window.setTimeout(function() 
    {
        performFiltering(timeline, bandIndices, table);
    }, 300);

    if (debug) console.log("onKeyPress: last value for timerID=[" + timerID + "].");
}

function cleanString(s) 
{
	try
	{
        return s.replace(/^\s+/, '').replace(/\s+$/, '');
	}
	catch (err)
	{
		return null;
	}
}

function performFiltering(timeline, bandIndices, table) 
{
	debug=false;

	// RRB
	if (debug) console.log("RRB - performFiltering...");
    if (debug) console.log("RRB - bandIndices.length=[" + bandIndices.length + "].");

    bandIndices[0]=3;
    bandIndices[1]=3;

    if (debug) console.log(bandIndices);
    
    var tr = table.rows[1];
    var myword=tr.cells[0].firstChild.value;
    if (debug) console.log("performFiltering: myword=[" +  myword + "].");

    var text = cleanString(myword);
    if (debug) console.log("After cleanString length is [" + text.length + "], =[" + text + "].");
    var filterMatcher = null;

    if (text && text.length > 0) 
    {
    	if (debug) console.log("RRB - performFiltering - has text, searching for=[" + text + "].");

    	// original RegExp based 
        //var regex = new RegExp(text, "i");
   
   		// XRegExp based search 
    	// unicode word test
    	// var regex = XRegExp('^\\pL+$');

		//var regex = XRegExp("決め", "i");

		var regex = XRegExp(text, "i");

		/*
		if (XRegExp.test(subject, myregexp)) 
		{
			// Successful match
		}
		else
		{
			// Match attempt failed
		}
		*/

        filterMatcher = function(evt) 
        {
            if (debug)
            {
        	   console.log("");
               console.log("filterMatcher: testing [" + text + "] against [" + evt.getText() + "]; also against [" + evt.getDescription() + "].");
            }

            var test1=regex.test(evt.getText(),regex);
            var test2=regex.test(evt.getDescription(),regex);

            if (debug)
            {
                console.log("test1=[" + test1 + "].");
                console.log("test2=[" + test2 + "].");
            }

            return (test1 || test2);

            //return regex.test(evt.getText()) || regex.test(evt.getDescription());
        };
    }
    else
    {
    	if (debug) console.log("RRB: performFilter : no text entered to search for...");
    }
    
    if (debug)
    {
        console.log("____________________________________________________________");
        console.log("Highlighting...");
    }

    var regexes = [];
    var hasHighlights = false;

    if (debug) console.log("tr.cells.length=[" + tr.cells.length + "].");

    for (var x = 1; x < tr.cells.length - 1; x++) 
    {
        var input = tr.cells[x].firstChild;

        if (input)
        {
        	if (debug) console.log("input before cleanstring [" + input.value + "].");
        }
        else
        {
        	if (debug) console.log("input doesn't exist.");
        }

        var text2 = cleanString(input.value);
        
        if (text2)
        {
        	if (debug) console.log("text2 after cleanstring [" + text2 + "], has length=[" + text2.length +"].");
        }
        else
        {
        	if (debug) console.log("text2 is null.");
        }

        if (text2 && text2.length > 0) 
        {
        	if (debug) console.log("RRB - performFiltering - text2.length > 0, length = [" + text2.length + "], text2=[" + text2 + "].");
            hasHighlights = true;
            
            // original RegExp based search
            //regexes.push(new RegExp(text2, "i"));

            // XRegExp based search
            //var unicodeWord = XRegExp('^\\pL+$');
            //regexes.push(myNewRegExp);

            var regex = XRegExp(text2, "i");
            regexes.push(regex);
        } 
        else 
        {
        	if (debug) console.log("RRB - performFiltering - text2.length=0.");
            regexes.push(null);
        }
    }

    var highlightMatcher = hasHighlights ? function(evt) 
    {
        var text = evt.getText();
        var description = evt.getDescription();
    
        if (debug)
        {
    	   console.log("RRB : performFiltering : hasHighlights : text=[" + text + "], description=[" + description + "].");
    	   console.log("RRB : performFiltering : hasHighlights : regexes.length=[" + regexes.length + "].");
        }

        for (var x = 0; x < regexes.length; x++) 
        {
            var regex = regexes[x];
            
            //if (regex != null && (regex.test(text) || regex.test(description)))
            if (regex !=null && (regex.test(text,regex) || regex.test(description,regex)) ) 
            {	
            	if (debug) console.log("RRB : performFiltering : hasHighlights : found a match : returning=[" + x + "].");
                return x;
            }
        }

        if (debug) console.log("RRB : performFiltering : hasHighlights : fell through : returning -1");

        return -1;

    } : null;
    
    if (debug) console.log("RRB - performFiltering : bandIndices.length=[" + bandIndices.length + "].");

    for (var i = 0; i < bandIndices.length; i++) 
    {
        var bandIndex = bandIndices[i];
        
        if (debug) console.log("adding matchers for bandIndex=[" + bandIndex + "].");

        timeline.getBand(bandIndex).getEventPainter().setFilterMatcher(filterMatcher);
        timeline.getBand(bandIndex).getEventPainter().setHighlightMatcher(highlightMatcher);
    }

    timeline.paint();

    // rrb
    if (debug) console.log("end of performFiltering...");
}

function clearAll(timeline, bandIndices, table) 
{
    var tr = table.rows[1];
    for (var x = 0; x < tr.cells.length - 1; x++) 
    {
        tr.cells[x].firstChild.value = "";
    }
    
    for (var i = 0; i < bandIndices.length; i++) 
    {
        var bandIndex = bandIndices[i];
        timeline.getBand(bandIndex).getEventPainter().setFilterMatcher(null);
        timeline.getBand(bandIndex).getEventPainter().setHighlightMatcher(null);
    }
    
    timeline.paint();
}