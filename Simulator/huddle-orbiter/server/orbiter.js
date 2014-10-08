if (Meteor.isServer) {

  Meteor.startup(function() {
    var _orbiters = { };

    var startOrbiter = function(userId, port) {

      if (!_orbiters[userId]) {
        _orbiters[userId] = new HuddleOrbiter()
          .on("connect", Meteor.bindEnvironment(function(event) {
            // Clients.insert({
            //   id: event.id,
            //   userId: user._id,
            //   name: "HuddleDevice",
            //   x: 0,
            //   y: 0,
            //   z: 0,
            //   angle: 0,
            // });
          }))
          .on("disconnect", Meteor.bindEnvironment(function(event) {
            Clients.remove({ id: event.id });
          }))
          .on("Handshake", Meteor.bindEnvironment(function(event) {

            var data = event.data;

            Clients.insert({
              id: event.id,
              deviceType: data.DeviceType,
              userId: userId,
              name: data.Name,
              x: 0,
              y: 0,
              z: 0,
              angle: 0,
              ratioX: 0,
              ratioY: 0,
            });
          }));
      }

      try {

        // If port is null or empty HuddleOrbiter will start on a random port
        // which will be returned afterwards.
        port = _orbiters[userId].start(port);

        Settings.update(
          { type: "server" },
          {
            type: "server",
            userId: userId,
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
            userId: userId,
            isRunning: false
          },
          { upsert: true }
        );
        return err;
      }

      return port;
    };

    var stopOrbiter = function(userId) {

      if (typeof(userId) === "undefined") {
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

    /**
     * Update proximity and send it to client
     */
    var updateProximity = function(newClient) {
      var userId = newClient.userId;
      var id = newClient.id;
      var x = newClient.x;
      var y = newClient.y;
      var z = newClient.z;
      var angle = newClient.angle;
      var ratioX = newClient.ratioX;
      var ratioY = newClient.ratioY;

      var proximity = {
          Type: "Display",
          Identity: id,
          Location: [x, y, z],
          Orientation: angle,
          Distance: 0,
          Movement: 0,
          RgbImageToDisplayRatio: {
            X: ratioX,
            Y: ratioY,
          },
          Presences: [],
        };

      var clients = Clients.find({ userId: userId });

      clients.forEach(function(client) {
        // console.log(client._id);

        if (client._id === newClient._id) return;

        var cx = client.x;
        var cy = client.y;
        var cz = client.z;

        var dx = cx - x;
        var dy = cy - y;

        var distance = Math.sqrt(dx * dx + dy * dy);

        var radiansToDegree = function(angle) {
            return angle * (180.0 / Math.PI);
        };

        // device2 to device1 angle
        var globalAngle = radiansToDegree(Math.atan(dy / dx));

        if (dx >= 0 && dy < 0)
            globalAngle = 90 + globalAngle;
        else if (dx >= 0 && dy >= 0)
            globalAngle = 90 + globalAngle;
        else if (dx < 0 && dy >= 0)
            globalAngle = 270 + globalAngle;
        else if (dx < 0 && dy < 0)
            globalAngle = 270 + globalAngle;

        // subtract own angle
        var localAngle = globalAngle + (360 - newClient.angle); // angle -= (device1.Angle % 180);
        localAngle %= 360;

        var presence = {
          Type: "Display",
          Identity: client.id,
          Location: [cx, cy, cz],
          Orientation: localAngle,
          Distance: distance,
          Movement: 0,
          RgbImageToDisplayRatio: {
            X: client.ratioX,
            Y: client.ratioY,
          },
          Presences: [],
        };

        proximity.Presences.push(presence);
      });

      _orbiters[userId].sendToId(id, {
        Type: "Proximity",
        Data: proximity
      });
    };

    Meteor.setInterval(function() {
      // console.log("echo");

      var userId = Meteor.userId;
      var clients = Clients.find({ userId: userId });

      // console.log(clients.count());

      clients.forEach(function (client) {
        updateProximity(client, clients);
      });
    }, 1000 / 30);

    // Clients.find().observe({
    //   changed: function(newClient, oldClient) {
    //     // updateProximity(newClient);
    //   }
    // });

    UserStatus.events.on("connectionLogin", function(fields) {
      var userId = fields.userId;
      var user = getUser(userId);

      if (user) {
        var email = user.emails[0].address;

        console.log(email + " logged in at " + fields.loginTime);

        var port = undefined;
        if (typeof(user.settings) !== "undefined") {
          var userSettings = user.settings;
          port = userSettings.orbiterPort;
        }

        var newPort = startOrbiter(userId, port);

        console.log("Started HuddleOrbiter for user " + email + " on port " + newPort);

        if (newPort !== port) {
          Meteor.users.update({ _id: user._id }, { $set: {
            "settings.orbiterPort": newPort
          }}, { multi: true } );
        }
      }
    });

    UserStatus.events.on("connectionLogout", function(fields) {
      var userId = fields.userId;
      var user = getUser(userId);

      if (user) {
        var email = user.emails[0].address;

        console.log(email + " logged out at " + fields.logoutTime);

        stopOrbiter(userId);

        console.log("Stopped HuddleOrbiter of user " + email);
      }
    });
  });
}
