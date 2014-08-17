if (Meteor.isClient){
  var range;
  Template.documentPopup.rendered = function() {
    window.setTimeout(function() {
      //TODO guess there is a better way to access params, but I havn't found it yet ^^
      ElasticSearch.get(decodeURIComponent(Router._currentController.params._id), function(err, result) {
        if (err) {
          console.error(err);
        }
        else {
          result = result.data;     

          DocumentMeta._upsert(result._id, {$set: {watched: true}});

          Session.set("lastQuery", decodeURIComponent(Router._currentController.params._lastQuery));
          Session.set("selectedSnippet", decodeURIComponent(Router._currentController.params._selectedSnippet));
          Session.set("document", result);   
        }
      });
    }, 1000); //TODO IndexSettings is not available otherwise

    //We check the current text selection regularly
    //Unfortunately, there seems to be no other way because we want to use that selection
    //when a color from the color palette is clicked/tapped. A click/tap removes the current
    //text selection, though, so we need to remember it before
    var noSelectionCount = 0;
    window.setInterval(function() {
      var selection = rangy.getSelection(0);
      
      if (selection.rangeCount === 0 || selection.isCollapsed) {
        noSelectionCount++;
        if (noSelectionCount >= 2) range = undefined;
      } else {
        noSelectionCount = 0;

        var newRange = selection.getRangeAt(0);
        range = {
          startOffset : newRange.startOffset, 
          endOffset   : newRange.endOffset,
          anchorNode  : newRange.startContainer,
          focusNode   : newRange.endContainer,
        };
      }

      countSelectedHighlights();
    }, 100);
  };

  /** Return the document shown in this popup **/
  Template.documentPopup.document = function() {
    return Session.get("document") || {};
  };

  /** Return the current scroll offset of the file content div **/
  Template.documentPopup.scrollOffset = function() {
    return Session.get("scrollOffset") || 0;
  };

  /** Return the number of currently selected text highlights, or undefined if no highlights
      are selected **/
  Template.documentPopup.selectedHighlightsCount = function() {
    return Session.get('selectedHighlightsCount') || undefined;
  };

  /** Return the URL of the favorites image for this document, depending on the
      favorited state **/
  Template.documentPopup.starImage = function() {
    var doc = Session.get("document");
    if (doc === undefined) return;

    var meta = DocumentMeta.findOne({_id: doc._id});

    if (meta && meta.favorited) return 'star_enabled.png';
    
    return 'star_disabled.png';
  };


  /** Return the comment made for this document, or an empty string if no comment was
      made **/
  Template.documentPopup.comment = function() {
    var doc = Session.get("document");
    if (doc === undefined) return;

    var meta = DocumentMeta.findOne({_id: doc._id});

    if (meta) return meta.comment;

    return "";
  };

  /** Return an array of all text highlights for the current document, or an empty array **/
  Template.documentPopup.textHighlights = function() {
    var doc = Session.get("document");
    if (doc === undefined) return;

    var meta = DocumentMeta.findOne({_id: doc._id});
    if (meta === undefined) return [];

    return meta.textHighlights || [];
  };

  Template.documentPopup.selectedSnippetContent = function() {
    //When the content changes and we have a selectedSnippet, scroll to that snippet
    contentDependency.depend();
    
    var snippetTime = Session.get('selectedSnippetSetTime');
    var now = Date.now();
    if (!snippetTime) {
      var snippet = encodeContent(Session.get('selectedSnippet'));
      var text = $("#textContent").html();
      if (snippet && snippet.length > 0 && text && text.length > 0) {
        //for some reason, if the snippet is at the very end of the file content it 
        //has an additional line break at the end. Because of that, remove line ends
        //from the end of the snippet
        var endsWithBreak = snippet.indexOf(String.fromCharCode(0x0A), snippet.length-1) !== -1;
        if (endsWithBreak) snippet = snippet.slice(0, snippet.length-1);

        var startOffset = text.indexOf(snippet);
        var endOffset = startOffset + snippet.length;

        if (startOffset < 0 || endOffset < 0) return;

        text = [ text.slice(0, startOffset), '<span class="snippetHighlighter">', text.slice(startOffset, endOffset), '</span>', text.slice(endOffset) ].join('');

        Session.set('selectedSnippetSetTime', now);

        //Put the text into a temporary element, walk over it and replace the text
        //with spaces
        var newText = "";
        var test = function() {
          if (this.nodeType === 3)  {
            newText += $(this).text().replace(/[^\n]/g, " ");
          }
          else {
            var tags = this.outerHTML.split($(this).html());
            var startTag = tags[0];
            var endTag = tags[1];
            newText += startTag;
            $(this).contents().each(test);
            newText += endTag;
          }
        };
        var div = $("<div></div>");
        div.html(text);
        div.contents().each(test);
        text = newText;

        //highlight the snippet when it arrived in the DOM (next run loop)
        Meteor.setTimeout(function() { 
          $(".snippetHighlighter").first().get(0).scrollIntoView(false);
          var scroll = $("#contentWrapper").scrollTop();
          Session.set("scrollOffset", scroll);

          //Show the highlight, wait for the CSS transition to finish, then hide it
          $("#selectedSnippetHighlight").css("opacity", 1.0);
          Meteor.setTimeout(function() { 
            $("#selectedSnippetHighlight").css("opacity", 0.0);
          }, 1200);
        }, 1);

        return text;
      }

      return "";
    } else {
      if ((now-snippetTime) < 5000) {
        return $("#selectedSnippetHighlight").html();
      } 

      return "";
    } 
  };

  /** Return the content of this document as HTML code. If the content is text, it will 
      contain highlights based on the query stored in the "lastQuery" session variable **/
  var contentDependency = new Deps.Dependency();
  Template.documentPopup.content = function() {
    var doc = Session.get("document");
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

      //Meteor updates the text highlights too early - #textContent's content is not set then
      //Because of that, we use a custom dependency that is triggered in the next run loop
      Meteor.setTimeout(function() { contentDependency.changed(); }, 1);

      return encodeContent(content);
    }
  };

  Template.documentPopup.events({
    /** Called when a text highlighter is clicked. If a valid selection was made in the file content,
        this will create a text highlight for that selection. Intersecting highlights will be taken
        care of **/
    'click .textHighlighter': function(e, tmpl) {
      var relativeRange = currentSelectionRelativeTo($("#textContent"));
      var startOffset = relativeRange[0];
      var endOffset = relativeRange[1];
      var color = $(e.target).css("background-color");

      //Check if the new highlight intersects an existing highlight
      var doc = Session.get("document");
      var meta = DocumentMeta.findOne({ _id: doc._id });      
      var updatedHighlights = [];
      if (meta && meta.textHighlights) {
        for (var i = 0; i < meta.textHighlights.length; i++) {
          var intersection = rangeIntersection(startOffset, endOffset, meta.textHighlights[i][0], meta.textHighlights[i][1]);
          
          //If the highlight is not intersected, keep it
          if (intersection === undefined) {
            updatedHighlights.push(meta.textHighlights[i]);
            continue;
          }

          //If there is an intersection and both highlights have the same color
          //we merge them. To do that, we alter the new highlight (will be added later)
          //and remove the old highlight
          if (color === meta.textHighlights[i][2]) {
            startOffset = Math.min(startOffset, meta.textHighlights[i][0]);
            endOffset = Math.max(endOffset, meta.textHighlights[i][1]);
            continue;
          }

          //If the entire highlight is intersected, it is removed
          if (intersection[0] === meta.textHighlights[i][0] && 
            intersection[1] === meta.textHighlights[i][1]) {
            continue;
          }

          //If the intersection is in the middle of the highlight, we need to split it
          if (intersection[0] > meta.textHighlights[i][0] && 
            intersection[1] < meta.textHighlights[i][1]) {
            var left = meta.textHighlights[i].slice(0);
            var right = meta.textHighlights[i].slice(0);
            left[1] = intersection[0];
            right[0] = intersection[1];
            updatedHighlights.push(left);
            updatedHighlights.push(right);

          }

          //If the start of the highlight is intersected, cut something from the old highlight
          if (intersection[0] === meta.textHighlights[i][0] && 
            intersection[1] < meta.textHighlights[i][1]) {
            meta.textHighlights[i][0] = intersection[1];
            updatedHighlights.push(meta.textHighlights[i]);
          }

          //If the end of the highlight is intersected, cut something from the old highlight
          if (intersection[0] > meta.textHighlights[i][0] && 
            intersection[1] === meta.textHighlights[i][1]) {
            meta.textHighlights[i][1] = intersection[0];
            updatedHighlights.push(meta.textHighlights[i]);
          }
        }
      }

      updatedHighlights.push([ startOffset, endOffset, color ]);

      var doc = Session.get("document");
      DocumentMeta._upsert(doc._id, {
        $set: {
          textHighlights: updatedHighlights
        } 
      });
    },

    /** Called whenever the file content is scrolled. I am not quite sure why we have to bind
        this to #contentWrapper and not #textContent, but what u gonna do? **/
    'scroll #contentWrapper': function(e) {
      var scroll = $("#contentWrapper").scrollTop();
      Session.set("scrollOffset", scroll);
    },

    /** We keep #contentWrapper and .textSelectionWrapper scrolling in sync, so no manual scroll **/
    'scroll .textSelectionWrapper': function(e) {
      e.preventDefault();
    },

    /** Clicking the favorite star toggles favorite state on or off **/
    'click .favoritedStar': function(e, tmpl) {
      var doc = Session.get("document");
      var meta = DocumentMeta.findOne({_id: doc._id});

      if (meta && meta.favorited) {
        DocumentMeta._upsert(doc._id, {$set: {favorited: false}});
      } else {
        DocumentMeta._upsert(doc._id, {$set: {favorited: true}});
      }
    },

    /** Clicking the save comment button saves the current comment **/
    'click #saveCommentButton': function(e, tmpl) {
      var doc = Session.get("document");
      DocumentMeta._upsert(doc._id, {$set: {comment: $("#comment").val()}});
    },
  });

  Template.documentPopup.helpers({
    /** Returns the content of a highlight overlay for a certain highlight. The content is 
        returned in such a way that the highlighted area will be above the text that was selected
        when the highlight was made **/
    'highlightContent': function(highlight) {
      contentDependency.depend();

      var startOffset = highlight[0];
      var endOffset = highlight[1];
      var color = highlight[2];
      
      var text = $("#textContent").text();
      if (text === undefined) return;
      var highlightText = "";

      //To create the content of a text highlight overlay, we basically copy #textContent
      //We then replace very non-newline character with a space (thank you monospace font!)
      //Also, obviously, we insert the text highlight where it belongs
      var textBeforeStart = text.slice(0, startOffset);
      textBeforeStart = textBeforeStart.replace(/[^\n]/g, " ");
      highlightText += textBeforeStart;

      highlightText += '<span class="textSelection" style="background-color: '+color+';">';
      var selectedText = text.slice(startOffset, endOffset);
      selectedText = selectedText.replace(/[^\n]/g, " ");
      highlightText += selectedText;
      highlightText += '</span>';

      var textAfterEnd = text.slice(endOffset);
      textAfterEnd = textAfterEnd.replace(/[^\n]/g, " ");
      highlightText += textAfterEnd;

      return highlightText;
    },

    /** Pluralizer. Returns singular if number is 1, otherwise plural **/
    'pluralize': function(number, singular, plural) {
      if (number === 1) return singular;
      return plural;
    }
  });

  /** Takes the current selection (stored in range) and returns it relative to the start of the
      given element. The returned range will be the offset from the first visible character in
      the element. HTML code is not counted. **/
  var currentSelectionRelativeTo = function(elem) {
    if (range === undefined) return undefined;

    //We need to figure out start/endOffset relative to the beginning of #textContent, so we know
    //where to place the text highlight. Walk recursively over all children of #textContent 
    //and count the length of all text nodes until we arrive at the anchor/focusNode
    var startOffset = 0;
    var endOffset = 0;
    var currentOffset = 0;  
    var doneAnchor = false;
    var doneFocus = false;
    var countOffset = function() {
      if (this === range.anchorNode) {
        startOffset = currentOffset + range.startOffset;
        doneAnchor = true;
      }

      if (this === range.focusNode) {
        endOffset = currentOffset + range.endOffset;
        doneFocus = true;
      }

      if (doneAnchor && doneFocus) return; 

      if (this.nodeType === 3)  currentOffset += this.length;
      else                      $(this).contents().each(countOffset);
    };

    $(elem).contents().each(countOffset);

    if (doneAnchor === false || doneFocus === false) return undefined;

    //If the selection is made backwards, the offset might be swapped
    if (startOffset > endOffset) {
      var temp = startOffset;
      startOffset = endOffset;
      endOffset = temp;
    }

    return [startOffset, endOffset];
  };

  /** Counts how many highlights are intersected by the current text selection and writes the
      result to the selectedHighlightsCount session variable **/
  var countSelectedHighlights = function() {
    //Check if the current selection intersects any existing highlights
    var relativeRange = currentSelectionRelativeTo($("#textContent"));
    if (relativeRange === undefined) {
      Session.set('selectedHighlightsCount', undefined);
      return;
    }

    var startOffset = relativeRange[0];
    var endOffset = relativeRange[1];

    var doc = Session.get("document");
    var meta = DocumentMeta.findOne({ _id: doc._id });   
    var count = 0;   
    if (meta && meta.textHighlights) {
      for (var i = 0; i < meta.textHighlights.length; i++) {
        var intersection = rangeIntersection(startOffset, endOffset, meta.textHighlights[i][0], meta.textHighlights[i][1]);
        if (intersection !== undefined) count++;
      }
    }

    if (count === 0) Session.set('selectedHighlightsCount', undefined);
    else Session.set('selectedHighlightsCount', count);
  };

  /** Encodes file content for displaying **/
  var encodeContent = function(text) {
    var pre = $("<pre></pre>");
    pre.html(text);
    pre.html(pre.html().replace(/&nbsp;/g, " "));
    return pre.html();
  };

  /** Returns the intersection between two ranges or undefined if they don't intersect **/
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
}
