if (Meteor.isServer) {
  // On Server
  EasySearch.config({
    host: "localhost:9200"
  });
}

// On Client and Server
Tags = new Meteor.Collection('tags');

// name is the field to search over
EasySearch.createSearchIndex('tags', {
    field : ['file'],
    collection : Tags,
    limit : 20,
    use: "elastic-search",
    query: function(searchString) {
        // // this contains all the configuration specified above
        // if (this.onlyShowDiscounts) {
        //     return { 'discount' : true, 'name' : searchString };
        // }

        console.log(searchString);

        return { name : searchString };
    }
});
