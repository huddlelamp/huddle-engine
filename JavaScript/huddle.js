/**
 * Helper function to use string format, e.g., as known from C#
 * var awesomeWorld = "Hello {0}! You are {1}.".format("World", "awesome");
 */
String.prototype.format = function () {
    var args = arguments;
    return this.replace(/\{\{|\}\}|\{(\d+)\}/g, function (m, n) {
        if (m == "{{") { return "{"; }
        if (m == "}}") { return "}"; }
        return args[n];
    });
};

// set web socket
window.WebSocket = window.WebSocket || window.MozWebSocket;

/**
 * An instance of Huddle handles the connection to a Huddle engine through a
 * web socket connection. It offers properties to automatically reconnect on
 * connection errors. The device will get a continues stream of proximity data if
 * a connection to Huddle engine is established.
 *
 * The data stream is received as JSON stream in the following object literal:
 *
 * {"Type":"TYPE","Data":DATA}
 *
 * TYPE := Type of data, e.g., Proximity, Digital, or Broadcast
 * DATA := Data that represents the given type of data, e.g., for Proximity
 * {
 *   Type: "TYPE",               // a string e.g., Device or Hand
 *   Identity: "IDENTITY",       // a string that represents the Huddle id
 *   Location: double[3],        // values are [0;1], Location[0] = x, Location[1] = y, Location[2] = z
 *   Orientation: double,        // value is [0;360]
 *   Distance: double,           // value is [0;1] and only set for presences in Presences property.
 *   Movement: double,           // not yet implemented
 *   Presences: Proximity[],
 *   RgbImageToDisplayRatio: {
 *                              X: double,
 *                              Y: double
 *                           },
 * }
 */
function Huddle(id, ondata) {
    this.id = id;
    this.ondata = ondata;
    this.connected = false;
    this.reconnect = false;

    this.glyph = createGlyph(this.id);
};

Huddle.prototype.glyphs = function () {
    return [
		{ id: 1, data: "0000001010001000100000000" },
	 	{ id: 2, data: "0000001010011000100000000" },
	 	{ id: 3, data: "0000000110011000100000000" },
	 	{ id: 4, data: "0000001100001100100000000" },
	 	{ id: 5, data: "0000001010011000001000000" },
	 	{ id: 6, data: "0000000100011000011000000" },
	 	{ id: 7, data: "0000001000010000111000000" },
	 	{ id: 8, data: "0000001010010000111000000" },
	 	{ id: 9, data: "0000001110001100001000000" },
	 	{ id: 10, data: "0000001010011000101000000" },
	 	{ id: 11, data: "0000001110010000001000000" },
	 	{ id: 12, data: "0000000110000100110000000" },
	 	{ id: 13, data: "0000001000010000011000000" },
	 	{ id: 14, data: "0000000110000100100000000" },
	 	{ id: 15, data: "0000001100001000001000000" },
	 	{ id: 16, data: "0000001110000100010000000" },
	 	{ id: 17, data: "0000001100001000101000000" },
	 	{ id: 18, data: "0000001100001100110000000" },
	 	{ id: 19, data: "0000000110001000100000000" },
	 	{ id: 20, data: "0000000110001100100000000" }
    ];
};

Huddle.prototype.createGlyph = function (id) {

    var data = null;
    for (var i = glyphs.length - 1; i >= 0; i--) {
        if (glyphs[i].id == id) {
            var glyphData = glyphs[i].data;

            var dim = Math.sqrt(glyphData.length);

            data = [];
            var cols = [];

            for (var j = 0; j < glyphData.length; j++) {
                var c = parseInt(glyphData[j]);

                if (j > 0 && j % dim == 0) {
                    data.push(cols);
                    cols = [];
                }

                cols.push(c);
            };
            data.push(cols);

            break;
        }
    };

    if (!data) return null;

    var dimension = 420;
    var box = 420 / (data.length + 2);

    var canvas = document.createElement('canvas');
    canvas.width = dimension;
    canvas.height = dimension;

    var ctx = canvas.getContext("2d");

    ctx.fillStyle = "white";
    ctx.fillRect(0, 0, dimension, dimension);

    for (var row = 0; row < data.length; row++) {
        for (var col = 0; col < data[row].length; col++) {
            ctx.fillStyle = data[row][col] ? "white" : "black";
            ctx.fillRect(box + (box * col), box + (box * row), box, box);
        }
    }

    var glyph = canvas.toDataURL("image/png");

    return glyph;
};

