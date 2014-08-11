if (Meteor.isClient) {

  var search = function(query) {

    var terms = query.split(" ");

    var must = "";
    var must_not = "";

    var term;
    for (var i = terms.length - 1; i >= 0; i--) {
      term = terms[i];

      if (term.charAt(0) === '-') {
        must_not += " " + term.substring(1, term.length);
      }
      else {
        must += " " + term;
      }
    }

    console.log(must);
    console.log(must_not);

    var q = {
      fields: ["file"],
      from: 0,
      size: 10,
      query: {
        bool: {
          must: [
            {
              multi_match: {
                query: must,
                fields: ["file^10", "_name"]
              }
            }
          ],
          must_not: [
            {
              multi_match: {
                query: must_not,
                fields: ["file^10", "_name"]
              }
            }
          ],
          // should: { match: { file: "Police" } },
        }
      },
      highlight: {
        fields: {
          file: {}
        }
      }
    };

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
