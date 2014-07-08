if (Meteor.isServer) {
  Meteor.startup(function () {

    var orbiter;

    Meteor.publish("clients-count", function() {
      return Clients.find();
    });

    Clients.remove({});
    Clients.find().observe({
      changed: function(newDocument, oldDocument) {
        // console.log('changed doc');

        var x = oldDocument.x;
        var y = oldDocument.y;
        var z = oldDocument.z;

        orbiter.send({
          Type: "Proximity",
          Data: {
            Type: "Display",
            Identity: 1,
            Location: [x, y, z],
            Orientation: 0,
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
      },
      identifyClient: function(id) {
        // orbiter.send({
        //   Type: "Digital",
        //   Data: {
        //     Key: "Identify",
        //     Value: true,
        //   }
        // });
        orbiter.sendGlyph();
      }
    });
  });
}
