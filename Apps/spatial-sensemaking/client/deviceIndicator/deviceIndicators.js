Template.deviceIndicators.rendered = function() {
  $(document).ready(function() {
    // $("#deviceIndicatorWrapper").width($("#deviceIndicatorWrapper").width());
    // $("#deviceIndicatorWrapper").height($("#deviceIndicatorWrapper").height());
  });
};

Template.deviceIndicators.borderCSS = function() {
  var thisDevice = Session.get('thisDevice');
  if (thisDevice === undefined || !thisDevice.id) return;
  
  var colorDeg = window.getDeviceColorDeg(thisDevice.id);
  // var info = DeviceInfo.findOne({ _id: thisDevice.id });
  // if (info === undefined || info.colorDeg === undefined) return;

  var color = window.degreesToColor(colorDeg);

  // return 'border-color: rgb('+color.r+', '+color.g+', '+color.b+')';
  return 'border-image: radial-gradient(rgb('+color.r+', '+color.g+', '+color.b+') 25%, rgba('+color.r+', '+color.g+', '+color.b+', 0.5) 100%, rgba('+color.r+', '+color.g+', '+color.b+', 0.5)) 1%;';
};

Template.deviceIndicators.deviceBackgroundColorCSS = function() {
  var colorDeg = window.getDeviceColorDeg(this.id);
  // var info = DeviceInfo.findOne({ _id: this.id });
  // if (info === undefined || !info.colorDeg) return "";

  var color = window.degreesToColor(colorDeg);

  return 'background-color: rgb('+color.r+', '+color.g+', '+color.b+');';
};

Template.deviceIndicators.deviceSizeAndPositionCSS = function() {
    var thisDevice = Session.get('thisDevice');
    var otherDevice = this;
    // var otherDevice = device;

    //Get the intersection with each of the four device boundaries
    var intersectLeft   = segmentIntersect(thisDevice.center, otherDevice.center, thisDevice.topLeft,    thisDevice.bottomLeft);
    var intersectTop    = segmentIntersect(thisDevice.center, otherDevice.center, thisDevice.topLeft,    thisDevice.topRight);
    var intersectRight  = segmentIntersect(thisDevice.center, otherDevice.center, thisDevice.topRight,   thisDevice.bottomRight);
    var intersectBottom = segmentIntersect(thisDevice.center, otherDevice.center, thisDevice.bottomLeft, thisDevice.bottomRight);


    //Get the distance of each intersection and the other device
    //We need this to figure out which intersection to use
    //If an intersection is outside of the device it must be invalid      
    var leftDist   = isInsideDevice(intersectLeft,   thisDevice) ? pointDist(intersectLeft,   otherDevice.center) : 4000;
    var topDist    = isInsideDevice(intersectTop,    thisDevice) ? pointDist(intersectTop,    otherDevice.center) : 4000;
    var rightDist  = isInsideDevice(intersectRight,  thisDevice) ? pointDist(intersectRight,  otherDevice.center) : 4000;
    var bottomDist = isInsideDevice(intersectBottom, thisDevice) ? pointDist(intersectBottom, otherDevice.center) : 4000;

    //Figure out the final x/y coordinates of the device indicator
    //This is done by using the boundary intersection that is closest and valid
    //Then, one coordinate will be converted from world coords into device pixels
    //The other coordinate is simply set so that half the indicator is visible,
    //which gives us a nice half-circle
    //Furthermore, we calculate the indicator size based on the distance
    var MIN_INDICATOR_SIZE = 70;
    var CLOSENESS_INDICATOR_EXPAND = 50;
    var top;
    var right;
    var bottom;
    var left;
    var indicatorSize;
    if (leftDist <= topDist && leftDist <= rightDist && leftDist <= bottomDist) {
      indicatorSize = CLOSENESS_INDICATOR_EXPAND * (1.0-leftDist) + MIN_INDICATOR_SIZE;
      var percent = ((intersectLeft.y - thisDevice.topLeft.y) / thisDevice.height);
      top = $(window).height() * percent - (indicatorSize/2.0);
      left = -(indicatorSize/2.0);
    } else if (topDist <= leftDist && topDist <= rightDist && topDist <= bottomDist) {
      indicatorSize = CLOSENESS_INDICATOR_EXPAND * (1.0-topDist) + MIN_INDICATOR_SIZE;
      var percent = ((intersectTop.x - thisDevice.topLeft.x) / thisDevice.width);
      top = -(indicatorSize/2.0);
      left = $(window).width() * percent - (indicatorSize/2.0);
    } else if (rightDist <= leftDist && rightDist <= topDist && rightDist <= bottomDist) {
      indicatorSize = CLOSENESS_INDICATOR_EXPAND * (1.0-rightDist) + MIN_INDICATOR_SIZE;
      var percent = ((intersectRight.y - thisDevice.topLeft.y) / thisDevice.height);
      top = $(window).height() * percent - (indicatorSize/2.0);
      right = -(indicatorSize/2.0);
    } else if (bottomDist <= leftDist && bottomDist <= topDist && bottomDist <= rightDist) {
      indicatorSize = CLOSENESS_INDICATOR_EXPAND * (1.0-bottomDist) + MIN_INDICATOR_SIZE;
      var percent = ((intersectBottom.x - thisDevice.topLeft.x) / thisDevice.width);
      bottom = -(indicatorSize/2.0);
      left = $(window).width() * percent - (indicatorSize/2.0);
    }

    var css = 'width: '+indicatorSize+'px; height: '+indicatorSize+'px; ';
    if (top    !== undefined) css += 'top: '+top+'px; ';
    if (right  !== undefined) css += 'right: '+right+'px; ';
    if (bottom !== undefined) css += 'bottom: '+bottom+'px; ';
    if (left   !== undefined) css += 'left: '+left+'px; ';

    return css;
};

