if (Meteor.isClient) {
  Template.samplingView.helpers({
    progress: function() {
      return Session.get("progress");
    },

    timerInSeconds: function() {
      return Session.get("timer") / 1000;
    },
  });
}
