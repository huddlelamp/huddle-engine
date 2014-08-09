if (Meteor.isClient) {

  Template.searchIndex.events({
    'keyup #search-query': function(e, tmpl) {
      if (e.keyCode == 13) {

        var query = tmpl.$('#search-query').val();

        Meteor.call('searchIndex', 'test', query, function(err, result) {
          if (err) {
            console.error(err);
          }
          else {
            console.log(result);
          }
        });
      }
    }
  })
}
