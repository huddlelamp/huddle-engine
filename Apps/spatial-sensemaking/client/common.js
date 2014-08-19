if (Meteor.isClient) {
  Template.navigation.helpers({
    "active": function(path) {
        var router = Router.current();
        if (router && router.route.name === path) {
          return "active";
        }
        return "";
      }
  });  
}