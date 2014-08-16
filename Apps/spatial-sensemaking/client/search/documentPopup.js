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

  Template.documentPopup.content = function() {
    var doc = Session.get("document");
    if (doc === undefined) return;

    var contentType = doc._source._content_type;
    if (contentType == "image/jpeg") {
      return '<img src="data:' + contentType + ';base64,' + doc._source.file + '" />';
    } else {
      var content = atob(doc._source.file);
      // content = $("<div/>").html(content).text();
      // console.log(content);
      // content = content.replace(/&nbsp;/g, " ");
      // console.log(content);

      var lastQuery = Session.get("lastQuery");
      if (lastQuery !== undefined) {
        processQuery(lastQuery, function(term, isNegated, isPhrase) {
          if (isNegated) return;
          content = content.replace(new RegExp("("+term+")", "gi"), "<em>$1</em>");
        });
      }

      // return '<div><pre id="textContent">' + content + '</pre></div>';
      var div = $("<div></div>");
      var pre = $("<pre></pre>");
      pre.attr("id", "textContent");
      pre.html(content);
      pre.html(pre.html().replace(/&nbsp;/g, " "));
      div.append(pre);
      // console.log(div[0].outerHTML);

      return div[0].outerHTML;
    }
  };

  Template.documentPopup.events({
    'click .textHighlighter': function(e, tmpl) {
      console.log("click");
      console.log(range);

      if (range === undefined) return;

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

        if (this.nodeType === 3) {
          console.log("TEXT NODE, ADDING "+this.length);
          console.log($(this).text());
          currentOffset += this.length;
        }
        else {
          var tags = this.outerHTML.split($(this).html());
          var startTag = tags[0];
          var endTag = tags[1];
          currentOffset += startTag.length;
          $(this).contents().each(nodeOffsetCount);
          currentOffset += endTag.length
        }
      };

      $("#textContent").contents().each(nodeOffsetCount);

      //If the selection is made backwards, the offset might be swapped
      if (startOffset > endOffset) {
        var temp = startOffset;
        startOffset = endOffset;
        endOffset = temp;
      }

      var text = $("#textContent").html();
      text = [ text.slice(0, startOffset), '<span class="textSelection">', text.slice(startOffset, endOffset), '</span>', text.slice(endOffset) ].join('');
      $("#textContent").html(text);
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
}
