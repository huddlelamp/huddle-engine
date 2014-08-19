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

        var observer = function(result) { 
          return function(newDocument) {
            result.documentMeta = newDocument;
            Session.set("results", results);
          }; 
        };

        for (var i = 0; i < results.hits.hits.length ; i++) {
          var result = results.hits.hits[i];

          var cursor = DocumentMeta.find({_id: result._id});
          result.documentMeta = cursor.fetch()[0];

          cursor.observe({
            added   : observer(result),
            changed : observer(result),
            removed : observer(result),
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

  Template.searchIndex.results = function() {
    return Session.get("results") || [];
  };

  Template.searchIndex.querySuggestions = function() {
    return Session.get("querySuggestions") || [];
  };

  Template.searchIndex.otherDevices = function() {
    return Session.get("otherDevices") || [];
  };

  Template.searchIndex.hasComment = function() {
    var meta = DocumentMeta.findOne({_id : this._id});
    return (meta && ((meta.comment && meta.comment.length > 0) || (meta.textHighlights && meta.textHighlights.length > 0)));
  };

  Template.searchIndex.wasWatched = function() {
    var meta = DocumentMeta.findOne({_id : this._id});
    return (meta && meta.watched);
  };

  Template.searchIndex.helpers({
    'thisDeviceBorderColorCSS': function() {
      return window.thisDeviceBorderColorCSS();
    },

    'deviceColorCSS': function() {
      return window.deviceColorCSS(this);
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

      $.fancybox({
        type: "iframe",
        href: "/documentPopup/"+encodeURIComponent(this._id)+"/"+encodeURIComponent(Session.get("lastQuery")),
        autoSize: false,
        autoResize: false,
        height: "952px",
        width: "722px"
      });
      
    },

    'click .document-highlight': function(e, tmpl) {
      
      $.fancybox({
        type: "iframe",
        href: "/documentPopup/"+encodeURIComponent($(e.currentTarget).attr("documentid"))+"/"+encodeURIComponent(Session.get("lastQuery"))+"/"+encodeURIComponent(this),
        autoSize: false,
        autoResize: false,
        height: "952px",
        width: "722px"
      });

    },

    'click .favoritedStar': function(e, tmpl) {
      if (this.documentMeta && this.documentMeta.favorited) {
        DocumentMeta._upsert(this._id, {$set: {favorited: false}});
      } else {
        DocumentMeta._upsert(this._id, {$set: {favorited: true}});
      }
    },

    'touchdown .deviceIndicator, click .deviceIndicator': function(e, tmpl) {
      e.preventDefault();

      var iframe = $("iframe").first().get(0);
      var win = iframe.contentWindow || iframe;

      var targetID = $(e.currentTarget).attr("deviceid");
      var text = win.getSelectedContent();
      if (text !== undefined) {
        huddle.broadcast("addtextsnippet", { target: targetID, snippet: text } );
      } else {
        var documentID = win.documentID;
        if (documentID === undefined) return;
        huddle.broadcast("showdocument", { target: targetID, documentID: documentID } );
      }
    }
  });
}
