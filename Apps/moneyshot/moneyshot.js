Objects = new Meteor.Collection("objects");

if (Meteor.isClient) {

  /**
   * Get url parameter, e.g., http://localhost:3000/?id=3 -> id = 3
   *
   * @name The parameter name.
   */
  var getParameterByName = function(name) {
      name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
      var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
          results = regex.exec(location.search);
      return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
  }

  /**
   * Starts huddle client and connects to the huddle tracking engine.
   *
   * @host Host address of the huddle tracking engine.
   * @port Port of the huddle tracking engine.
   */
  var hutHutHut = function(host, port) {

    var that = this;

    var $worldCanvas = $('#world-canvas');
    var canvasWidth = $worldCanvas.width();
    var canvasHeight = $worldCanvas.height();
    var windowWidth = $(window).width();
    var windowHeight = $(window).height();

    var devicePixelRatio = window.devicePixelRatio || 1.0;
    window.peepholeMetadata = {
      canvasWidth: canvasWidth,
      canvasHeight: canvasHeight,
      scaleX: 1.0,
      scaleY: 1.0
    };
    window.canvasScaleFactor = 1;

    var transformCanvas = function(x, y, scaleX, scaleY, rotation, ratioX, ratioY) {

      var deviceCenterToDeviceLeft = ((windowWidth / ratioY) / 2);
      var deviceCenterToDeviceTop = ((windowHeight / ratioY) / 2);

      var tx = -1 * x * canvasWidth;
      var ty = -1 * y * canvasHeight;

      var txd = tx + deviceCenterToDeviceLeft;
      var tyd = ty + deviceCenterToDeviceTop;

      var transform = 'scale(' + scaleX + ',' + scaleY + ') ' +
                      'translate(' + txd + 'px,' +tyd + 'px)';

      // Translate to center, rotate around center point, and translate back
      transform += 'translate(' + (-tx) + 'px,' + (-ty) + 'px) ' +
                   'rotate(' + (-rotation) + 'deg) ' +
                   'translate(' + tx + 'px,' + ty + 'px)';

                   console.log(transform);

      $worldCanvas.css('-webkit-transform', transform);
    };

    var huddle = Huddle.client("moneyshot")
    	.on('proximity', function (data) {

  			if (debug) return;

  			if (!this.hands) {
  				this.counter = 0;
  				this.hands = [];
  			}

  			var location = data.Location;
  			var x = location[0];
  			var y = location[1];
  			var z = location[2]; // the z-axis is unknown for devices
  			var angle = data.Orientation;
  			var ratio = data.RgbImageToDisplayRatio;

  			// Indonesia Jakarta
  			//var x = 0.769;
  			//var y = 0.402;

  			var scaleX = ((ratio.X * windowWidth) / canvasWidth);
  			var scaleY = ((ratio.Y * windowHeight) / canvasHeight);

  			window.peepholeMetadata.scaleX = 1 / (ratio.Y / canvasScaleFactor);
  			window.peepholeMetadata.scaleY = 1 / (ratio.Y / canvasScaleFactor);
  			window.orientationDevice = angle;

  			transformCanvas(x, y, scaleX, scaleY, angle, ratio.X, ratio.Y);

  			this.counter++;
  			var currentHands = [];
  			var presences = data.Presences;

  			for (var i = 0; i < presences.length; i++) {
  				var presence = presences[i];

  				if (presence.Type != "Hand") continue;

  				var currentHandId = presence.Identity;

  				var eventType;

  				var handIdx = -1;
  				var length = this.hands.length;

  				for (var j = 0; j < length; j++) {
  					var hand = this.hands[j];
  					if (hand.Identity == currentHandId) {
  						handIdx = j;
  						break;
  					}
  				}

  				if (handIdx > -1) {
  					eventType = "move";
  					this.hands.splice(handIdx, 1);
  					currentHands.push(presence);
  				}
  				else {
  					eventType = "start";
  					currentHands.push(presence);
  				}

  				var location = presence.Location.split(",");
  				var x = location[0];
  				var y = location[1];
  				var z = location[2]; // the z-axis is unknown for devices

  				// the event we are triggering
  				var pEvent = $.Event("presence_" + eventType);

  				// get the touch coordinates from the touch object
  				pEvent = $.extend(
  					pEvent,
  					{
  						id: presence.Identity,
  						x: x,
  						y: y,
  						z: z, // z-value is absolute (not normalized like x and y)
  					});

  				// trigger the event in a try/catch block
  				// because we do not want any exception to stop the current function
  				try {
  					$worldCanvas.children().trigger(pEvent);
  				}
  				catch (error) {
  					console.log(error);
  				}
  			}

  			// send end event for each hand that is not present
  			for (var k = 0; k < this.hands.length; k++) {
  				var hand = this.hands[k];
  				var location = hand.Location.split(",");
  				var x = location[0];
  				var y = location[1];
  				var z = location[2]; // the z-axis is unknown for devices

  				// the event we are triggering
  				var pEvent = $.Event("presence_end");

  				// get the touch coordinates from the touch object
  				pEvent = $.extend(
  					pEvent,
  					{
  						id: hand.Identity,
  						x: x,
  						y: y,
  						z: z, // z-value is absolute (not normalized like x and y)
  					});

  				// trigger the event in a try/catch block
  				// because we do not want any exception to stop the current function
  				try {
  					$worldCanvas.children().trigger(pEvent);
  				}
  				catch (error) {
  					console.log(error);
  				}
  			}

  			this.hands = currentHands;//.splice();
  			delete currentHands;
    	});
      huddle.connect(host, port);
  }

  UI.body.rendered = function() {
    console.log("UI.body.rendered");

    // disable touch to avoid moving scroll view
    document.ontouchstart = function(e) {
      e.preventDefault();
    };

    //this.$('#world-canvas').visualizeTouches();

    window.debug = getParameterByName("debug");
    if (debug == 'undefined')
      debug = false;

    Objects.find({}).observe({
      changed: function (newDocument, oldDocument) {
        $visual = $('#' + newDocument._id);
        $visual.data('visualProperties', newDocument);
      },
    });

    /*
    $('#world-canvas').on('presence_move', function(e) {
      console.log('presence moved: ' + e.id);
    });
*/

    // Start huddle
    hutHutHut("134.34.226.168", 4711);
  };

  Template.worldCanvas.objects = function() {
    return Objects.find({});
  };

  Template.worldObject.rendered = function(e) {
    console.log("rendered world canvas: ");

      $div = this.$('#' + this.data._id);

      $div.interactive({
        visualProperties: this.data,
        peepholeMetadata: window.peepholeMetadata,
        modelUpdated: function(model) {
          //console.log('model updated' + this);

          var id = this.get(0).id;

          var $visual = $(this);

          var position = $visual.position();

          var transformOrigin = $visual.css('-webkit-transform-origin');
          var transform = $visual.css('-webkit-transform');

          var vp = $visual.data('visualProperties');

          Objects.update({_id: id}, { $set: {
              lockedBy: vp.lockedBy,
              lockedForAction: vp.lockedForAction,
              lockedData: vp.lockedData,
              x: vp.x,
              y: vp.y,
              rotation: vp.rotation,
              scale: vp.scale,
              transform: transform,
              transformOrigin: transformOrigin
          }});
        }
      });
  };

  Template.worldCanvas.events({
    'click #world-canvas2': function(e) {
      console.log('clicked canvas');

      Objects.insert({
        x: e.offsetX,
        y: e.offsetY,
        width: 200,
        height: 200,
        angle: 0.0,
        scale: 1.0
      });

      e.preventDefault();
    },
  });
}

if (Meteor.isServer) {
  Meteor.startup(function () {
    // code to run on server at startup
  });
}
