if (Meteor.isClient) {

  Deps.autorun(function() {
    Meteor.subscribe("settings-subscription");
  });

  /**
   *
   */
  Template.settings.connectedClients = function () {
    return Clients.find().count();
  };

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

  /**
   *
   */
  Template.settings.events({
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
    },
    'click #cmd-client-connect': function() {

      var host = $('#cmd-client-host').val();
      var port = parseInt($('#cmd-client-port').val());
      var name = $('#cmd-client-name').val();
      var reconnect = $('#cmd-client-reconnect').val();

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
    'click #cmd-client-disconnect': function() {
      if (huddle) {
        huddle.disconnect();
      }
    },
  });
}
