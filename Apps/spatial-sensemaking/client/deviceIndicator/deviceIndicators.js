Template.deviceIndicators.rendered = function() {
  $(document).ready(function() {
    $("#deviceIndicatorWrapper").width($("#deviceIndicatorWrapper").width());
    $("#deviceIndicatorWrapper").height($("#deviceIndicatorWrapper").height());
  });
};

Template.deviceIndicators.borderCSS = function() {
  var thisDevice = Session.get('thisDevice');
  if (thisDevice === undefined || !thisDevice.id) return;
  
  var info = DeviceInfo.findOne({ _id: thisDevice.id });
  if (info === undefined || info.colorDeg === undefined) return;

  var color = window.degreesToColor(info.colorDeg);

  // return 'border-color: rgb('+color.r+', '+color.g+', '+color.b+')';
  return 'border-image: radial-gradient(rgb('+color.r+', '+color.g+', '+color.b+') 25%, rgba('+color.r+', '+color.g+', '+color.b+', 0.5) 100%, rgba('+color.r+', '+color.g+', '+color.b+', 0.5)) 1%;';
};

Template.deviceIndicators.deviceBackgroundColorCSS = function() {
  var info = DeviceInfo.findOne({ _id: this.id });
  if (info === undefined || !info.colorDeg) return "";

  var color = window.degreesToColor(info.colorDeg);

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
  'touchend .deviceIndicator, mouseup .deviceIndicator': function(e, tmpl) {
    e.preventDefault();

    var targetID = $(e.currentTarget).attr("deviceid");
    if (targetID === undefined) return;

    var text = Template.detailDocumentTemplate.currentlySelectedContent();

    if (text !== undefined && text.length > 0) {
      huddle.broadcast("addtextsnippet", { target: targetID, snippet: text } );
    } else {
      //If no selection was made, show the entire document
      var doc = Session.get("detailDocument");
      if (doc === undefined) return;
      huddle.broadcast("showdocument", { target: targetID, documentID: doc._id } );
    }

    return false;
  },
});

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