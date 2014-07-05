// client: declare collection to hold count object
Clients = new Meteor.Collection("clients");

if (Meteor.isClient) {

  Deps.autorun(function() {
    Meteor.subscribe("clients-count");
  });

  Template.hello.connectedClients = function () {
    return Clients.find().count();
  };

  Template.hello.clients = function() {
    return Clients.find();
  }

  Template.hello.greetings = function () {
    return "Hallo Johannes";
  };

  Template.hello.rendered = function() {
    var huddle = Huddle.client(3)
        .on("proximity", function (data) {
            //console.log(data);
        })
        .on("message", function (data) {
            console.log(data);
        });
        huddle.reconnect = true;
        huddle.connect("192.168.2.110", 4711);
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

  Meteor.publish("clients-count", function() {
    //orbiter.onConnected = function() {
      return Clients.find();
    //};
  });

  Clients.remove({});

    var orbiter;

    Meteor.methods({
      startOrbiter: function(port) {
        //check(port, [Number]);

        if (!orbiter) {
          orbiter = new HuddleOrbiter();
            orbiter.on("connect", Meteor.bindEnvironment(function(event) {
              Clients.insert({
                id: event.id,
                name: "HuddleDevice"
              });
            }));
            orbiter.on("disconnect", Meteor.bindEnvironment(function(event) {
              Clients.remove({id: event.id});
            }));
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