Template.deviceIndicators.otherDevices = function() {
  return Session.get("otherDevices") || [];
};

Template.deviceIndicators.events({
  'touchstart .deviceIndicator, mouseover .deviceIndicator': function(e) {
    // Template.deviceIndicators.highlightIndicator(e.currentTarget);
  },

  'touchend .deviceIndicator, mouseup .deviceIndicator': function(e) {
    e.preventDefault();
    Template.deviceIndicators.sendThroughIndicator(e.currentTarget);
  }
});

//
// "PUBLIC" API
// 

Template.deviceIndicators.highlightIndicator = function(indicator) {
  $(indicator).css('transform', 'scale(1.25, 1.25)');
};

Template.deviceIndicators.unhighlightIndicator = function(indicator) {
  $(indicator).css('transform', '');
};

Template.deviceIndicators.sendThroughIndicator = function(indicator, text, sourcedocID) {
  var targetID = $(indicator).attr("deviceid");
  if (targetID === undefined) return;

  if (text === undefined) {
    text = Template.detailDocumentTemplate.currentlySelectedContent();
  }

  //If no source document for the send is provided, we assume that a detial document is open and is
  //the source
  if (sourcedocID === undefined) {
    var doc = Session.get("detailDocument");
    if (doc !== undefined) sourcedocID = doc._id;
  }

  if (text !== undefined && text.length > 0) {
    //If a text selection exists, send it
    huddle.broadcast("addtextsnippet", { target: targetID, doc: sourcedocID, snippet: text } );
    pulseIndicator(indicator);
    showSendConfirmation(indicator, "The selected text was sent to the device.");

    var thisDevice = Session.get('thisDevice');
    var actionSource = (Router.current().route.name === "searchIndex") ? "detailDocument" : "snippets";
    Logs.insert({
      timestamp      : Date.now(),
      route          : Router.current().route.name,
      deviceID       : thisDevice.id,  
      actionType     : "shareSnippet",
      actionSource   : actionSource, 
      actionSubsource: "deviceIndicator",
      targetDeviceID : targetID,
      documentID     : sourcedocID,
      snippet        : text,
    });
  } else {
    //If no selection was made but a document is open, send that
    
    if (doc !== undefined) {
      huddle.broadcast("showdocument", { target: targetID, documentID: sourcedocID } );
      pulseIndicator(indicator);
      showSendConfirmation(indicator, "The document "+sourcedocID+" is displayed on the device.");

      var thisDevice = Session.get('thisDevice');
      Logs.insert({
        timestamp      : Date.now(),
        route          : Router.current().route.name,
        deviceID       : thisDevice.id,  
        actionType     : "shareDocument",
        actionSource   : "detailDocument", //must be, only source for shareDocument
        actionSubsource: "deviceIndicator",
        targetDeviceID : targetID,
        documentID     : sourcedocID,
      });
    } else {
      //If no document is open but a query result is shown, send that
      var lastQuery = Session.get('lastQuery');
      var lastQueryPage = Session.get('lastQueryPage');
      var route = Router.current().route.name;
      if (lastQuery !== undefined && route === "searchIndex") {
        huddle.broadcast("go", {
          target: targetID,
          template: "searchIndex",
          params: {
            _query: lastQuery,
            _page: lastQueryPage
          }
        });
        pulseIndicator(indicator);
        showSendConfirmation(indicator, "Search results were sent to the device.");

        console.log(Router.current());
        var thisDevice = Session.get('thisDevice');
        Logs.insert({
          timestamp      : Date.now(),
          route          : Router.current().route.name,
          deviceID       : thisDevice.id,  
          actionType     : "shareResults",
          actionSource   : "search", //must be
          actionSource   : "deviceIndicator",
          targetDeviceID : targetID,
          query          : lastQuery,
          page           : lastQueryPage
        });
      }
    }
  }
};

