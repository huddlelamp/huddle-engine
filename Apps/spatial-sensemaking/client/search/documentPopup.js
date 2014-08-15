if (Meteor.isClient){
  Template.documentPopup.rendered = function() {
    window.setTimeout(function() {
      //TODO guess there is a better way to access params, but I havn't found it yet ^^
      ElasticSearch.get(Router._currentController.params._id, function(err, result) {
        if (err) {
          console.error(err);
        }
        else {
          result = result.data;

          // var cursor = DocumentMeta.find({_id: result._id});
          // result.documentMeta = cursor.fetch()[0];

          // cursor.observe({
          //   added: function(result) { return function(newDocument) {
          //     result.documentMeta = newDocument;
          //     Session.set("document", result);
          //   }; }(result),

          //   changed: function(result) { return function(newDocument) {
          //     result.documentMeta = newDocument;
          //     Session.set("document", result);
          //   }; }(result),

          //   removed: function(result) { return function(newDocument) {
          //     result.documentMeta = newDocument;
          //     Session.set("document", result);
          //   }; }(result),
          // });       

          Session.set("lastQuery", Router._currentController.params._lastQuery);
          Session.set("document", result);   
        }
      });
    }, 250); //TODO IndexSettings is not available otherwise
  };
};

Template.documentPopup.document = function() {
  return Session.get("document") || {};
};


Template.documentPopup.starImage = function() {
  var doc = Session.get("document");

  if (doc === undefined) return;

  var meta = DocumentMeta.findOne({_id: doc._id});

  if (meta && meta.favorited) return 'star_enabled.png';
  else return 'star_disabled.png';
};

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

    return '<div><pre>' + content + '</pre></div>';
  }
};