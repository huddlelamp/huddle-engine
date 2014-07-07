if (Meteor.isClient) {
  var huddle;

  Deps.autorun(function() {
    Meteor.subscribe("clients-count");
  });

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
