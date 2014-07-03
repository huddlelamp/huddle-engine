if (Meteor.isClient) {
  Template.hello.greeting = function () {
    return "Welcome to huddle-orbiter.";
  };

  Template.hello.events({
    'click #orbiter-start': function () {

      var port = parseInt($('#orbiter-port').val());

      // template data, if any, is available in 'this'
      //if (typeof console !== 'undefined')
      //  console.log("Start huddle orbiter on port " + port);

      Meteor.call('startOrbiter', port, function(error, result) {
        if (result) {
          console.log("Huddle Orbiter started successfully.");
        }
        else {
          console.log("Could not start Huddle Orbiter because " + error);
        }
      });
    },
    'click #orbiter-stop': function () {
      Meteor.call('stopOrbiter', function(error, result) {
        if (result) {
          console.log("Huddle Orbiter stopped successfully.");
        }
        else {
          console.log("Could not stop Huddle Orbiter");
        }
      });
    }
  });
}

if (Meteor.isServer) {
  Meteor.startup(function () {
    // code to run on server at startup

    var orbiter;

    Meteor.methods({
      startOrbiter: function(port) {
        //check(port, [Number]);

        if (!orbiter) {
          orbiter = new HuddleOrbiter();
        }

        try {
          orbiter.start(port);
        }
        catch (err) {
          return err;
        }

        return true;
      },
      stopOrbiter: function() {
        if (orbiter) {
          orbiter.stop();
          return true;
        }
        return false;
      }
    });
  });
}
