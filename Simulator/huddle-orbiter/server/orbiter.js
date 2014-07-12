if (Meteor.isServer) {

  Meteor.startup(function() {
    var _orbiters = { };

    var startOrbiter = function(port) {

      var user = Meteor.user();
      if (typeof(user.settings) !== 'undefined') {
        var userSettings = user.settings;
        port = userSettings.orbiterPort;
      }

      if (!_orbiters[user._id]) {
        _orbiters[user._id] = new HuddleOrbiter()
          .on("connect", Meteor.bindEnvironment(function(event) {
            Clients.insert({
              id: event.id,
              userId: user._id,
              name: "HuddleDevice",
              x: 0,
              y: 0,
              z: 0,
              angle: 0,
            });
          }))
          .on("disconnect", Meteor.bindEnvironment(function(event) {
            Clients.remove({id: event.id});
          }));
      }

      try {

        // If port is null or empty HuddleOrbiter will start on a random port
        // which will be returned afterwards.
        port = _orbiters[user._id].start(port);

        if (user) {
          Meteor.users.update({_id: user._id}, { $set: { 'settings.orbiterPort': port } }, { multi: true } );
        }

        Settings.update(
          { type: "server" },
          {
            type: "server",
            userId: user._id,
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
            userId: user._id,
            isRunning: false
          },
          { upsert: true }
        );
        return err;
      }

      return port;
    };

    var stopOrbiter = function(userId) {

      if (typeof(userId) === 'undefined') {
        // current user
        var user = Meteor.user();
        userId = user._id;
      }

      if (_orbiters[userId]) {
        _orbiters[userId].stop();

        _orbiters[userId] = undefined;

        Settings.update(
          { type: "server" },
          {
            type: "server",
            userId: userId,
            isRunning: false
          },
          { upsert: true }
        );

        return true;
      }
      return false;
    };

    Meteor.methods({
      startOrbiter: startOrbiter,
      stopOrbiter: stopOrbiter,
      identifyDevice: function(id, enabled) {
        var user = Meteor.user();
        if (enabled) {
          _orbiters[user._id].sendGlyph(id, enabled);
        }
        else {
          _orbiters[user._id].identifyDevice(id, enabled);
        }
      },
      showColor: function(id, color, enabled) {
        var user = Meteor.user();
        _orbiters[user._id].showColor(id, color, enabled);
      }
    });

    Clients.find().observe({
      changed: function(newDocument, oldDocument) {
        var userId = newDocument.userId;
        var id = newDocument.id;
        var x = newDocument.x;
        var y = newDocument.y;
        var z = newDocument.z;
        var angle = newDocument.angle;

        // console.log(userId);

        _orbiters[userId].sendToId(id, {
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

    Hooks.onLoggedIn = function(userId) {
      console.log('User ' + userId + ' logged in.');

      startOrbiter();
    };

    Hooks.onLoggedOut = function(userId) {
      console.log('User ' + userId + ' logged out.');

      stopOrbiter(userId);
    };

    Hooks.treatCloseAsLogout = true;
  });
}
