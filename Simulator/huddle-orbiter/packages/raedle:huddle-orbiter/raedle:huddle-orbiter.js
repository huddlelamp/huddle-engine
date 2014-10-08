HuddleOrbiter = function() {
  // A dictionary where each entry represents a single event. The key is the
  // event name. Each entry of the dictionary is an array of callbacks that
  // should be called when the event is triggered.
  this._events = {};
};

/**
 *
 */
HuddleOrbiter.prototype.start = function(port) {
  var orbiter = this;

  if (this.server) {
    console.log("Huddle Orbiter is already running on port " + this.port + ".  Stop running Huddle Orbiter first!");
    return this;
  }

  // create a future to return only when listening port is know (see listening function below).
  var Future = Npm.require('fibers/future');
  var future = new Future();

  var WebSocketServer = Npm.require('websocket').server;
  var http = Npm.require('http');

  this.server = http.createServer(function(request, response) { });

  /**
   * Listener for server.
   */
  var listening = function() {
    var address = this.server.address();
    this.port = address.port;

    console.log("HuddleOrbiter is listening on port " + this.port);

    future.return(this.port);
  }.bind(this);

  if (port) {
    this.server.listen(port, listening);
  }
  else {
    this.server.listen(listening);
  }

  // create the server
  this.wsServer = new WebSocketServer( {
      httpServer: this.server
  });

  var count = 0;
  var connected = 0;
  this._clients = {};

  /**
   * Handles incoming connection requests.
   */
  var onRequest = function (request) {
      var connection = request.accept(null, request.origin);

      ++connected;

      // Specific id for this client & increment count
      var id = count++;

      // Store the connection method so we can loop through & contact all clients
      this._clients[id] = connection;

      console.log('Connection accepted [' + id + ']. #' + connected + ' clients connected.');

      // Meteor.bindEnvironment(orbiter.onConnected());
      this._trigger("connect", {id: id});

      // Create event listener
      connection.on('message', function (message) {

          if (!message || !message.utf8Data) return;

            var msg = message.utf8Data;

            console.log("Message received from client [" + id + "]: " + msg);

            try {
                var object = JSON.parse(msg);

                if (!object) return;

                if (!object.Type) {
                  console.log("Message does not have the required Type property '" + msg + "'");
                  return;
                }

                // If a message is received then it is returned to all other clients
                // immediately. Messages will be processed on each client individually
                // and are triggered as an on message's Type event.
                if (object.Type === "Message") {
                  // Loop through all clients
                  for (var i in this._clients) {

                      // ignore sender
                      if (i == id) continue;

                      // Send a message to the client with the message
                      this._clients[i].sendUTF(msg);
                  }
                  return;
                }

                // call onData function with huddle object as this
                this._trigger(object.Type, {id: id, data: object.Data});
            }
            catch (exception) {
                console.log("Could not parse message. Invalid JSON '" + msg + "'");
            }
      }.bind(this));

      connection.on('close', function (reasonCode, description) {
          if (id && this._clients[id]) {
              delete this._clients[id];
          }
          --connected;

          console.log((new Date()) + ' Peer ' + connection.remoteAddress + ' disconnected.');

          this._trigger("disconnect", {id: id});
      }.bind(this));
  }.bind(this);

  // WebSocket server
  this.wsServer.on('request', onRequest);

  // return only when listening po
  return future.wait();
};

HuddleOrbiter.prototype.stop = function() {
  if (this.wsServer) {
    this.wsServer.shutDown();
    delete this.wsServer;
  }

  if (this.server) {
    this.server.close();
    delete this.server;

    console.log("Closed Huddle Orbiter");
  }

  delete this._clients;
};


/**
* Adds a callback for the specified event.
*
* @this HuddleOrbiter
* @param {string} event Event name, e.g., proximity, identify, message
* @param {function} callback Callback function receives object as parameter.
*/
HuddleOrbiter.prototype.on = function (event, callback) {
    this._register(event, callback);
    return this;
};

/**
* Registers the given callback function for the given event. When the event is triggered, the callback will be executed.
*
* @param {string} event The name of the event
* @param {function} callback The callback function to call when the event is triggered
*
* @memberof HuddleOrbiter
*/
HuddleOrbiter.prototype._register = function(event, callback) {

  if (typeof(event) !== "string") throw "Event name must be a string";
  if (typeof(callback) !== "function") throw "Event callback must be a function";

  if (!this._events[event]) this._events[event] = [];
  this._events[event].push(callback);
};

/**
* Triggers the given events, calling all callback functions that have registered for the event.
*
* @param {string} event The name of the event to trigger
*
* @memberof HuddleOrbiter
*/
HuddleOrbiter.prototype._trigger = function(event) {

  if (!this._events[event]) return;

  //Get all arguments passed to trigger() and remove the event
  var args = Array.prototype.slice.call(arguments);
  args.shift();

  for (var i = 0; i < this._events[event].length; i++)
  {
    var callback = this._events[event][i];
    callback.apply(null, args);
  }
};

HuddleOrbiter.prototype.send = function(object) {

  var msg = JSON.stringify(object);

  // Loop through all clients
  for (var i in this._clients) {
      // Send a message to the client with the message
      this._clients[i].sendUTF(msg);
  }
};

HuddleOrbiter.prototype.sendToId = function(id, object) {

  var msg = JSON.stringify(object);

  // Send a message to the client with the message
  this._clients[id].sendUTF(msg);
};

HuddleOrbiter.prototype.sendGlyph = function(id) {
  var msgObject = {
    Type: "Glyph",
    Id: id,
    GlyphData: "0000001010011100101000000"
  };

  this.sendToId(id, msgObject);
};

HuddleOrbiter.prototype.identifyDevice = function(id, enabled) {
  var msgObject = {
    Type: "Digital",
    Data: {
      Type: "IdentifyDevice",
      Value: enabled
    }
  };

  this.sendToId(id, msgObject);
};

HuddleOrbiter.prototype.showRed = function(id, enabled) {
  var msgObject = {
    Type: "Digital",
    Data: {
      Type: "ShowRed",
      Value: enabled
    }
  };

  this.sendToId(id, msgObject);
};

HuddleOrbiter.prototype.showColor = function(id, color, enabled) {
  var msgObject = {
    Type: "Digital",
    Data: {
      Type: "ShowColor",
      Color: color,
      Value: enabled
    }
  };

  this.sendToId(id, msgObject);
};
