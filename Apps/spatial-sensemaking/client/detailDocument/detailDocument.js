var contentDependency = new Deps.Dependency();

Template.detailDocumentTemplate.rendered = function() {
  //Make sure scroll offset has a value
  Session.set("detailDocumentScrollOffset", 0);
};

Template.detailDocumentTemplate.content = function() {
  var doc = Session.get("detailDocument");
  if (doc === undefined) return;

  var contentType = doc._source._content_type;
  if (contentType == "image/jpeg") {
    return '<img src="data:' + contentType + ';base64,' + doc._source.file + '" />';
  } else {
    var content = atob(doc._source.file);

    var lastQuery = Session.get("lastQuery");
    if (lastQuery !== undefined) {
      processQuery(lastQuery, function(term, isNegated, isPhrase) {
        if (isNegated) return;
        content = content.replace(new RegExp("("+term+")", "gi"), "<em>$1</em>");
      });
    }

    //Meteor updates the text highlights before this content enters the DOM
    //Therefore, trigger a custom dependency on the next run loop
    Meteor.setTimeout(function() { contentDependency.changed(); }, 1);

    return encodeContent(content);
  }
};

Template.detailDocumentTemplate.previewSnippetContent = function() {
  contentDependency.depend();

  var snippet = encodeContent(Session.get('detailDocumentPreviewSnippet'));
  var snippetHTML = Session.get('detailDocumentPreviewSnippetHTML');
  var text = $("#content").html();
  if (snippetHTML === undefined && snippet && snippet.length > 0 && text && text.length > 0) {
    //for some reason, if the snippet is at the very end of the file content it 
    //has an additional line break at the end. Because of that, remove line ends
    //from the end of the snippet
    var endsWithBreak = snippet.indexOf(String.fromCharCode(0x0A), snippet.length-1) !== -1;
    if (endsWithBreak) snippet = snippet.slice(0, snippet.length-1);

    var startOffset = text.indexOf(snippet);
    var endOffset = startOffset + snippet.length;

    if (startOffset < 0 || endOffset < 0) return;

    text = [ 
      text.slice(0, startOffset), 
      '<span class="highlight">', 
      text.slice(startOffset, endOffset), 
      '</span>', 
      text.slice(endOffset) 
    ].join('');

    //Put the text into a temporary element, walk over it and replace the text
    //with spaces while keeping HTML tags
    snippetHTML = "";
    var spaceText = function() {
      if (this.nodeType === 3)  {
        snippetHTML += $(this).text().replace(/[^\n]/g, " ");
      }
      else {
        var tags = this.outerHTML.split($(this).html());
        var startTag = tags[0];
        var endTag = tags[1];
        snippetHTML += startTag;
        $(this).contents().each(spaceText);
        snippetHTML += endTag;
      }
    };
    var div = $("<div></div>");
    div.html(text);
    div.contents().each(spaceText);

    //highlight the snippet when it arrived in the DOM (next run loop)
    Meteor.setTimeout(function() { 
      var bodyScroll = $("body").scrollTop();
      $("#previewSnippetHighlight .highlight").first().get(0).scrollIntoView(false);
      var scroll = $("#contentWrapper").scrollTop();
      Session.set("detailDocumentScrollOffset", scroll);

      //kind of hackish way to fix fancybox positioning on the iPad
      // $("body").scrollTop(bodyScroll); //use to fix scrollup on desktop
      $("body").scrollTop(0); 
      $(".fancybox-wrap").css("top", "35px");

      //Show the highlight, wait for the CSS transition to finish, then hide it
      $("#previewSnippetHighlight").css("opacity", 1.0);
      Meteor.setTimeout(function() { 
        $("#previewSnippetHighlight").css("opacity", 0.0);
      }, 2000);
    }, 500);

    Session.set('detailDocumentPreviewSnippetHTML', snippetHTML);
  }

  return snippetHTML;
};

/** Returns the content of a highlight overlay for a certain highlight. The content is 
        returned in such a way that the highlighted area will be above the text that was selected
        when the highlight was made **/
