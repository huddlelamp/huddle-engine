if (Meteor.isClient) {

  Deps.autorun(function() {
    Meteor.subscribe("settings-subscription");
  });

  Deps.autorun(function() {
    Meteor.subscribe("user-data");
  });

  /**
   *
   */
  Template.orbiterConnection.isServerRunning = function() {
    var serverSettings = Settings.findOne({type: "server"});

    if (serverSettings)
      return serverSettings.isRunning;
    else
      return false;
  };

  Template.orbiterConnection.orbiterPort = function() {
    var user = Meteor.user();

    if (user) {
      if (typeof(user.settings) !== "undefined") {
        var userSettings = user.settings;
        return userSettings.orbiterPort;
      }
    }

    return null;
  };

  /**
   *
   */
  Template.orbiterConnection.events({
    "click #orbiter-start": function () {
      var port = parseInt($("#orbiter-port").val());

      Meteor.call("startOrbiter", port, function(error, orbiterPort) {
        if (orbiterPort) {
          console.log("Huddle Orbiter started successfully.");

          var user = Meteor.user();
          if (user) {
            Meteor.users.update({_id: user._id}, { $set: { "settings.orbiterPort": orbiterPort } }, { multi: true } );
          }
        }
        else {
          console.log("Could not start Huddle Orbiter because " + error);
        }
      });
    },
    "click #orbiter-stop": function () {
      Meteor.call("stopOrbiter", function(error, result) {
        if (result) {
          console.log("Huddle Orbiter stopped successfully.");
        }
        else {
          console.log("Could not stop Huddle Orbiter");
        }
      });
    },
  });

  /**
   *
   */
  Template.clientConnection.events({
    "click #cmd-client-connect": function() {

      var host = $("#cmd-client-host").val();
      var port = parseInt($("#cmd-client-port").val());
      var name = $("#cmd-client-name").val();
      var reconnect = $("#cmd-client-reconnect").val();

      huddle = Huddle.client(name)
          .on("proximity", function (data) {
              console.log(data);
          })
          .on("message", function (data) {
              console.log(data);
          });
      huddle.reconnect = reconnect;
      huddle.connect(host, port);
    },
    "click #cmd-client-disconnect": function() {
      if (huddle) {
        huddle.disconnect();
      }
    },
  });
}
