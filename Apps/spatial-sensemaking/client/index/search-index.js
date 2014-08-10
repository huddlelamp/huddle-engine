if (Meteor.isClient) {

  Template.searchIndex.helpers({
    hits: function() {
      return Session.get("hits") || [];
    },
  });

  Template.searchIndex.events({
    'keyup #search-query': function(e, tmpl) {
      if (e.keyCode == 13) {

        var indexName = tmpl.$('#search-index').val();
        var query = tmpl.$('#search-query').val();

        Meteor.call('searchIndex', indexName, query, function(err, result) {
          if (err) {
            console.error(err);
          }
          else {
            //console.log(result);

            var res = JSON.parse(result);

            var hits = res.hits.hits;

            console.log("hits: " + hits.length);

            var allHits = [];
            for (var i = 0; i < hits.length; i++) {

              var highlight = hits[i].highlight;

              allHits.push(highlight.file);
            }

            console.log(allHits);

            Session.set("hits", allHits);
          }
        });
      }
    }
  });
}
