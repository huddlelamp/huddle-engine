HuddleOrbiter = function() {
	
};

HuddleOrbiter.prototype.start = function(port) {
	if (this.server) {
		console.log("Huddle Orbiter is already running on port " + this.port + ".  Stop running Huddle Orbiter first!");
		return this;
	}

	this.port = port;

	console.log("Starting Huddle Orbiter on port " + port);

	var WebSocketServer = Npm.require('websocket').server;
	var http = Npm.require('http');

	this.server = http.createServer(function(request, response) {});
	this.server.listen(port, function() {
	    console.log((new Date()) + ' Server is listening on port ' + port);
	});

	// create the server
	this.wsServer = new WebSocketServer( {
	    httpServer: this.server
	});

	var count = 0;
	var connected = 0;
	var clients = {};

	// WebSocket server
	this.wsServer.on( 'request', function ( request ) {
	    var connection = request.accept( null, request.origin );
	    
	    ++connected;

	    // Specific id for this client & increment count
	    var id = count++;
	    
	    // Store the connection method so we can loop through & contact all clients
	    clients[id] = connection;

	    console.log('Connection accepted [' + id + ']. #' + connected + ' clients connected.');

	    // Create event listener
	    connection.on('message', function (message) {
	        console.log(message.utf8Data);
	    });

	    connection.on('close', function ( reasonCode, description ) {
	        delete clients[id];
	        --connected;

	        console.log( ( new Date() ) + ' Peer ' + connection.remoteAddress + ' disconnected.' );
	    });
	});

	setInterval(function() {
		// The string message that was sent to us
		var msgString = "{\"Type\":\"Glyph\",\"Id\":\"1\",\"GlyphData\":\"0000001010001000100000000\"}";

		// Loop through all clients
		for (var i in clients) {
		    // Send a message to the client with the message
		    clients[i].sendUTF(msgString);
		}
	}, 2500);

	return this;
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
};