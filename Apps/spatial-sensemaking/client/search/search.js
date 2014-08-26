if (Meteor.isClient) {

  var SEARCH_RESULTS_PER_PAGE = 10;
  var SEARCH_FIELDS = ["file^10", "_name"];

  var search = function(query, page) {
    if (query === undefined) return;
    if (page === undefined) page = 1;

    Session.set('querySuggestions', []);

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
      from: (page-1)*SEARCH_RESULTS_PER_PAGE,
      size: SEARCH_RESULTS_PER_PAGE,
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
        Session.set("lastQueryPage", page);
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
    //If we have some parameters for search passed to us, do the appropiate search
    var query = Router._currentController.params._query ? decodeURIComponent(Router._currentController.params._query) : undefined;
    var page = Router._currentController.params._page ? decodeURIComponent(Router._currentController.params._page) : undefined;

    if (query !== undefined) {
      if (page === undefined) page = 1;

      $('#search-query').val(query);
      // Meteor.setTimeout(function() {
      //   search(query, page);
      // }, 1000);

      console.log(IndexSettings.find({}).count());
      console.log(IndexSettings.findOne({ name: "default" }));

      search(query, page);
    }
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

  Template.searchIndex.pagination = function() {
    var pages = Math.ceil(this.hits.total/SEARCH_RESULTS_PER_PAGE);
    var query = encodeURIComponent(Session.get("lastQuery"));
    var result = "";
    for (var i=1; i<=pages; i++) {
      if (i == Session.get("lastQueryPage")) {
        result += '<span class="paginationLink active">'+i+'</span>';
      } else {
        result += "<a href='#' query='"+query+"' page='"+i+"' class='paginationLink'>"+i+"</a>";
      }
    }

    return result;
  };

  var highlightDocumentContentDep = new Deps.Dependency();
  Template.searchIndex.helpers({
    'toSeconds': function(ms) {
      var time = new Date(ms);
      return time.getSeconds() + "." + time.getMilliseconds();
    },

    urlToDocument: function(id) {
      var s = Meteor.settings.public.elasticSearch;

      var url = s.protocol + "://" + s.host + ":" + s.port + "/" + IndexSettings.getActiveIndex() + "/" + s.attachmentsPath + "/" + id;
      return url;
    },

    /** Pluralizer. Returns singular if number is 1, otherwise plural **/
    'pluralize': function(number, singular, plural) {
      if (number === undefined || number === null) number = 0;
      if (Array.isArray(number)) number = number.length;

      if (number === 1) return singular;
      return plural;
    },
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

    'click .paginationLink': function(e) {
      e.preventDefault();

      var query = encodeURIComponent($(e.currentTarget).attr("query"));
      var page = encodeURIComponent($(e.currentTarget).attr("page"));
      location.replace('/search/'+query+'/'+page); //go f* yourself iron router
      // Router.go('/search/'+query+'/'+page);
      // Router.go('/search', [ 'alderwoodpolice', 3]);
    },

    'click .hit': function(e, tmpl) {
      ElasticSearch.get(this._id, function(err, result) {
        if (err) {
          console.error(err);
        }
        else {
          Template.detailDocumentTemplate.open(result.data);
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
          Template.detailDocumentTemplate.open(result.data, that.toString());
        }
      });
    },

    'touchdown .deviceIndicator, click .deviceIndicator': function(e, tmpl) {
      e.preventDefault();

      var targetID = $(e.currentTarget).attr("deviceid");
      if (targetID === undefined) return;

      var text = Template.detailDocumentTemplate.currentlySelectedContent();

      if (text !== undefined && text.length > 0) {
        huddle.broadcast("addtextsnippet", { target: targetID, snippet: text } );
      } else {
        //If no selection was made, show the entire document
        var doc = Session.get("detailDocument");
        if (doc === undefined) return;
        huddle.broadcast("showdocument", { target: targetID, documentID: doc._id } );
      }
    },

    'touchdown .worldDevice, click .worldDevice': function(e, tmpl) {
      e.preventDefault();

      var targetID = $(e.currentTarget).attr("deviceid");
      if (targetID === undefined) return;

      var text = Session.get("worldViewSnippetToSend");

      Template.deviceWorldView.hide();

      if (text !== undefined && text.length > 0) {
        huddle.broadcast("addtextsnippet", { target: targetID, snippet: text } );
      } else {
        //If no selection was made, show the entire document
        var doc = Session.get("detailDocument");
        if (doc === undefined) return;
        huddle.broadcast("showdocument", { target: targetID, documentID: doc._id } );
      }
    },
  });
}
