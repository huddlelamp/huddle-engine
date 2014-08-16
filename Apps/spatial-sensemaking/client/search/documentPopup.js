if (Meteor.isClient){
  var range;
  Template.documentPopup.rendered = function() {
    window.setTimeout(function() {
      //TODO guess there is a better way to access params, but I havn't found it yet ^^
      ElasticSearch.get(Router._currentController.params._id, function(err, result) {
        if (err) {
          console.error(err);
        }
        else {
          result = result.data;     

          DocumentMeta._upsert(result._id, {$set: {watched: true}});

          Session.set("lastQuery", Router._currentController.params._lastQuery);
          Session.set("document", result);   
        }
      });
    }, 1000); //TODO IndexSettings is not available otherwise

    var noSelectionCount = 0;
    window.setInterval(function() {
      var selection = rangy.getSelection(0);

      //TODO check if newRange.anchorNode and focusNode both have the textContent as a parent (at any level)
      //if not ignore this selection
      
      if (selection.rangeCount === 0 || selection.isCollapsed) {
        noSelectionCount++;
        if (noSelectionCount >= 5) range = undefined;
        return;
      }

      noSelectionCount = 0;

      var newRange = selection.getRangeAt(0);
      range = {
        startOffset : newRange.startOffset, 
        endOffset   : newRange.endOffset,
        anchorNode  : newRange.startContainer,
        focusNode   : newRange.endContainer,
      };
    }, 100);
  };

  Template.documentPopup.document = function() {
    return Session.get("document") || {};
  };


  Template.documentPopup.starImage = function() {
    var doc = Session.get("document");
    if (doc === undefined) return;

    var meta = DocumentMeta.findOne({_id: doc._id});

    if (meta && meta.favorited) return 'star_enabled.png';
    
    return 'star_disabled.png';
  };


  Template.documentPopup.comment = function() {
    var doc = Session.get("document");
    if (doc === undefined) return;

    var meta = DocumentMeta.findOne({_id: doc._id});

    if (meta) return meta.comment;

    return "";
  };

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

      //Rangy can't really handle &nbsp; because it is counted as a single character (a space)
      //Because of that, we simply replace every &nbsp; with spaces
      //Worst case, a few spaces are lost, but guess that's not so bad
      var pre = $("<pre></pre>");
      pre.attr("id", "textContent");
      pre.html(content);
      pre.html(pre.html().replace(/&nbsp;/g, " "));

      //Meteor updates the highlights too early - #textContent's content is not set then
      //Because of that, we use a custom dependency that is triggered in the next run loop
      Meteor.setTimeout(function() { contentDependency.changed(); } , 1);

      return pre.html();
    }
  };

  Template.documentPopup.textHighlights = function() {
    var doc = Session.get("document");
    if (doc === undefined) return;

    var meta = DocumentMeta.findOne({_id: doc._id});
    if (meta === undefined) return [];

    return meta.textHighlights || [];
  };

  Template.documentPopup.scrollOffset = function() {
    return Session.get("scrollOffset") || 0;
  };

  Template.documentPopup.events({
    'click .textHighlighter': function(e, tmpl) {
      if (range === undefined) return;

      //We need to figure out start/endOffset relative to the beginning of #textContent, so we know
      //where to place the text highlight. Walk recursively over all children of #textContent 
      //and count the length of all text nodes until we arrive at the anchor/focusNode
      var startOffset = 0;
      var endOffset = 0;
      var currentOffset = 0;  
      var doneAnchor = false;
      var doneFocus = false;
      var nodeOffsetCount = function() {
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
        else                      $(this).contents().each(nodeOffsetCount);
      };

      $("#textContent").contents().each(nodeOffsetCount);

      //If the selection is made backwards, the offset might be swapped
      if (startOffset > endOffset) {
        var temp = startOffset;
        startOffset = endOffset;
        endOffset = temp;
      }

      var color = $(e.target).css("background-color");

      var doc = Session.get("document");
      DocumentMeta._upsert(doc._id, {
        $push: {
          textHighlights: [ startOffset, endOffset, color ] 
        } 
      });
    },

    'scroll #contentWrapper': function(e) {
      //Not quite sure why scrolling the selection wrappers does not work, but setting
      //top seems to be a more-or-less good solution
      var scroll = $("#contentWrapper").scrollTop();
      Session.set("scrollOffset", scroll);
    },

    'scroll .textSelectionWrapper': function(e) {
      e.preventDefault();
    },

    'click .favoritedStar': function(e, tmpl) {
      var doc = Session.get("document");
      var meta = DocumentMeta.findOne({_id: doc._id});

      if (meta && meta.favorited) {
        DocumentMeta._upsert(doc._id, {$set: {favorited: false}});
      } else {
        DocumentMeta._upsert(doc._id, {$set: {favorited: true}});
      }
    },

    'click #saveCommentButton': function(e, tmpl) {
      var doc = Session.get("document");
      DocumentMeta._upsert(doc._id, {$set: {comment: $("#comment").val()}});
    },
  });

  Template.documentPopup.helpers({
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
    }
  });
}
