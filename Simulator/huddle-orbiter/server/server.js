if (Meteor.isServer) {
  Meteor.startup(function () {

    // if (Meteor.users.findOne("7aq2jG6QFhwLFE8Jb"))
    //     Roles.addUsersToRoles("7aq2jG6QFhwLFE8Jb", ['admin']);

    //// create a couple of roles if they don't already exist (THESE ARE NOT NEEDED -- just for the demo)
    // if(!Meteor.roles.findOne({name: "secret"}))
    //     Roles.createRole("secret");
    //
    // if(!Meteor.roles.findOne({name: "double-secret"}))
    //     Roles.createRole("double-secret");

    Meteor.publish("user-data", function () {
      if (this.userId) {
        return Meteor.users.find({_id: this.userId},
                                 {fields: {'settings': 1}});
      }
      else {
        this.ready();
      }
    });

    var orbiter;

    Clients.remove({});
    Meteor.publish("clients-subscription", function() {
      return Clients.find();
    });

    Settings.remove({});
    Meteor.publish("settings-subscription", function() {
      return Settings.find();
    });

    Clients.allow({
      insert: function (userId, doc) {
        // the user must be logged in, and the document must be owned by the user
        return true;
      },
      update: function (userId, doc, fields, modifier) {
        // can only change your own documents
        return true;
      },
      remove: function (userId, doc) {
        // can only remove your own documents
        return true;
      },
    });

    Settings.allow({
      insert: function (userId, doc) {
        // the user must be logged in, and the document must be owned by the user
        return true;
      },
      update: function (userId, doc, fields, modifier) {
        // can only change your own documents
        return true;
      },
      remove: function (userId, doc) {
        // can only remove your own documents
        return true;
      },
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

          // If port is null or empty HuddleOrbiter will start on a random port
          // which will be returned afterwards.
          port = orbiter.start(port);

          console.log("Server PORT: " + port);

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

        return port;
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

  Meteor.users.allow({
    update: function(userId, user, fields, modifier) {
      if (userId != user._id) return false;

      if (fields.length == 1 && fields[0] == "settings") return true;

      return false;
    }
  });
}
