Objects = new Meteor.Collection("objects");

if (Meteor.isClient) {
  
  /**
   * Get url parameter, e.g., http://localhost:3000/?id=3 -> id = 3
   *
   * @name The parameter name.
   */  
  function getParameterByName(name) {
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
  function hutHutHut(host, port) {

    var that = this;

    var $worldCanvas = $('#world-canvas');
    var canvasWidth = $worldCanvas.width();
    var canvasHeight = $worldCanvas.height();
    var windowWidth = $(window).width();
    var windowHeight = $(window).height();

    var id = getParameterByName("id");
    console.log("Client Id: " + id);

    // set code for client id
    var glyphContainer = this.$('#glyph-container');
    var glyph = this.$('#glyph');
    glyph.css('background-image', 'url(glyphs/' + id + '.png)');

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

      $worldCanvas.css('-webkit-transform', transform);
    };

    var ignoreProximity = true;
    var huddle = new Huddle(id, function (data) {

        if (data.Type) {

            if (debug) return;

            if (!this.hands) {
              this.counter = 0;
              console.log("new hands array");
              this.hands = [];
            }

            switch (data.Type) {
                case 'Proximity':
                  //return;

                  //console.log("Received proximity data");

                  if (ignoreProximity) return;

                  var data = data.Data;

                  var location = data.Location.split(",");
                  var x = location[0];
                  var y = location[1];
                  var z = location[2]; // the z-axis is unknown for devices
                  var angle = data.Orientation;
                  var ratio = data.RgbImageToDisplayRatio;

                  // Indonesien Jakarta
                  //var x = 0.769;
                  //var y = 0.402;

                  var scaleX = ((ratio.X * windowWidth) / canvasWidth);
                  var scaleY = ((ratio.Y * windowHeight) / canvasHeight);

                  window.peepholeMetadata.scaleX = 1 / (ratio.Y / canvasScaleFactor);
                  window.peepholeMetadata.scaleY = 1 / (ratio.Y / canvasScaleFactor);

                  window.orientationDevice = angle;

                  transformCanvas(x, y, scaleX, scaleY, angle, ratio.X, ratio.Y);

                  this.counter++;
                  //if (this.counter % 10 != 0) break;

                  //console.log("THAT3: " + that);
                  var currentHands = [];

                  var presences = data.Presences;

                  //console.log("I have " + presences.length + " presences");

                  for (var i = 0; i < presences.length; i++) {
                    var presence = presences[i];

                    //console.log("Presence[" + i + "]: " + JSON.stringify(presence));

                    //console.log("Presence Type: " + presence.Type);

                    if (presence.Type != "Hand") continue;

                    var currentHandId = presence.Identity;

                    var eventType;

                    var handIdx = -1;
                    var length = this.hands.length;

                    //console.log("length of hands: " + length);

                    //console.log("Hand identity: " + presence.Identity);

                    //continue;

                    for (var j = 0; j < length; j++) {
                      var hand = this.hands[j];
                      //console.log("compare: " + hand.Identity + " == " + currentHandId);
                      if (hand.Identity == currentHandId) {
                        //console.log("found hand with id: " + j);
                        handIdx = j;
                        break;
                      }
                    }

                    //console.log("Hand identity: " + presence.Identity + " at index: " + handIdx);
                    //console.log('continue');

                    //continue;
                    
                    if (handIdx > -1) {
                      eventType = "move";
                      this.hands.splice(handIdx, 1);

                      //console.log('move hand: ' + presence.Identity);

                      currentHands.push(presence);
                    }
                    else {
                      eventType = "start";

                      //console.log('start hand: ' + presence.Identity);

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
                      //$worldCanvas.trigger(pEvent);
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

                    //console.log('end hand: ' + hand.Identity);

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
                      //$worldCanvas.trigger(pEvent);
                    }
                    catch (error) {
                      console.log(error);
                    }
                  }

                  this.hands = currentHands;//.splice();
                  delete currentHands;

                  //console.log("Hands: " + this.hands.length + ", Current Hands: " + currentHands.length);

                  break;
                case 'Digital':
                  if (data.Data.Value) {
                    glyphContainer.show();
                    ignoreProximity = true;
                  }
                  else {
                    ignoreProximity = false;
                    glyphContainer.hide();
                  }
                  break;
                case 'Broadcast':
                  
                  break;
            }
        }
        else {
            console.log("Huddle data type " + data.Type + " not supported");
        }
      });
      huddle.reconnect = true;
      huddle.connect(host, port);
  }

  /**
   * Renders an overview of the world canvas.
   */
  function showOverview() {
    var $overview = $($worldCanvas.get(0).cloneNode(true));
    //var canvasBackgroundImage = $overview.css('background-image');
    //var canvasBackgroundRepeat = $overview.css('background-repeat');
    //var canvasBackgroundSize = $overview.css('background-size');
    $overview.attr('id', 'world-canvas-copy');
    //$overview.css('-webkit-transform', 'scale(0.03,0.03)');
    $overview.css('height', '100%');
    //$overview.css('background-color', 'green');
    //$overview.css('background-image', canvasBackgroundImage);
    //$overview.css('background-repeat', canvasBackgroundRepeat);
    //$overview.css('background-size', canvasBackgroundSize);
    $('#world-canvas-overview').append($overview);
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
    hutHutHut("192.168.1.119", 4711);
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
