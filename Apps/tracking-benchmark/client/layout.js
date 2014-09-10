if (Meteor.isClient) {

  Template.layout.helpers({

    'huddleFps': function() {
      return Session.get("huddleFps");
    },
  });
}
