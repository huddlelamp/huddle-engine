if (Meteor.isClient) {

  var getESUrl = function() {

    var s = Meteor.settings.public;

    var protocol = s.es_protocol;
    var host = s.es_host;
    var port = s.es_port;
    var index = s.es_index;
    var attachmentPath = s.es_attachment_path;

    var url = host + ":" + port + "/" + index + "/" + attachmentPath + "/";

    if (protocol) {
      url = protocol + "://" + url;
    }
    return url;
  };

  var search = function(query) {

    var indexName = Meteor.settings.public.es_index;

    Meteor.call('searchIndex', indexName, query, function(err, result) {
      if (err) {
        console.error(err);
      }
      else {
        var res = JSON.parse(result);
        var hits = res.hits.hits;
        Session.set("hits", hits);
      }
    });
  };

  Template.searchIndex.helpers({
    hits: function() {
      return Session.get("hits") || [];
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
      var url = getESUrl();

      HTTP.get(url + this._id, {
        timeout: 30000
      }, function(err, result) {
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
