if (Meteor.isServer) {
  Meteor.startup(function () {

    var orbiter;

    Clients.remove({});
    Meteor.publish("clients-subscription", function() {
      return Clients.find();
    });

    Settings.remove({});
    Meteor.publish("settings-subscription", function() {
      return Settings.find();
    });

    Clients.find().observe({
      changed: function(newDocument, oldDocument) {
        // console.log('changed doc');

        var id = newDocument.id;
        var x = newDocument.x;
        var y = newDocument.y;
        var z = newDocument.z;
        var angle = newDocument.angle;

        console.log(id);

        orbiter.sendToId(id, {
          Type: "Proximity",
          Data: {
            Type: "Display",
            Identity: 1,
            Location: [x, y, z],
            Orientation: angle,
            Distance: 0,
            Movement: 0,
            RgbImageToDisplayRatio: {
              X: 4.0,
              Y: 4.0
            },
            Presences: [],
          }
        });
      }
    });

    Meteor.methods({
      startOrbiter: function(port) {
        //check(port, [Number]);

        if (!orbiter) {
          orbiter = new HuddleOrbiter();
            orbiter.on("connect", Meteor.bindEnvironment(function(event) {
              Clients.insert({
                id: event.id,
                name: "HuddleDevice",
                x: 0,
                y: 0,
                z: 0,
                angle: 0,
              });
            }));
            orbiter.on("disconnect", Meteor.bindEnvironment(function(event) {
              Clients.remove({id: event.id});
            }));
        }

        try {
          orbiter.start(port);

          Settings.update(
            { type: "server" },
            {
              type: "server",
              isRunning: true
            },
            { upsert: true }
          );
        }
        catch (err) {
          Settings.update(
            { type: "server" },
            {
              type: "server",
              isRunning: false
            },
            { upsert: true }
          );

          return err;
        }

        return true;
      },
      stopOrbiter: function() {
        if (orbiter) {
          orbiter.stop();

          Settings.update(
            { type: "server" },
            {
              type: "server",
              isRunning: false
            },
            { upsert: true }
          );

          return true;
        }
        return false;
      },
      identifyDevice: function(id, enabled) {
        if (enabled) {
          orbiter.sendGlyph(id, enabled);
        }
        else {
          orbiter.identifyDevice(id, enabled);
        }
      },
      showColor: function(id, color, enabled) {
        orbiter.showColor(id, color, enabled);
      }
    });
  });
}
