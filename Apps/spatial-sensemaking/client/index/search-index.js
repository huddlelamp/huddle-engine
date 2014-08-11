if (Meteor.isClient) {

  var search = function(query) {

    ElasticSearch.query({
      fields: [],
      from: 0,
      size: 20,
      query: {
        match: {
          file: query
        }
      },
      highlight: {
        fields: {
          file: {}
        }
      }
    }, function(err, result) {
      if (err) {
        console.error(err);
      }
      else {
        var hits = result.data.hits;
        Session.set("hits", hits.hits);
      }
    });
  };

  Template.searchIndex.helpers({
    hits: function() {
      return Session.get("hits") || [];
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
          var file = source.file;

          var content = atob(file);

          $.fancybox('<div><pre>' + content + '</pre></div>', {
            title: "Alderwood Daily News Articles"
          });
        }
      });
    },
  });
}
