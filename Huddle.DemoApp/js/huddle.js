/**
 * Helper function to use string format, e.g., as known from C#
 * var awesomeWorld = "Hello {0}! You are {1}.".format("World", "awesome");
 *
 * TODO Enclose the format prototype function in Huddle JavaScript API.
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
 *
 * @class
 * @author Roman Rädle [firstname.lastname@outlook.com] replace 'ä' with 'ae'
 * @requires jQuery
 * @param {int} Device id.
 */
function Huddle(id) {
    this.id = id;
    this.connected = false;
    this.reconnect = false;

    // TODO Synchronize glyph data with Huddle Engine instead of defining it explicitly in the JavaScript API. It might be more appropriate if Huddle Engine assigns ids to devices.
    this.glyphs = [
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

    this.glyph = this.createGlyph(this.id);

    // DEBUG
    this.identify({
        Value: true
    });
};

/**
 * Creates a glyph using the device id and returns the glyph as a base64
 * encoded image/png.
 *
 * @this Huddle
 * @param {int} Device id.
 * @returns {string} Glyph as base64 encoded image/png.
 *
 * TODO Make this function private!
 */
Huddle.prototype.createGlyph = function (id) {

    var data = null;
    for (var i = this.glyphs.length - 1; i >= 0; i--) {
        if (this.glyphs[i].id == id) {
            var glyphData = this.glyphs[i].data;

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
    }

    if (!data) return null;

    // for some glyph dimensions the glyph blocks are not connected tightly, therefore the
    // rendered glyph image needs to be tested after changing dimension 
    var dimension = 840;
    var box = dimension / (data.length + 2);

    var canvas = document.createElement('canvas');
    canvas.width = dimension;
    canvas.height = dimension;

    var ctx = canvas.getContext("2d");

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
 *
 * @this Huddle
 * @param {string} host Web socket server host name.
 * @param {int} [port=4711] Web socket server port.
 */
Huddle.prototype.connect = function (host, port) {
    this.host = host;
    this.port = typeof port !== 'undefined' ? port : 4711;;

    this.doConnect();
};

/**
 * Connects to the web socket server. The host and port is given in the connection
 * function. In order to keep the web socket open it also will send an alive message
 * in an interval of 10 seconds to the server. If the connection drops it will
 * automatically re-establish connection if reconnect property is set to true.
 *
 * @this Huddle
 *
 * TODO Make this function private!
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

        if (!data) return;

        // call onData function with huddle object as this
        huddle.onData.call(huddle, data);
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
 *
 * @this Huddle
 */
Huddle.prototype.close = function () {
    if (this.socket)
        this.socket.close();
};

/**
 * Receives the raw data stream from Huddle engine.
 *
 * @this Huddle
 * @param {Object} data The proximity data as object literal.
 *
 * TODO Make this function private!
 */
Huddle.prototype.onData = function (data) {

    // handle pre-defined data types
    if (data.Type) {
        switch (data.Type) {
            case "Digital":
                if (typeof (this.identify) == "function") {
                    this.identify(data.Data);
                }
                return;
            case "Proximity":
                if (typeof (this.updateProximity) == "function") {
                    this.updateProximity(data.Data);
                }
                return;
            case "Broadcast":
                if (typeof (this.message) == "function") {
                    this.message(data.Data);
                }
                return;
        }
    }

    // call undefined data function if data could not be handled by default handlers
    if (typeof (this.undefinedData) == "function") {
        this.undefinedData(data);
    }
};

/**
 * Show a glyph in full screen if display is not identified in Huddle engine
 * otherwise remove glyph.
 *
 * @this Huddle
 * @param {Object} data The digital data as object literal.
 */
Huddle.prototype.identify = function (data) {
    if (data.Value) {

        // do not add a glyph container if it already exists
        if (jQuery('#huddle-glyph-container').length)
            return;

        var glyphContainer = jQuery('<div id="huddle-glyph-container"></div>').appendTo(jQuery('body'));
        glyphContainer.css({
            'top': '0',
            'left': '0',
            'position': 'fixed',
            'background-color': 'white',
            'vertical-align': 'bottom',
            'margin-left': 'auto',
            'margin-right': 'auto',
            'width': '100%',
            'height': '100%'
        });

        var glyph = glyphContainer.append('<div id="huddle-glyph-{0}"></div>'.format(this.id));
        glyph.css({
            'left': '0',
            'top': '0',
            'width': '100%',
            'height': '100%',
            'background-size': 'contain',
            'background-repeat': 'no-repeat',
            'background-position': 'center',
            'background-image': "url('" + this.glyph + "')"
        });
    }
    else {
        jQuery('#huddle-glyph-container').remove();
    }
};

/**
 * The update proximity function is called each time a proximity data is received. This
 * function needs be overriden in the Huddle application.
 *
 * @this Huddle
 * @param {Object} data The proximity data as object literal.
 */
Huddle.prototype.updateProximity = function (data) {
    // empty
};

/**
 * Called if broadcast message is of undefined data type. This function can be overriden if custom
 * data types are needed.
 *
 * @this Huddle
 * @param {Object} data The undefined data as object literal.
 */
Huddle.prototype.message = function (data) {
    // empty
};

/**
 * Called if data is of undefined data type. This function can be overriden if custom
 * data types are needed.
 *
 * @this Huddle
 * @param {Object} data The undefined data as object literal.
 */
Huddle.prototype.undefinedData = function (data) {
    // empty
};

/**
 * Broadcast message to other connected devices. The message is send to the web
 * socket server, which distributes it to all connected clients. Clients need to
 * listen explicitly to broadcast messages.
 *
 * @this Huddle
 * @param message Message content.
 */
Huddle.prototype.broadcast = function (message) {
    this.send("Broadcast", message);
};

/**
 * Send message to Huddle engine.
 *
 * @this Huddle
 * @param {string} type Message type.
 * @param {string} content Message content.
 */
Huddle.prototype.send = function (type, content) {
    var message = '{{"Type": "{0}", {1}}}'.format(type, content);
    this.socket.send(message);
};