Template.detailDocumentTemplate.highlightContent = function(highlight) {
  var startOffset = highlight[0];
  var endOffset = highlight[1];
  var color = highlight[2];
  
  var text = $("#content").text();
  if (text === undefined) return;
  var highlightText = "";

  //To create the content of a text highlight overlay, we basically copy #textContent
  //We then replace very non-newline character with a space (thank you monospace font!)
  //Also, obviously, we insert the text highlight where it belongs
  var textBeforeStart = text.slice(0, startOffset);
  textBeforeStart = textBeforeStart.replace(/[^\n]/g, " ");
  highlightText += textBeforeStart;

  highlightText += '<span class="highlight" style="background-color: '+color+';">';
  var selectedText = text.slice(startOffset, endOffset);
  selectedText = selectedText.replace(/[^\n]/g, " ");
  highlightText += selectedText;
  highlightText += '</span>';

  var textAfterEnd = text.slice(endOffset);
  textAfterEnd = textAfterEnd.replace(/[^\n]/g, " ");
  highlightText += textAfterEnd;

  return highlightText;
};

Template.detailDocumentTemplate.highlights = function() {
  contentDependency.depend();

  var meta = DocumentMeta.findOne({_id: this._id});
  if (meta === undefined) return [];

  return meta.textHighlights || [];
};

Template.detailDocumentTemplate.comment = function() {
  var meta = DocumentMeta.findOne({_id: this._id});
  if (meta) return meta.comment;
  return "";
};

Template.detailDocumentTemplate.isFavorited = function() {
  var meta = DocumentMeta.findOne({_id: this._id});
  return (meta && meta.favorited);
};

Template.detailDocumentTemplate.deviceColorCSS = function() {
  var info = DeviceInfo.findOne({ _id: this.id });
  if (info === undefined || !info.colorDeg) return "";

  var color = window.degreesToColor(info.colorDeg);

  return 'color: rgb('+color.r+', '+color.g+', '+color.b+');';
};

Template.detailDocumentTemplate.document = function() {
  return Session.get("detailDocument") || undefined;
};

Template.detailDocumentTemplate.scrollOffset = function() {
  return Session.get("detailDocumentScrollOffset") || 0;
};

Template.detailDocumentTemplate.otherDevices = function() {
  return Session.get("otherDevices") || [];
};


//
// "PUBLIC" API
//

/** Open method that opens this template in a fancybox with the given document
    and an optional snippetText that is highlighted after the load. This method
    is supposed to be called by other templates. **/
Template.detailDocumentTemplate.open = function(doc, snippetText) {
  DocumentMeta._upsert(doc._id, {$set: {watched: true}});

  $.fancybox({
    href: "#documentDetails",
    autoSize: false,
    autoResize: false,
    height: "952px",
    width: "722px",
    beforeLoad: function() {
      Session.set('detailDocumentPreviewSnippetHTML', undefined);
      Session.set("detailDocumentPreviewSnippet", snippetText);
      Session.set("detailDocument", doc); 
    },
    afterLoad: function() { 
      //Dirty hack: 500ms delay so we are pretty sure that all DOM elements arrived
      Meteor.setTimeout(function() {
        attachEvents();
        $("#devicedropdown").chosen({
          width: "125px",
          disable_search_threshold: 100
        });
      }, 500);
    },
    afterClose: function() {
      Session.set('detailDocumentPreviewSnippetHTML', undefined);
      Session.set("detailDocumentPreviewSnippet", undefined);
      Session.set("detailDocument", undefined); 
    },
  });
};

Template.detailDocumentTemplate.currentlySelectedContent = function() {
  var selection = getContentSelection();
  if (selection === undefined) return "";
  return $("#content").text().slice(selection[0], selection[1]);
};


////////////////////
// ATTACH EVENTS //
///////////////////


