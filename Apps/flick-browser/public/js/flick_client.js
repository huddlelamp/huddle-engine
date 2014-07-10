// Represents a meteor client device

/**
* Client constructor
*
* @deviceID The huddle ID of the client
*/
function Client(deviceID) {

  /** DeviceInfo for this device, including location and ID */
  this.deviceInfo = new DeviceInfo(deviceID, undefined, true);
  /** mode of this client (master or slave) */
  this.mode = (this.deviceInfo.identity == "master") ? 'master' : 'slave';

  /** (masters only) our connection to the huddle server */
  this.huddle = Huddle.client(this.deviceInfo.identity);
  /** (masters only) list of other detected devices as DeviceInfos */
  this.otherDevices = [];
  /** (masters only) position of the first touch on a flickable element */
  this.flickTouchStart;
  /** (masters only) position of the last two touches on a flickable element */
  this.flickLastTouches;
  /** (masters only) time the touch draggableTouchStart occured */
  this.flickTouchStartTime;
  /** (masters only) times the two touches in draggableLastTouches occured **/
  this.flickLastTouchesTime;

  /** (slaves only) Last object received by a master device and currently shown */
  this.receivedFlickedObject;

  this.proximityUpdatesPassed = 0;

  console.log("Client created with id "+this.deviceInfo.identity);
}

