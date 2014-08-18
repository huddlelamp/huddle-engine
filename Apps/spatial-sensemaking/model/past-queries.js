PastQueries = new Meteor.Collection("past-queries");

if (Meteor.isClient) {
  Meteor.subscribe("past-queries");
}

if (Meteor.isServer) {
  Meteor.startup(function() {

    Meteor.publish("past-queries", function() {
      return PastQueries.find();
    });

    PastQueries.allow({
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