var attachEvents = function() {

  var toggleFavorited = function() {
    var doc = Session.get("detailDocument");
    var meta = DocumentMeta.findOne({_id: doc._id});
    if (meta && meta.favorited) {
      DocumentMeta._upsert(doc._id, {$set: {favorited: false}});
    } else {
      DocumentMeta._upsert(doc._id, {$set: {favorited: true}});
    }
  };
  
  var addHighlight = function(e) {
    var selection = getContentSelection();
    var color = $(e.currentTarget).css("background-color");

    if (selection === undefined) return;

    var startOffset = selection[0];
    var endOffset = selection[1];

    //We do not want to allows overlapping highlights
    //Therefore, check every existing highlight if it intersects and modify it
    //to prevent overlap
    var doc = Session.get("detailDocument");
    var meta = DocumentMeta.findOne({_id: doc._id});
    var updatedHighlights = [];
    if (meta && meta.textHighlights) {
      for (var i = 0; i < meta.textHighlights.length; i++) {
        var highlight = meta.textHighlights[i];
        var intersection = rangeIntersection(startOffset, endOffset, highlight[0], highlight[1]);
        
        //If the highlight is not intersected, keep it as it is
        if (intersection === undefined) {
          updatedHighlights.push(highlight);
          continue;
        }

        //If there is an intersection and both highlights have the same color
        //we merge them. To do that, we alter the new highlight (will be added later)
        //and remove the old highlight
        if (color === highlight[2]) {
          startOffset = Math.min(startOffset, highlight[0]);
          endOffset = Math.max(endOffset, highlight[1]);
          continue;
        }

        //If the entire highlight is intersected, it is removed (= not kept)
        if (intersection[0] === highlight[0] && 
          intersection[1] === highlight[1]) {
          continue;
        }

        //If the intersection is in the middle of the highlight, we need to split it
        if (intersection[0] > highlight[0] && 
          intersection[1] < highlight[1]) {
          var left = highlight.slice(0);
          var right = highlight.slice(0);
          left[1] = intersection[0];
          right[0] = intersection[1];
          updatedHighlights.push(left);
          updatedHighlights.push(right);

        }

        //If the start of the existing highlight is intersected, cut that part away
        if (intersection[0] === highlight[0] && 
          intersection[1] < highlight[1]) {
          highlight[0] = intersection[1];
          updatedHighlights.push(highlight);
        }

        //If the end of the existing highlight is intersected, cut that part away
        if (intersection[0] > highlight[0] && 
          intersection[1] === highlight[1]) {
          highlight[1] = intersection[0];
          updatedHighlights.push(highlight);
        }
      }
    }

    //Finally, add our selection as a new highlight, which now shouldn't interect
    //any other highlight. Then, write our new highlights in the DB
    updatedHighlights.push([ startOffset, endOffset, color ]);
    DocumentMeta._upsert(doc._id, { $set: { textHighlights: updatedHighlights } });

    //Clear selection
    rangy.getSelection(0).removeAllRanges();
  };

  
  var deleteHighlights = function() {
    var selection = getContentSelection();
    if (selection === undefined) return;

    var startOffset = selection[0];
    var endOffset = selection[1];

    //Walk over all highlights, check if they intersect the current selection
    //If they do, they are not taken into newHighlights
    var doc = Session.get("detailDocument");
    var meta = DocumentMeta.findOne({_id: doc._id});
    var newHighlights = [];
    if (meta && meta.textHighlights) {
      for (var i = 0; i < meta.textHighlights.length; i++) {
        var highlight = meta.textHighlights[i];
        var intersection = rangeIntersection(startOffset, endOffset, highlight[0], highlight[1]);
        if (intersection === undefined) {
          newHighlights.push(meta.textHighlights[i]);
        }
      }
    }

    //Insert the "surviving" highlights back into the DB
    DocumentMeta._upsert(doc._id, { $set: { textHighlights: newHighlights } });

    //Clear selection
    rangy.getSelection(0).removeAllRanges();
  };

  var saveComment = function() {
    var doc = Session.get("detailDocument");
    DocumentMeta._upsert(doc._id, {$set: {comment: $("#comment").val()}});
  };

  var scrolled = function(e) {
    var scroll = $("#contentWrapper").scrollTop();
    Session.set("detailDocumentScrollOffset", scroll);
  };

  // var deviceSelectorClick = function() {
  //   console.log("GOT "+Template.detailDocumentTemplate.currentlySelectedContent());
  //   Session.set('deviceSelectorSnippetToSend', Template.detailDocumentTemplate.currentlySelectedContent());
  // };

  var deviceSelected = function() {
    var select = $("#devicedropdown")[0];
    var option = select.options[select.selectedIndex];

    var targetID = $(option).attr("deviceid");

    //set the select back to the placeholder
    select.selectedIndex = 0;
    $("#devicedropdown").trigger("chosen:updated");

    if (targetID === undefined) return;

    var text = Session.get('deviceSelectorSnippetToSend');

    if (text !== undefined && text.length > 0) {
      huddle.broadcast("addtextsnippet", { target: targetID, snippet: text } );
    } else {
      //If no selection was made, show the entire document
      var doc = Session.get("detailDocument");
      if (doc === undefined) return;
      huddle.broadcast("showdocument", { target: targetID, documentID: doc._id } );
    }
  };

  var openWorldView = function() {
    if (!Template.deviceWorldView) return;
    Template.deviceWorldView.show();
  };

  var prevent = function(e) {
    e.preventDefault();
  };

  $("#detailDocumentStar").off("click touchdown");
  $("#detailDocumentStar").on("click touchdown", toggleFavorited);

  $(".highlightButton").off('click touchdown');
  $(".highlightButton").on('click touchdown', addHighlight);

  $("#deleteHighlightButton").off('click touchdown');
  $("#deleteHighlightButton").on('click touchdown', deleteHighlights);

  $("#saveCommentButton").off('click touchdown');
  $("#saveCommentButton").on('click touchdown', saveComment);

  $("#contentWrapper").off('scroll');
  $("#contentWrapper").on('scroll', scrolled);

  $(".highlightWrapper").off('scroll');
  $(".highlightWrapper").on('scroll', prevent);

  // Meteor.setTimeout(function() {
  //   $("#devicedropdown_chosen").off('click touchdown chosen:showing_dropdown');
  //   $("#devicedropdown_chosen").on('click touchdown chosen:showing_dropdown', deviceSelectorClick);
  // }, 1);

  $("#devicedropdown").off('change');
  $("#devicedropdown").on('change', deviceSelected);

  $("#openWorldView").off('click touchdown');
  $("#openWorldView").on('click touchdown', openWorldView);
};