Client.prototype = {
  connect: function(host, port) {
    this.huddle.on('proximity', this.onReceivedHuddleProximityData.bind(this));
    this.huddle.on('message', this.onReceivedHuddleBroadcastData.bind(this));
    this.huddle.reconnect = true;
    this.huddle.connect(host, port);

    this.initWebview();
  },

  onReceivedHuddleProximityData: function(data) {
    this.deviceInfo.updateData(data);

    this.proximityUpdatesPassed++;
    if (this.proximityUpdatesPassed < 50) {
      return;
    }
    this.proximityUpdatesPassed = 0;

    var newOtherDevices = [];

    if (data.Presences) {
      for (var i=0; i<data.Presences.length; i++) {
        var presence = data.Presences[i];

        if (presence.Type == "Device" && presence.Identity != this.deviceInfo.identity) {
          var otherDevice = new DeviceInfo(presence.Identity, presence, false);
          newOtherDevices.push(otherDevice);
        }
      }
    }

    this.otherDevices = newOtherDevices;
  },

  onReceivedHuddleBroadcastData: function(data) {
    console.log("Received broadcast with target ID "+data.identity);
    console.log(data);

    if (data.identity == this.deviceInfo.identity) {
      //If the broadcast tells us to show an object, do just that
      //If another object was shown earlier, get rid of that first

      if (this.receivedFlickedObject !== undefined) {
        var that = this;
        $("#loading").show();
        this.receivedFlickedObject.animate({
          width: "0px",
          height: "0px"
        }, 500, function() {
          that.receivedFlickedObject.remove();
          showReceivedObject(that);
        });
      } else {
        $("#loading").show();
        showReceivedObject(this);
      }

      function showReceivedObject(that) {
        switch (data.type) {
          case 'video':
            that.receivedFlickedObject = $('<iframe width="560" height="315" src="'+data.url+'?autoplay=1" frameborder="0" allowfullscreen></iframe>');
            break;
          case 'image':
            that.receivedFlickedObject = $('<img src="'+data.url+'"/>');
            break;
          case 'webpage':
            that.receivedFlickedObject = $('<iframe src="'+data.url+'"/>');
            break;
        }

        that.receivedFlickedObject.width(0);
        that.receivedFlickedObject.height(0);

        $("body").append(that.receivedFlickedObject);

        $("#loading").show();
        that.receivedFlickedObject.load(function() {
          $("#loading").hide();
          that.receivedFlickedObject.animate({
            width: $(document).width()+"px",
            height: $(document).height()+"px"
          }, 500);
        });
      }
    }
  },

  initWebview: function() {

    if (this.mode == 'master') {
      console.log("Initializing master webview");

      //Set up the master iframe where our webpage is shown and create the
      //event handlers on flickable elements
      var masterPage = $('<iframe src="imdbnojs.html" id="master-page"></iframe>');
      masterPage.width($(window).width()+"px");
      masterPage.height($(window).height()+"px");
      $("body").prepend(masterPage);

      var that = this;
      masterPage.load(function() {
        masterPage.contents().find('.sendable').each(function () {
          console.log("found sendable element");

          var clone;
          $(this).on('touchstart', function(e) {
            //When a sendable element is touched, we create a clone of it
            //This clone can then be dragged around and flicked to another device, while the original element stays in place
            console.log("Cloning sendable element...");

            clone = $(this).clone();
            clone[0].style.cssText = window.getComputedStyle(this, null).cssText;
            clone.css('overflow', 'hidden');
            clone.css('position', 'fixed');
            clone.css('top', $(this).offset().top - $(window).scrollTop());
            clone.css('left', $(this).offset().left - $(window).scrollLeft());

            if (clone.css('background-color') == undefined || clone.css('background-color') == 'transparent' || clone.css('background-color') == 'rgba(0, 0, 0, 0)') {
              clone.css('background-color', '#CCC');
            }

            clone.removeClass("sendable");
            clone.addClass("flickable");
            clone.attr("flick_type", $(this).attr("flick_type"));
            clone.attr("flick_url", $(this).attr("flick_url"));

            //Deep-clone the object
            $(this).contents().each(function(index, child) {
              if (child == undefined || child == null || !child) return true;

              var childClone = $(child).clone();
              if (child.nodeType !== Node.TEXT_NODE) {
                childClone[0].style.cssText = window.getComputedStyle(child, null).cssText;
              }
              clone.append(childClone);
            });

            //clone.gesture({ drag: true });

            $("body").append(clone);

//$(this).on('touchmove', jQuery.proxy(that.onFlickableElementTouchmove, that));
  //          jQuery.event.proxyEvents(this, clone[0]);

            //$(clone).on('touchstart', jQuery.proxy(that.onFlickableElementTouchstart, that));
            //$(clone).on('touchmove', jQuery.proxy(that.onFlickableElementTouchmove, that));
            //$(clone).on('touchend', jQuery.proxy(that.onFlickableElementTouchend, that));

            try {
              e.delegateTarget = $(clone);
              that.onFlickableElementTouchstart(e);
            } catch (error) {

            }
          }); // end sendable touchstart

          $(this).on('touchmove', function(e) {
            try {
              e.delegateTarget = $(clone);
              that.onFlickableElementTouchmove(e);
            } catch (error) {
            }
          });

          $(this).on('touchend', function(e) {
            try {
              e.delegateTarget = $(clone);
              that.onFlickableElementTouchend(e);
            } catch (error) {
            }
          });
        }); // end masterPage find sendables
      }); //end masterPage load
    } else {
      console.log("Initializing slave webview");

      var loading = $('<div id="loading">Loading</div>');
      $("body").prepend(loading);
    }
  }, //end initWebview

  onFlickableElementTouchstart: function(e) {
    e.preventDefault();

    this.flickTouchStart = Coordinates.fromThisDeviceCoordinates(
      e.originalEvent.touches[0].pageX,
      e.originalEvent.touches[0].pageY,
      this.deviceInfo
    );
    this.flickTouchStartTime = Date.now();

    this.flickLastTouches = [this.flickTouchStart, undefined];
    this.flickLastTouchesTime = [this.flickTouchStartTime, undefined];
  },

  onFlickableElementTouchmove: function(e) {
    e.preventDefault();

    var thisTouchTime = Date.now();
    var thisTouch = Coordinates.fromThisDeviceCoordinates(
      e.originalEvent.changedTouches[0].pageX,
      e.originalEvent.changedTouches[0].pageY,
      this.deviceInfo
    );

    //Remember finger direction before the new touch arrived
    var lastDirection = undefined;
    if (this.flickLastTouches[0] !== undefined && this.flickLastTouches[1] !== undefined) {
      lastDirection = {
        'x': this.flickLastTouches[1].x - this.flickLastTouches[0].x,
        'y': this.flickLastTouches[1].y - this.flickLastTouches[0].y
      };
    }

    //We need to remember the last 2 touches received because the last touch causes a touchmove and a touchup. Only remembering 1 touch would result in a delta of 0 in touchend
    if (this.flickLastTouches[1]) {
      this.flickLastTouches[0] = this.flickLastTouches[1];
      this.flickLastTouchesTime[0] = this.flickLastTouchesTime[1];
    }
    this.flickLastTouches[1] = thisTouch;
    this.flickLastTouchesTime[1] = thisTouchTime;

    //Calculate the finger direction after the new touch arrived
    var newDirection = {
      'x': this.flickLastTouches[1].x - this.flickLastTouches[0].x,
      'y': this.flickLastTouches[1].y - this.flickLastTouches[0].y
    };

    var blubb = Coordinates.vectorInThisDeviceCoordinates(
      this.flickLastTouches[0],
      this.flickLastTouches[1],
      this.deviceInfo
    );

    var draggedElement = $(e.delegateTarget);
    draggedElement.css('top', (parseFloat(draggedElement.css('top')) + parseFloat(blubb.y))+"px");
    draggedElement.css('left', (parseFloat(draggedElement.css('left')) + parseFloat(blubb.x))+"px");

    //Determine if the finger direction changed - if so this counts like a touchstart
    //This way we can still detect a flick even if the user drags the element around for a while and then flicks without lifting his finger
    if (lastDirection !== undefined
      && ((lastDirection.x * newDirection.x) < 0 || (lastDirection.y * newDirection.y) < 0)) {
      console.log("Direction change detected");
      this.flickTouchStart = thisTouch;
      this.flickLastTouches = [thisTouch, undefined];

      this.flickTouchStartTime = thisTouchTime;
      this.flickLastTouchesTime = [thisTouchTime, undefined];
    }
  }, // on flickable element touch move end

  onFlickableElementTouchend: function(e) {
    e.preventDefault();

    var flickedElement = $(e.delegateTarget);

    if (this.flickLastTouches[0] == undefined || this.flickLastTouches[1] == undefined) {
      flickedElement.hide();
      return;
    }

    var lastTouchTime = Date.now();
    var lastTouch = Coordinates.fromThisDeviceCoordinates(
      e.originalEvent.changedTouches[0].pageX,
      e.originalEvent.changedTouches[0].pageY,
      this.deviceInfo
    );

    //Determine the speed of the touch at the end by calculating the vector between the last two received touches
    //If the speed is above 10 pixels we consider that a flick
    var lastTouchVectorPx = Coordinates.vectorInThisDeviceCoordinates(
      this.flickLastTouches[0],
      this.flickLastTouches[1],
      this.deviceInfo);

    if (lastTouchVectorPx.x > 20 || lastTouchVectorPx.y > 20
      || lastTouchVectorPx.x < -20 || lastTouchVectorPx.y < -20) {
      console.log("Detected flick");

      //Create a vector that represents the entire flick
      var flickVector = {
        'x': lastTouch.x - this.flickTouchStart.x,
        'y': lastTouch.y - this.flickTouchStart.y
      };

      //Create a distant point where the flick would have ended
      //The 1000 is a dirty hack to make sure there is enough space between flick start and this point
      var distantFlickEnd = {
        'x': this.flickTouchStart.x + 1000*flickVector.x,
        'y': this.flickTouchStart.y + 1000*flickVector.y
      };

      //Check which other device is closest
      //Check which of the other devices in our world is closest to the last finger direction
      var closestDevice = undefined;
      console.log(this.otherDevices.length+" other devices");
      for (var i=0; i<this.otherDevices.length; i++) {
        console.log("Checking device...");
        var otherDevice = this.otherDevices[i];

        var distance = distToSegment(otherDevice.center, this.flickTouchStart, distantFlickEnd);
        console.log("Distance is "+distance);

        if (!closestDevice || closestDevice.distance > distance) {
          closestDevice = {'device': otherDevice, 'distance': distance};
        }
      }

      if (closestDevice && closestDevice.distance < 0.2) {
        //Get some values we need in pixel coordinates
        var flickVectorPx = Coordinates.vectorInThisDeviceCoordinates(
          this.flickTouchStart,
          lastTouch,
          this.deviceInfo
        );
        var flickDuration = lastTouchTime - this.flickStartTouchTime;

        var currentPositionX = parseFloat(flickedElement.css('left'));
        var currentPositionY = parseFloat(flickedElement.css('top'));

        //Check where the element will cross a device border if it follows the flick vector
        var howOften = Math.min(
          Math.abs(currentPositionX/lastTouchVectorPx.x),
          Math.abs(currentPositionY/lastTouchVectorPx.y)
        );
        var neededTranslationX = Math.round(howOften * lastTouchVectorPx.x);
        var neededTranslationY = Math.round(howOften * lastTouchVectorPx.y);

        var newPositionX = currentPositionX + neededTranslationX;
        var newPositionY = currentPositionY + neededTranslationY;

        //Check which device border the element will leave the device and adjust our
        //translation so the object is not visible anymore after the translation
        var translationAdjustX = 0;
        var translationAdjustY = 0;
        if (newPositionX <= 0) {
          translationAdjustX = -flickedElement.width();
        } else if (newPositionY <= 0) {
          translationAdjustY = -flickedElement.height();
        } else if (newPositionX >= $(window).width()) {
          translationAdjustX = flickedElement.width();
        } else if (newPositionY >= $(window).height()) {
          translationAdjustY = flickedElement.height();
        }
        //remainingTranslationX += translationAdjustX;
        //remainingTranslationY += translationAdjustY;
        //newTranslateX += translationAdjustX;
        //newTranslateY += translationAdjustY;
        newPositionX += translationAdjustX;
        newPositionY += translationAdjustY;

        //Determine the speed in which we need to animate the object
        //We try to adjust to the flick speed to get a fluid motion
        //var lastTouchDuration = this.flickLastTouchesTime[1] - this.flickLastTouchesTime[0];
        //var flickSpeed = Math.max(Math.abs(flickVectorPx.x/flickDuration), Math.abs(flickVectorPx.y/flickDuration));
        //flickSpeed *= 10; //convert to ms
        var flickSpeed = lineLength(0, 0, lastTouchVectorPx.x, lastTouchVectorPx.y);
        var translationLength = lineLength(
          currentPositionX,
          currentPositionY,
          newPositionX,
          newPositionY
        );

        // console.log("Starting at "+currentTranslateX+", "+currentTranslateY);
        // console.log("Going to "+newTranslateX+", "+newTranslateY);
        // console.log("Vector is "+JSON.stringify(flickVectorPx));

        var moveOutAnimationLength = flickSpeed*2;
        //var moveOutAnimationLength = (translationLength/flickSpeed);
        //moveOutAnimationLength = 500;

        flickedElement.animate({
          left: newPositionX,
          top: newPositionY
        }, moveOutAnimationLength);

        window.setTimeout(jQuery.proxy(function() {
          console.log("Sending to device:")
          console.log(closestDevice);
          this.huddle.broadcast('"identity": "'+closestDevice.device.identity+'", "type": "'+flickedElement.attr("flick_type")+'", "url": "'+flickedElement.attr("flick_url")+'"');
          flickedElement.hide();
        }, this), moveOutAnimationLength);
      } else {
        flickedElement.hide();
      }
    } else {
      flickedElement.hide();
    }
  }, //on flickable element touch end end
}; //end client prototype
