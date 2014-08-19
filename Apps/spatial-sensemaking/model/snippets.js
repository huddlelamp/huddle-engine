Snippets = new Meteor.Collection("snippets");

if (Meteor.isClient) {
  Meteor.subscribe("snippets");
}

if (Meteor.isServer) {
  Meteor.startup(function() {

    Meteor.publish("snippets", function() {
      return Snippets.find();
    });

    Snippets.allow({
      insert: function () {
        return true;
      },
      update: function () {
        return true;
      },
      remove: function () {
        return true;
      },
    });
  });
}
