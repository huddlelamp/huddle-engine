if (Meteor.isClient) {

  var search = function(query) {

    Session.set('querySuggestions', []);

    var SEARCH_FIELDS = ["file^10", "_name"];

    var must = [];
    var must_not = [];

    //Create an elasticsearch-entry for each term in the query
    processQuery(query, function(term, isNegated, isPhrase) {
      var entry = {
        multi_match: {
          query: term,
          fields: SEARCH_FIELDS
        }
      };

      if (isPhrase) entry.multi_match.type = "phrase";

      if (isNegated)  must_not.push(entry);
      else            must.push(entry);
    });

    //elasticsearch can't handle empty must/must_not blocks
    var bool = {};
    if (must.length > 0) bool.must = must;
    if (must_not.length > 0) bool.must_not = must_not;

    console.log(bool);

    var q = {
      fields: ["file"],
      from: 0,
      size: 10,
      query: { bool: bool },
      highlight: {
        fields: {
          file: {}
        }
      }
    };
    console.log(q);

    ElasticSearch.query(q, function(err, result) {
      if (err) {
        console.error(err);
      }
      else {
        var results = result.data;

        var observer = function(i) { 
          return function(newDocument) {
            var results = Session.get('results');
            if (results === undefined) return;
            results.hits.hits[i].documentMeta = newDocument;
            Session.set('results', results);
          }; 
        };

        for (var i = 0; i < results.hits.hits.length ; i++) {
          var result = results.hits.hits[i];

          var cursor = DocumentMeta.find({_id: result._id});
          result.documentMeta = cursor.fetch()[0];

          cursor.observe({
            added   : observer(i),
            changed : observer(i),
            removed : observer(i),
          });
        }

        Session.set("lastQuery", query);
        Session.set("results", results);

        var pastQuery = PastQueries.findOne({ query: query });
        if (pastQuery === undefined) {
          var newDoc = {
            query : query,
            count : 1
          };
          PastQueries.insert(newDoc);
        } else {
          PastQueries.update({_id: pastQuery._id}, { $inc: {count: 1}});
        }
      }
    });
  };

  Template.searchIndex.rendered = function() {
    //Make sure scroll offset has a value
    Session.set("detailDocumentScrollOffset", 0);

    //Check the current text selection twice per second
    //This will populate detailDocumentSelectionRange and 
    //detailDocumentSelectedHighlights
    window.setInterval(checkCurrentSelection, 500);
  };

  Template.searchIndex.results = function() {
    return Session.get("results") || [];
  };

  Template.searchIndex.querySuggestions = function() {
    return Session.get("querySuggestions") || [];
  };

  Template.searchIndex.detailDocument = function() {
    return Session.get("detailDocument") || undefined;
  };

  Template.searchIndex.otherDevices = function() {
    return Session.get("otherDevices") || [];
  };

  Template.searchIndex.isFavorited = function() {
    var meta = this.documentMeta;
    return (meta && meta.favorited);
  };

  Template.searchIndex.hasComment = function() {
    var meta = this.documentMeta;
    return (meta && ((meta.comment && meta.comment.length > 0) || (meta.textHighlights && meta.textHighlights.length > 0)));
  };

  Template.searchIndex.wasWatched = function() {
    var meta = this.documentMeta;
    return (meta && meta.watched);
  };

  Template.searchIndex.comment = function() {
    if (this.documentMeta) return this.documentMeta.comment;
    return "";
  };

  var highlightDocumentContentDep = new Deps.Dependency();
  Template.searchIndex.helpers({
    'highlightDocumentContent': function() {
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
        Meteor.setTimeout(function() { highlightDocumentContentDep.changed(); }, 1);

        return encodeContent(content);
      }
    },

    'highlightDocumentSelectedSnippetContent': function() {
      highlightDocumentContentDep.depend();

      var snippetTime = Session.get('detailDocumentSelectedSnippetSetTime');
      var now = Date.now();
      if (!snippetTime) {
        var snippet = encodeContent(Session.get('detailDocumentSelectedSnippet'));
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

          Session.set('detailDocumentSelectedSnippetSetTime', now);

          //Put the text into a temporary element, walk over it and replace the text
          //with spaces while keeping HTML tags
          var spacedText = "";
          var spaceText = function() {
            if (this.nodeType === 3)  {
              spacedText += $(this).text().replace(/[^\n]/g, " ");
            }
            else {
              var tags = this.outerHTML.split($(this).html());
              var startTag = tags[0];
              var endTag = tags[1];
              spacedText += startTag;
              $(this).contents().each(spaceText);
              spacedText += endTag;
            }
          };
          var div = $("<div></div>");
          div.html(text);
          div.contents().each(spaceText);
          text = spacedText;

          //highlight the snippet when it arrived in the DOM (next run loop)
          Meteor.setTimeout(function() { 
            $(".snippetHighlighter").first().get(0).scrollIntoView(false);
            var scroll = $("#contentWrapper").scrollTop();
            Session.set("detailDocumentScrollOffset", scroll);

            //Show the highlight, wait for the CSS transition to finish, then hide it
            $("#selectedSnippetHighlight").css("opacity", 1.0);
            Meteor.setTimeout(function() { 
              $("#selectedSnippetHighlight").css("opacity", 0.0);
            }, 2000);
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
    },

    'detailDocumentHighlights': function() {
      highlightDocumentContentDep.depend();

      var doc = Session.get("detailDocument");
      if (doc === undefined) return;

      var meta = DocumentMeta.findOne({_id: doc._id});
      if (meta === undefined) return [];

      return meta.textHighlights || [];
    },

    /** Returns the content of a highlight overlay for a certain highlight. The content is 
        returned in such a way that the highlighted area will be above the text that was selected
        when the highlight was made **/
    'detailDocumentHighlightContent': function(highlight) {
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

    detailDocumentSelectedHighlightsCount: function() {
      var sh = Session.get('detailDocumentSelectedHighlights');
      if (sh === undefined || sh === null) return 0;
      return sh.length;
    },

    detailDocumentScrollOffset: function() {
      return Session.get("detailDocumentScrollOffset") || 0;
    },

    'thisDeviceBorderColorCSS': function() {
      return window.thisDeviceBorderColorCSS();
    },

    'deviceBackgroundColorCSS': function() {
      return 'background-color: '+window.deviceColorCSS(this);
    },

    'deviceSizePositionCSS': function() {
      return window.deviceSizePositionCSS(this);
    },

    'toSeconds': function(ms) {
      var time = new Date(ms);
      return time.getSeconds() + "." + time.getMilliseconds();
    },

    urlToDocument: function(id) {
      var s = Meteor.settings.public.elasticSearch;

      var url = s.protocol + "://" + s.host + ":" + s.port + "/" + IndexSettings.getActiveIndex() + "/" + s.attachmentsPath + "/" + id;
      return url;
    },

    count: function(array) {
      if (array === undefined || array === null) return 0;
      return array.length;
    },

    /** Pluralizer. Returns singular if number is 1, otherwise plural **/
    'pluralize': function(number, singular, plural) {
      if (number === undefined || number === null) number = 0;
      if (Array.isArray(number)) number = number.length;

      if (number === 1) return singular;
      return plural;
    },

    stringify: function(obj) {
      return JSON.stringify(obj);
    }
  });

  Template.searchIndex.events({
    'click #search-btn': function(e, tmpl) {
      var query = tmpl.$('#search-query').val();
      search(query);
    },

    'keyup #search-query': function(e, tmpl) {
      var query = tmpl.$('#search-query').val();
      if (query.trim().length === 0) {
        // $("#search-tag-wrapper").empty();
        return;
      }

      //On enter, start the search
      if (e.keyCode == 13) {
        search(query);
      //On every other key, try to fetch some query suggestions
      } else {
        // $("#search-tag-wrapper").empty();
        // tmpl.$('#search-query').val("");
        var unfinishedTerms = "";
        processQuery(query, function(term, isNegated, isPhrase, isFinished) {
          if (isFinished) {
            // $("#search-tag-wrapper").append("<span class='search-tag'>"+term+"</span>");
          } else {
            if (isPhrase) unfinishedTerms+= '"';
            unfinishedTerms += term+" ";
            // tmpl.$('#search-query').val(tmpl.$('#search-query').val()+" "+term);
          }
        });

        unfinishedTerms = unfinishedTerms.trim();
        // tmpl.$('#search-query').val(unfinishedTerms);

        var regexp = new RegExp('.*'+query+'.*', 'i');
        var suggestions = PastQueries.find(
          { query : { $regex: regexp } }, 
          { sort  : [["count", "desc"]] }
        );
        Session.set('querySuggestions', suggestions.fetch());
      }
    },

    'click .querySuggestion': function(e, tmpl) {
      tmpl.$('#search-query').val(this.query);
      search(this.query);
    },

    'click .hit': function(e, tmpl) {

      ElasticSearch.get(this._id, function(err, result) {
        if (err) {
          console.error(err);
        }
        else {
          result = result.data;  

          openDetailPopup(result);   

          // DocumentMeta._upsert(result._id, {$set: {watched: true}});

          // var observer = function() {
          //   return function(newDocument) {
          //     var doc = Session.get('detailDocument');
          //     doc.documentMeta = newDocument;
          //     Session.set('detailDocument', doc);
          //   }; 
          // };

          // var cursor = DocumentMeta.find({_id: result._id});
          // result.documentMeta = cursor.fetch()[0];

          // cursor.observe({
          //   added   : observer(),
          //   changed : observer(),
          //   removed : observer(),
          // });

          // Session.set("detailDocumentSelectedSnippet", undefined);
          // Session.set("detailDocument", result); 

          // $.fancybox({
          //   href: "#documentDetails",
          //   autoSize: false,
          //   autoResize: false,
          //   height: "952px",
          //   width: "722px",
          //   afterLoad: attachDetailDocumentEvents
          // });
        }
      });

    },

    'click .document-highlight': function(e, tmpl) {
      
      var that = this;
      ElasticSearch.get($(e.currentTarget).attr("documentid"), function(err, result) {
        if (err) {
          console.error(err);
        }
        else {
          result = result.data;     

          openDetailPopup(result, that.toString());

          // DocumentMeta._upsert(result._id, {$set: {watched: true}});

          // Session.set("detailDocumentSelectedSnippet", that.toString());
          // Session.set("detailDocumentSelectedSnippetSetTime", undefined);
          // Session.set("detailDocument", result); 
          
          // $.fancybox({
          //   href: "#documentDetails",
          //   autoSize: false,
          //   autoResize: false,
          //   height: "952px",
          //   width: "722px",
          //   afterLoad: attachDetailDocumentEvents
          // });
        }
      });

    },

    'click .favoritedStar': function(e, tmpl) {  
      var doc = this;
      if (doc.documentMeta && doc.documentMeta.favorited) {
        DocumentMeta._upsert(doc._id, {$set: {favorited: false}});
      } else {
        DocumentMeta._upsert(doc._id, {$set: {favorited: true}});
      }
    },

    /** Clicking the save comment button saves the current comment **/
    'click #saveCommentButton': function(e, tmpl) {
      console.log("save");
      var doc = Session.get("detailDocument");
      DocumentMeta._upsert(doc._id, {$set: {comment: $("#comment").val()}});
    },

    /** Called whenever the file content is scrolled. I am not quite sure why we have to bind
        this to #contentWrapper and not #textContent, but what u gonna do? **/
    'scroll #contentWrapper': function(e) {
      console.log("scroll");
      var scroll = $("#contentWrapper").scrollTop();
      Session.set("detailDocumentScrollOffset", scroll);
    },

    /** We keep #contentWrapper and .textSelectionWrapper scrolling in sync, so no manual scroll **/
    'scroll .textSelectionWrapper': function(e) {
      e.preventDefault();
    },

    'touchdown .deviceIndicator, click .deviceIndicator': function(e, tmpl) {
      e.preventDefault();
      sendCurrentContentTo($(e.currentTarget).attr("deviceid"));
    }
  });
}

var openDetailPopup = function(result, snippetText) {
  DocumentMeta._upsert(result._id, {$set: {watched: true}});

  var observer = function() {
    return function(newDocument) {
      var doc = Session.get('detailDocument');
      if (doc === undefined) return;
      doc.documentMeta = newDocument;
      Session.set('detailDocument', doc);
    }; 
  };

  var cursor = DocumentMeta.find({_id: result._id});
  result.documentMeta = cursor.fetch()[0];

  cursor.observe({
    added   : observer(),
    changed : observer(),
    removed : observer(),
  });

  Session.set("detailDocumentSelectedSnippet", snippetText);
  Session.set("detailDocumentSelectedSnippetSetTime", undefined);
  Session.set("detailDocument", result); 

  $.fancybox({
    href: "#documentDetails",
    autoSize: false,
    autoResize: false,
    height: "952px",
    width: "722px",
    afterLoad: setTimeout(attachDetailDocumentEvents, 250)
  });
};

var attachDetailDocumentEvents = function() {

  var toggleFavorited = function() {
    console.log("WOHOO");
    var doc = Session.get("detailDocument");
    if (doc.documentMeta && doc.documentMeta.favorited) {
      DocumentMeta._upsert(doc._id, {$set: {favorited: false}});
    } else {
      DocumentMeta._upsert(doc._id, {$set: {favorited: true}});
    }
  };
  
  var addHighlight = function(e) {
    var selection = Session.get('detailDocumentSelectionRange');
    var color = $(e.currentTarget).css("background-color");

    if (selection === undefined) return;

    var startOffset = selection[0];
    var endOffset = selection[1];

    //We do not want to allows overlapping highlights
    //Therefore, check every existing highlight if it intersects and modify it
    //to prevent overlap
    var doc = Session.get("detailDocument");
    var meta = doc.documentMeta;
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
    var selection = Session.get('detailDocumentSelectionRange');
    if (selection === undefined) return;

    var startOffset = selection[0];
    var endOffset = selection[1];

    //Walk over all highlights, check if they intersect the current selection
    //If they do, they are not taken into newHighlights
    var doc = Session.get("detailDocument");
    var meta = doc.documentMeta;
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

  var wasScrolled = function(e) {
    var scroll = $("#contentWrapper").scrollTop();
    Session.set("detailDocumentScrollOffset", scroll);
  };

  var prevent = function(e) {
    e.preventDefault();
  };

  $("#detailDocumentStar").off("click touchdown");
  $("#detailDocumentStar").on("click touchdown", toggleFavorited);

  $(".textHighlighter").off('click touchdown');
  $(".textHighlighter").on('click touchdown', addHighlight);

  $("#deleteHighlights").off('click touchdown');
  $("#deleteHighlights").on('click touchdown', deleteHighlights);

  $("#saveCommentButton").off('click touchdown');
  $("#saveCommentButton").on('click touchdown', saveComment);

  $("#contentWrapper").off('scroll');
  $("#contentWrapper").on('scroll', wasScrolled);

  $(".textSelectionWrapper").off('scroll');
  $(".textSelectionWrapper").on('scroll', prevent);
};

var sendCurrentContentTo = function(targetID) {
  var selection = Session.get('detailDocumentSelectionRange');

  if (selection !== undefined) {
    //If a selection was made, grab the selected text and send it as a snippet
    var selectedText = $("#textContent").text().slice(selection[0], selection[1]);
    huddle.broadcast("addtextsnippet", { target: targetID, snippet: selectedText } );
  } else {
    //If no selection was made, show the entire document
    var doc = Session.get("detailDocument");
    huddle.broadcast("showdocument", { target: targetID, documentID: doc._id } );
  }
};

/** Encodes file content for displaying **/
var encodeContent = function(text) {
  var pre = $("<pre></pre>");
  pre.html(text);
  pre.html(pre.html().replace(/&nbsp;/g, " "));
  return pre.html();
};

var checkCurrentSelection = function() {
  var selection = rangy.getSelection(0);
  
  if (selection.rangeCount === 0 || selection.isCollapsed) {
    Session.set("detailDocumentSelectionRange", undefined);
    Session.set('detailDocumentSelectedHighlights', []);
  } else {
    var range = selection.getRangeAt(0);
    var relativeRange = selectionRelativeTo(range, $("#textContent"));

    //If the range is not made inside the document text, we throw it away
    if (relativeRange === undefined) {
      Session.set("detailDocumentSelectionRange", undefined);
      Session.set('detailDocumentSelectedHighlights', []);
      return;
    }

    //Otherwise, populate detailDocumentSelectionRange and detailDocumentSelectedHighlights
    //The latter saves an array of highlights intersected by the selection
    Session.set("detailDocumentSelectionRange", relativeRange);

    var doc = Session.get("detailDocument");
    var meta = doc.documentMeta;
    var selectedHighlights = [];
    if (meta && meta.textHighlights) {
      for (var i = 0; i < meta.textHighlights.length; i++) {
        var intersection = rangeIntersection(relativeRange[0], relativeRange[1], meta.textHighlights[i][0], meta.textHighlights[i][1]);
        if (intersection !== undefined) {
          selectedHighlights.push(meta.textHighlights[i]);
        }
      }
    }
    Session.set('detailDocumentSelectedHighlights', selectedHighlights);
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
    if (this === selection.startContainer) {
      startOffset = currentOffset + selection.startOffset;
      doneStart = true;
    }

    if (this === selection.endContainer) {
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



/// HUDDLE STUFF

Huddle.on("showdocument", function(data) {
  var thisDevice = Session.get('thisDevice');
  if (data.target !== thisDevice.id) return;

  ElasticSearch.get(data.documentID, function(err, result) {
    if (err) {
      console.error(err);
    }
    else {
      result = result.data;    

      openDetailPopup(result); 

      // DocumentMeta._upsert(result._id, {$set: {watched: true}});

      // Session.set("detailDocumentSelectedSnippet", undefined);
      // Session.set("detailDocument", result); 
      // $.fancybox({
      //   href: "#documentDetails",
      //   autoSize: false,
      //   autoResize: false,
      //   height: "952px",
      //   width: "722px",
      //   afterLoad: attachDetailDocumentEvents
      // });
    }
  });
});

Huddle.on("addtextsnippet", function(data) {
  console.log(data);
  var thisDevice = Session.get('thisDevice');
  if (data.target !== thisDevice.id) return;

  //TODO also insert source document and the device that sent the snippet
  Snippets.insert({ device: thisDevice.id, text: data.snippet });
});