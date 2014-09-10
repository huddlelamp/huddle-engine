Logs = new Meteor.Collection("logs");

if (Meteor.isClient) {
  Meteor.subscribe("logs");
}

if (Meteor.isServer) {
  Meteor.startup(function() {

    Meteor.publish("logs", function() {
      return Logs.find();
    });

    Logs.allow({
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
