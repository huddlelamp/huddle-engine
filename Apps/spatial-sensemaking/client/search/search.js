if (Meteor.isClient) {

  var search = function(query) {

    var SEARCH_FIELDS = ["file^10", "_name"];

    var terms = query.split(" ");

    var must = [];
    var must_not = [];

    var term;
    var quoteOpen;
    var not = false;

    //Some rather complex code for building queries, but here we go...
    //We walk over every (space-separated) term. Every term is converted into an
    //entry usable by elasticsearch. The entries are added to must/must_not, when:
    //    * A normal term is simply added to the must array
    //    * A term prefixed by '-' is added to the must_not arrya
    //    * If a term begins with a quote, it starts a phrase. The term is not added yet
    //    * The next term ending with a quote ends the phrase. The entire phrase is added 
    //      to either must or must_not with the type "phrase"
    for (var i = 0; i < terms.length; i++) {
      if (quoteOpen === undefined) {
        term = terms[i];
        not = false;

        //Check for negation prefix
        if (term.charAt(0) === '-') {
          term = term.substring(1, term.length);
          not = true;
        }

        //Check for opening quotes
        if (term.charAt(0) === "'" || term.charAt(0) === '"') {
          quotesOpen = term.charAt(0);
          term = term.substring(1, term.length);
        } else {
          var entry = {
            multi_match: {
              query: term,
              fields: SEARCH_FIELDS
            }
          };

          if (not)  must_not.push(entry);
          else      must.push(entry);
        }
      } else {
        //A phrase is still open, add the term to the existing phrase
        term += " " + terms[i];

        //Check if the phrase ends here
        if (term.charAt(term.length-1) === quotesOpen) {
          term = term.substring(0, term.length-1);
          quotesOpen = undefined;

          var phraseEntry = {
            multi_match: {
              query: term,
              fields: SEARCH_FIELDS,
              type: "phrase"
            }
          };

          if (not)  must_not.push(phraseEntry);
          else      must.push(phraseEntry);
        }
      }
    }

    //elasticsearch can't handle empty must/must_not blocks
    var bool = {};
    if (must.length > 0) bool.must = must;
    if (must_not.length > 0) bool.must_not = must_not;

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

        console.log(results);

        Session.set("results", results);
      }
    });
  };

  Template.searchIndex.helpers({
    results: function() {
      return Session.get("results") || [];
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
  });

  Template.searchIndex.events({
    'click #search-btn': function(e, tmpl) {
      var query = tmpl.$('#search-query').val();
      search(query);
    },

    'keyup #search-query': function(e, tmpl) {
      if (e.keyCode == 13) {
        var query = tmpl.$('#search-query').val();
        search(query);
      }
    },

    'click .hit': function(e, tmpl) {

      ElasticSearch.get(this._id, function(err, result) {
        if (err) {
          console.error(err);
        }
        else {
          var attachment = result.data;

          var source = attachment._source;
          var contentType = source._content_type;
          var file = source.file;

          // console.log(contentType);

          var content = atob(file);

          var html;
          if (contentType == "image/jpeg") {
            html = '<img src="data:' + contentType + ';base64,' + file + '" />'
          }
          else {
            html = '<div><pre>' + content + '</pre></div>';
          }

          $.fancybox(html, {
            title: "Alderwood Daily News Articles"
          });
        }
      });
    },
  });
}
