String.prototype.format = function () {
  var args = arguments;
  return this.replace(/\{\{|\}\}|\{(\d+)\}/g, function (m, n) {
    if (m == "{{") { return "{"; }
    if (m == "}}") { return "}"; }
    return args[n];
  });
};

window.WebSocket = window.WebSocket || window.MozWebSocket;

function Huddle(id, ondata) {
    this.id = id;
    this.ondata = ondata;
    this.connected = false;
    this.reconnect = false;
};

Huddle.prototype.connect = function(host, port) {
    this.host = host;
    this.port = port;

    this.doConnect();
};

Huddle.prototype.doConnect = function () {
    var huddle = this;

    // send alive message every 10 seconds, otherwise web socket server closes connection
    var aliveInterval = setInterval(function() {
        if (huddle.connected) {
            var content = '"DeviceId": "{0}"'.format(huddle.id);
            huddle.send("Alive", content);
        }
    }, 10000);

    this.wsUri = "ws://{0}".format(this.host);
    if (this.port)
        this.wsUri = "{0}:{1}".format(this.wsUri, this.port); 

    this.socket = new WebSocket(this.wsUri);

    this.socket.onopen = function() {
        console.log("Huddle connection open");
        
        if (huddle.reconnectTimeout)
        {
            clearTimeout(huddle.reconnectTimeout);
            huddle.reconnectTimeout = null;
        }

        huddle.connected = true;

        var content = '"DeviceId": "{0}"'.format(huddle.id);

        huddle.send("Handshake", content);
    };

    this.socket.onmessage = function(event) {
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

        if (typeof(huddle.ondata) == "function") {
            huddle.ondata(data);
        }
    };

    this.socket.onerror = function(event) {
        console.log("Huddle Error {0}".format(event));

        huddle.connected = false;

        if (huddle.reconnect)
        {
            huddle.reconnectTimeout = setTimeout(function()
            {
                huddle.doConnect(huddle.host, huddle.port);
            }, 1000);
        }
    };

    this.socket.onclose = function(event) {
        console.log("Huddle Closed {0}".format(event));

        huddle.connected = false;
    };


};

Huddle.prototype.close = function() {
    if (this.socket)
        this.socket.close();
};

Huddle.prototype.broadcast = function (message) {
    this.send("Broadcast", message);
};

Huddle.prototype.send = function (type, content) {
    var message = '{{"Type": "{0}", {1}}}'.format(type, content);
    this.socket.send(message);
}