//
// ANIMATION STUFF
// 
function pulseIndicator(indicator) {
  $(indicator).css('transform', 'scale(1.5, 1.5)');
  Meteor.setTimeout(function() {
    $(indicator).css('transform', '');
  }, 300);
}

function showSendConfirmation(indicator, text) {
  $("#deviceIndicatorSendText").text(text);

  Meteor.setTimeout(function() {
    var eWidth = $("#deviceIndicatorSendText").width() + parseInt($("#deviceIndicatorSendText").css('padding-left')) + parseInt($("#deviceIndicatorSendText").css('padding-right'));
    var eHeight = $("#deviceIndicatorSendText").height() + parseInt($("#deviceIndicatorSendText").css('padding-top')) + parseInt($("#deviceIndicatorSendText").css('padding-bottom'));
    var indicatorSize = $(indicator).width();

    $("#deviceIndicatorSendText").css({ top: "auto", left: "auto", right: "auto", bottom: "auto"});

    var css = { opacity: 1.0 };
    if ($(indicator).css('top') !== 'auto') {
      css.top = parseInt($(indicator).css('top')) + indicatorSize/2.0 - eHeight/2.0;
      if (css.top < 100) css.top = indicatorSize/2.0 + 20;
      css.top += "px";
    }
    if ($(indicator).css('left') !== 'auto') {
      css.left = parseInt($(indicator).css('left')) + indicatorSize/2.0 - eWidth/2.0;
      if (css.left < 100) css.left = indicatorSize/2.0 + 20;
      css.left += "px";
    }
    if ($(indicator).css('right') !== 'auto') {
      css.right = parseInt($(indicator).css('right')) - indicatorSize/2.0 + eWidth/2.0;
      if (css.right < 100) css.right = indicatorSize/2.0 + 20;
      css.right += "px";
    }
    if ($(indicator).css('bottom') !== 'auto') {
      css.bottom = parseInt($(indicator).css('bottom')) - indicatorSize/2.0 + eHeight/2.0;
      if (css.bottom < 100) css.bottom = indicatorSize/2.0 + 20;
      css.bottom += "px";
    }

    $("#deviceIndicatorSendText").css(css);

    Meteor.setTimeout(function() {
      $("#deviceIndicatorSendText").css("opacity", 0);
    }, 2000);
  }, 1);
}

//
// MISC
//

/** Returns the point of intersection of two lines.
      The lines are defined by their beginning and end points **/
  function segmentIntersect(p1, p2, p3, p4) {
    if (p1 === undefined || p2 === undefined || p3 === undefined || p4 === undefined) {
      return undefined;
    }

    //Slopes
    var m1 = (p1.y-p2.y)/(p1.x-p2.x);
    var m2 = (p3.y-p4.y)/(p3.x-p4.x);

    //If a line is axis-parallel its slope is 0
    if (isNaN(m1) || !isFinite(m1)) m1 = 0;
    if (isNaN(m2) || !isFinite(m2)) m2 = 0;

    //If the two lines are parallel they don't intersect
    //If both lines have a slope of 0 they are orthogonal
    if (m1 === m2 && m1 !== 0) return undefined;

    // y = mx + c   =>   c = y - mx
    var c1 = p1.y - m1 * p1.x;
    var c2 = p3.y - m2 * p3.x;

    // y = m1 * x + c1 and y = m2 * x + c2   =>   x = (c2-c1)/(m1-m2)
    // Special case: If one of the two lines is y-parallel 
    var ix = (c2-c1)/(m1-m2);
    if ((p1.x-p2.x) === 0) ix = p1.x;
    if ((p3.x-p4.x) === 0) ix = p3.x;

    // y can now be figured out by inserting x into y = mx + c
    // Again special case: If a line is x-parallel
    var iy = m1 * ix + c1;
    if ((p1.y-p2.y) === 0) iy = p1.y;
    if ((p3.y-p4.y) === 0) iy = p3.y;

    return { x: ix, y: iy };
  }

  /** Checks if point p is inside the boundaries of the given device **/
  function isInsideDevice(p, device) {
    if (p === undefined || device === undefined) return false;

    return (
      p.y >= device.topLeft.y && 
      p.y <= device.bottomLeft.y &&
      p.x >= device.topLeft.x &&
      p.x <= device.topRight.x
    );
  }

  /** Distance between two points **/
  function pointDist(p1, p2) {
    return Math.sqrt(Math.pow(p1.x - p2.x, 2) + Math.pow(p1.y - p2.y, 2));
  }