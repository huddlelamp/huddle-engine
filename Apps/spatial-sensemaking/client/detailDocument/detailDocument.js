var popupID = 0;

Template.detailDocumentTemplate.content = function() {
  var doc = Session.get("detailDocument");
  if (doc === undefined) return undefined;

  var contentType = doc._source._content_type;
  if (contentType == "image/jpeg") {
    return '<img src="data:' + contentType + ';base64,' + doc._source.file + '" />';
  } else {
    var content = atob(doc._source.file);

    if (content.length < 1) return "";

    var lastQuery = Session.get("lastQuery");
    if (lastQuery !== undefined) {
      processQuery(lastQuery, function(term, isNegated, isPhrase) {
        if (isNegated) return;
        content = content.replace(new RegExp("("+term+")", "gi"), "<em>$1</em>");
      });
    }

    var insertHighlightIntoText = function(text, highlight, start, end, cssClasses) {
      return [
        text.slice(0, start),
        '<span class="highlight '+cssClasses+'" style="background-color: '+highlight[2]+';">',
        text.slice(start, end),
        '</span>',
        text.slice(end)
      ].join('');
    };

    var insertHighlightIntoDOM = function(elem, highlight, state) {
      if (elem === undefined || highlight === undefined) return;
      if (state === undefined) state = {};
      if (!state.offset) state.offset = 0;
      if (!state.opened) state.opened = false;
      if (!state.closed) state.closed = false;
      if (!state.cssClasses) state.cssClasses = "";

      if (state.closed) return state;

      var startOffset = highlight[0];
      var endOffset = highlight[1];

      if (elem.nodeType === 3)  {
        state.offset += elem.length;

        if (!state.opened && state.offset > startOffset) {
          //CASE 1: This node contains the start of the highlight
          state.opened = true;

          //Highlight start is defined by startOffset, highlightEnd depends on
          //if the highlight ends in this node or goes beyond this node (in which 
          //case it might be continued in CASE 3 and will be closed in CASE 2)
          var highlightStart = startOffset - (state.offset - elem.length);
          var highlightEnd = elem.length;
          if (state.offset >= endOffset) {
            highlightEnd = endOffset - (state.offset - elem.length);
            state.closed = true;
          }

          $(elem).replaceWith(insertHighlightIntoText(
            $(elem).text(), 
            highlight, 
            highlightStart, 
            highlightEnd,
            state.cssClasses
          ));
        } else {
          if (state.opened) {
            if (state.offset >= endOffset) {
              //CASE 2: End of highlight is in this node
              //Since the highlight started before this node, we highlight from the
              //beginning to endOffset
              var highlightStart = 0;
              var highlightEnd = endOffset - (state.offset - elem.length);

              $(elem).replaceWith(insertHighlightIntoText(
                $(elem).text(), 
                highlight, 
                highlightStart, 
                highlightEnd,
                state.cssClasses
              ));

              state.closed = true;
            } else {
              //CASE 3: the entire node is part of the highlight, it doesn't end here
              //Therefore, the entire node needs to be highlighted
              $(elem).replaceWith(insertHighlightIntoText(
                $(elem).text(), 
                highlight, 
                0, 
                elem.length,
                state.cssClasses
              ));
            }
          }
        }
      } else {
        //CASE 4: If this node is not a textnode, we go deeper
        $(elem).contents().each(function() { state = insertHighlightIntoDOM(this, highlight, state); });
      }

      return state;
    };

    //Add the snippet highlight and text highlights if necessary
    //In order to be able to count the text chars only (without html tags)
    //we create a temporary element that we can walk over and find text nodes
    var tempContent = $("<div />").html(content);

    //TEXT HIGHLIGHTS    
    var meta = DocumentMeta.findOne({_id: this._id});
    if (meta !== undefined && meta.textHighlights !== undefined) {
      for (var i=0; i<meta.textHighlights.length; i++) {
        var highlight = meta.textHighlights[i];
        var color = new tinycolor(highlight[2]);
        color = color.setAlpha(0.35).toRgbString();
        highlight[2] = color;
        insertHighlightIntoDOM(tempContent[0], meta.textHighlights[i]);
      }
    }

    //PREVIEW SNIPPET
    var snippet = Session.get('detailDocumentPreviewSnippet');
    var snippetLocked = Session.get('detailDocumentPreviewSnippetLocked');
    if (!snippetLocked && snippet && snippet.length > 0) {
      //for some reason, if the snippet is at the very end of the file content it 
      //has an additional line break at the end. Because of that, remove line ends
      //from the end of the snippet
      snippet = $("<span />").html(snippet).text();
      var endsWithBreak = snippet.indexOf(String.fromCharCode(0x0A), snippet.length-1) !== -1;
      if (endsWithBreak) snippet = snippet.slice(0, snippet.length-1);

      var startOffset = tempContent.text().indexOf(snippet);
      var endOffset = startOffset + snippet.length;
      if (startOffset >= 0 && endOffset >= 0) {
        //Create a fake highlight and add it to the text
        var fakeHighlight = [startOffset, endOffset, "transparent"];
        insertHighlightIntoDOM(tempContent[0], fakeHighlight, { cssClasses: "previewSnippet" });

        //Remember the current popup ID. For the timeouts, we check if the ID
        //changed - if so the popup was closed and/or reopened and we shouldn't
        //perform the snippet animations
        var currentID = popupID;

        //Fade in and out the preview snippet
        Meteor.setTimeout(function() {
          if (currentID === popupID) {
            $(".highlight.previewSnippet").first().scrollintoview({
              duration: "normal",
              complete: function() {
                if (currentID === popupID) {
                  $(".highlight.previewSnippet").css("background-color", "rgba(255,0,0,1.0)");
                  $(".highlight.previewSnippet").css("color", "white");

                  Meteor.setTimeout(function() {
                    if (currentID === popupID) {
                      $(".highlight.previewSnippet").css("background-color", "transparent");
                      $(".highlight.previewSnippet").css("color", "");

                      //After the fade-out we lock the snippet so it will not be shown again
                      Meteor.setTimeout(function() { 
                        if (currentID === popupID) {
                          Session.set('detailDocumentPreviewSnippetLocked', true); 
                        }
                      }, 1000);
                    }
                  }, 2500);
                }
              }
            });
          }
        }, 1000);
      }
    }

    content = tempContent.html();

    return encodeContent(content);
  }
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
    // autoSize: false,
    // autoResize: false,
    // height: "952px",
    // width: "722px",
    beforeLoad: function() {
      isOpen = true;

      Session.set('detailDocumentPreviewSnippetLocked', false);
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
    beforeClose: function() {
      isOpen = false;

      //Increase popup ID on close, so we know the popup has changed
      popupID++;

      var doc = Session.get("detailDocument");
      DocumentMeta._upsert(doc._id, {$set: {comment: $("#comment").val()}});
    },
    afterClose: function() {
      Session.set('detailDocumentPreviewSnippetLocked', false);
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
    e.preventDefault();

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
    e.preventDefault();

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
    if (doc === undefined) return;

    var oldMeta = DocumentMeta.findOne({_id: doc._id});

    var oldComment = oldMeta.comment;
    var newComment = $("#comment").val();

    if (oldComment !== newComment) {
      DocumentMeta._upsert(doc._id, {$set: {comment: newComment}});

      $("#savedText").css("opacity", 1.0);
      Meteor.setTimeout(function() {
        $("#savedText").css("opacity", 0);
      }, 1700);
    }
  };

  var timedSaveCommentTimer;
  var timedSaveComment = function() {
    Meteor.clearTimeout(timedSaveCommentTimer);
    timedSaveCommentTimer = Meteor.setTimeout(function() {
      saveComment();
    }, 2000);
  };

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

  var openWorldView = function(e) {
    e.preventDefault();

    if (!Template.deviceWorldView) return;
    Template.deviceWorldView.show();
  };

  var fixFixed = function() {
    //When the keyboard is shown or hidden, elements with position: fixed are
    //fucked up. This can sometimes be fixed by scrolling the body a little
    Meteor.setTimeout(function() {
      $("body").scrollTop($("body").scrollTop()+1);
    }, 1);
  };

  var prevent = function(e) {
    e.preventDefault();
  };

  $("#detailDocumentStar").off("click");
  $("#detailDocumentStar").on("click", toggleFavorited);

  $(".highlightButton").off('touchend mouseup');
  $(".highlightButton").on('touchend mouseup', addHighlight);

  $("#deleteHighlightButton").off('touchend mouseup');
  $("#deleteHighlightButton").on('touchend mouseup', deleteHighlights);

  $("#comment").off('focus blur');
  $("#comment").on('focus blur', fixFixed);
  // $("#comment").on('blur', saveComment);

  $("#comment").off("keyup");
  $("#comment").on("keyup", timedSaveComment);

  // $("#saveCommentButton").off('click');
  // $("#saveCommentButton").on('click', saveComment);

  // Meteor.setTimeout(function() {
  //   $("#devicedropdown_chosen").off('click touchdown chosen:showing_dropdown');
  //   $("#devicedropdown_chosen").on('click touchdown chosen:showing_dropdown', deviceSelectorClick);
  // }, 1);

  $("#devicedropdown").off('change');
  $("#devicedropdown").on('change', deviceSelected);

  $("#openWorldView").off('touchend mouseup');
  $("#openWorldView").on('touchend mouseup', openWorldView);
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
  return text;
  // var pre = $("<pre></pre>");
  // pre.html(text);
  // pre.html(pre.html().replace(/&nbsp;/g, " "));
  // return pre.html();
};