///////////////
// SELECTION //
///////////////

var getContentSelection = function() {
  var selection = rangy.getSelection(0);
  
  if (selection.rangeCount === 0 || selection.isCollapsed) {
    return undefined;
  } else {
    var range = selection.getRangeAt(0);
    var relativeRange = selectionRelativeTo(range, $("#content"));

    return relativeRange;
  }
};


var selectionRelativeTo = function(selection, elem) {
  //The selection is relative to a childnode of elem (at least it should be)
  //Therefore, we count the length of every child of elem until we arrive at the
  //start/endnode of the selection.
  var startOffset = 0;
  var endOffset = 0;
  var currentOffset = 0;  
  var doneStart = false;
  var doneEnd = false;

  var countRecursive = function() {
    if (this.isSameNode(selection.startContainer)) {
      startOffset = currentOffset + selection.startOffset;
      doneStart = true;
    }

    if (this.isSameNode(selection.endContainer)) {
      endOffset = currentOffset + selection.endOffset;
      doneEnd = true;
    }

    if (doneStart && doneEnd) return; 

    if (this.nodeType === 3)  currentOffset += this.length;
    else                      $(this).contents().each(countRecursive);
  };

  //Start the recursion
  $(elem).contents().each(countRecursive);

  //If we didn't find the start or endnode they are not children of elem
  if (doneStart === false || doneEnd === false) return undefined;

  //If the selection is made backwards, the offset might be swapped
  if (startOffset > endOffset) {
    var temp = startOffset;
    startOffset = endOffset;
    endOffset = temp;
  }

  return [startOffset, endOffset];
};

var rangeIntersection = function(s1, e1, s2, e2) {
  if (s2 <= s1 && e2 >= s1) {
    if (e2 > e1) return [s1, e1];
    return [s1, e2];
  }

  if (s1 <= s2 && e1 >= s2) {
    if (e1 > e2) return [s2, e2];
    return [s2, e1];
  }

  return undefined;
};


//////////
// MISC //
//////////


/** Encodes file content for displaying **/
var encodeContent = function(text) {
  var pre = $("<pre></pre>");
  pre.html(text);
  pre.html(pre.html().replace(/&nbsp;/g, " "));
  return pre.html();
};