/**
 * Connects to Huddle engine at host:port.
 */
Huddle.prototype.connect = function (host, port) {
    this.host = host;
    this.port = port;

    this.doConnect();
};

/**
 * Connects to the web socket server. The host and port is given in the connection
 * function. In order to keep the web socket open it also will send an alive message
 * in an interval of 10 seconds to the server. If the connection drops it will
 * automatically re-establish connection if reconnect property is set to true.
 */
Huddle.prototype.doConnect = function () {
    var huddle = this;

    // send alive message every 10 seconds, otherwise web socket server closes
    // connection automatically
    var aliveInterval = setInterval(function () {
        if (huddle.connected) {
            var content = '"DeviceId": "{0}"'.format(huddle.id);
            huddle.send("Alive", content);
        }
    }, 10000);

    this.wsUri = "ws://{0}".format(this.host);
    if (this.port)
        this.wsUri = "{0}:{1}".format(this.wsUri, this.port);

    this.socket = new WebSocket(this.wsUri);

    this.socket.onopen = function () {
        console.log("Huddle connection open");

        if (huddle.reconnectTimeout) {
            clearTimeout(huddle.reconnectTimeout);
            huddle.reconnectTimeout = null;
        }

        huddle.connected = true;

        var content = '"DeviceId": "{0}"'.format(huddle.id);

        huddle.send("Handshake", content);
    };

    this.socket.onmessage = function (event) {
        //console.log("Huddle Message {0}".format(event));

        if (!event || !event.data) return;

        var data;
        try {
            data = JSON.parse(event.data);
        }
        catch (exception) {
            data = event.data;
        }

        if (!data || data == null) return;

        // handle pre-defined data types
        if (data.Type) {
            switch (data.Type) {
                case "Identify":
                    if (typeof (huddle.onidentify) == "function") {
                        huddle.onidentify(data.Data);//.bind(huddle);
                    }
                    return;
                case "Proximity":
                    if (typeof (huddle.onproximity) == "function") {
                        huddle.onproximity(data.Data);//.bind(huddle);
                    }
                    return;
            }
        }

        if (typeof (huddle.ondata) == "function") {
            huddle.ondata(data);
        }
    };

    this.socket.onerror = function (event) {
        console.error("Huddle Error {0}".format(event));

        huddle.connected = false;

        if (huddle.reconnect) {
            huddle.reconnectTimeout = setTimeout(function () {
                huddle.doConnect(huddle.host, huddle.port);
            }, 1000);
        }
    };

    this.socket.onclose = function (event) {
        console.log("Huddle Closed {0}".format(event));

        huddle.connected = false;
    };
};

/**
 * Closes connection to Huddle engine.
 */
Huddle.prototype.close = function () {
    if (this.socket)
        this.socket.close();
};

/**
 * Show a glyph in full screen if display is not identified in Huddle engine
 * otherwise remove glyph.
 */
Huddle.prototype.onidentifiy = function (data) {
    if (data.value) {
        var glyphContainer = jQuery('<div id="huddle-glyph-container"></div>').appendTo(jQuery('body'));
        glyphContainer.css({
            "top": "0",
            "left": "0",
            "position": "fixed",
            "background-color": "white",
            "vertical-align": "bottom",
            "margin-left": "auto",
            "margin-right": "auto",
            "width": "100%",
            "height": "100%"
        });

        var glyph = glyphContainer.append('<div id="huddle-glyph-{0}"></div>'.format(this.deviceId));
        glyph.css({
            "left": "0",
            "top": "0",
            "width": "100%",
            "height": "100%",
            "background-size": "contain",
            "background-repeat": "no-repeat",
            "background-position": "center",
            "background-image": "url('" + this.glyph + "')"
        });
    }
    else {
        jQuery('#huddle-glyph-container').remove();
    }
};

/**
 * The on proximity function is called each time a proximity data is received. This
 * function needs be overriden in the Huddle application.
 */
Huddle.prototype.onproximity = function (data) {
    // empty
}

/**
 * Broadcast message to other connected devices. The message is send to the web
 * socket server, which distributes it to all connected clients. Clients need to
 * listen explicitly to broadcast messages.
 */
Huddle.prototype.broadcast = function (message) {
    this.send("Broadcast", message);
};

/**
 * Send message to Huddle engine.
 */
Huddle.prototype.send = function (type, content) {
    var message = '{{"Type": "{0}", {1}}}'.format(type, content);
    this.socket.send(message);
}
