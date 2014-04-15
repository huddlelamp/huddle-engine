// Represents a point on the huddle word coordinate system. Can be converted to a point in a devices (pixel-based) coordinate system

/**
* Coordinates constructor
*
* @x x-coordinate in huddle world coordinates
* @y y-coordinate in huddle world coordinates
*/
function Coordinates(x, y) {
  this.x = parseFloat(x);
  this.y = parseFloat(y);
}

Coordinates.prototype = {
  inThisDeviceCoordinates: function(thisDeviceInfo) {
    return Coordinates.toThisDeviceCoordinates(this.x, this.y, thisDeviceInfo);
  }
}

/**
* Create world coordinates from pixel-coordinates of this device
*
* @x x-coordinates in pixels relative to the top left corner of this device's screen
* @y y-coordinates in pixels relative to the top left corner of this device's screen
*/
Coordinates.fromThisDeviceCoordinates = function(x, y, thisDeviceInfo) {
  if (!thisDeviceInfo.isThisDevice) return undefined;
  if (!DeviceInfo.thisWindowSizeWorld || !DeviceInfo.pixelToWorldRatio) return undefined;

  //Calculate the top-left and bottom-right corners of the device's screen in world coordinates
  var topLeft = new Coordinates(
    thisDeviceInfo.center.x - (DeviceInfo.thisWindowSize.x/2 * DeviceInfo.pixelToWorldRatio.x),
    thisDeviceInfo.center.y - (DeviceInfo.thisWindowSize.y/2 * DeviceInfo.pixelToWorldRatio.y)
  );
  var bottomRight = new Coordinates(
    (thisDeviceInfo.center.x + (DeviceInfo.thisWindowSize.x/2 * DeviceInfo.pixelToWorldRatio.x)),
    (thisDeviceInfo.center.y + (DeviceInfo.thisWindowSize.y/2 * DeviceInfo.pixelToWorldRatio.y))
  );

  //Calculate the location of the given device coordinates in percent relative to the screen's resolution
  var coordsInPercent = {
    'x': x/DeviceInfo.thisWindowSize.x,
    'y': y/DeviceInfo.thisWindowSize.y
  };

  //Finally, convert pixel coordinates to world coordinates
  return new Coordinates(
    topLeft.x + ((bottomRight.x-topLeft.x) * coordsInPercent.x),
    topLeft.y + ((bottomRight.y-topLeft.y) * coordsInPercent.y)
  );
};

/**
* Converts world coordinates to pixel-based coordinates in the device's coordinate system
* 0|0 will be the top left of the device
* The bottom right will be the resolution of the device
*/
Coordinates.toThisDeviceCoordinates = function(x, y, thisDeviceInfo) {
  if (!thisDeviceInfo.isThisDevice) return undefined;
  if (!DeviceInfo.thisWindowSizeWorld || !DeviceInfo.pixelToWorldRatio) return undefined;

  //Calculate the top-left corner of the device's screen in world coordinates
  var topLeft = new Coordinates(
    (thisDeviceInfo.center.x - (DeviceInfo.thisWindowSize.x/2 * DeviceInfo.pixelToWorldRatio.x)),
    (thisDeviceInfo.center.y - (DeviceInfo.thisWindowSize.y/2 * DeviceInfo.pixelToWorldRatio.y))
  );

  //Calculate the location of the given world coordinates in percent relative to the screen's top left corner
  var coordsInPercent = {
    'x': (x - topLeft.x)/DeviceInfo.thisWindowSizeWorld.x,
    'y': (y - topLeft.y)/DeviceInfo.thisWindowSizeWorld.y
  };

  //Finally, convert world coordinates to pixel coordinates
  return {
    'x': DeviceInfo.thisWindowSize.x * coordsInPercent.x,
    'y': DeviceInfo.thisWindowSize.y * coordsInPercent.y,
  };
};

Coordinates.vectorInThisDeviceCoordinates = function(coords1, coords2, thisDeviceInfo) {
  var coords1Px = coords1.inThisDeviceCoordinates(thisDeviceInfo);
  var coords2Px = coords2.inThisDeviceCoordinates(thisDeviceInfo);

  return {
    'x': coords2Px.x - coords1Px.x,
    'y': coords2Px.y - coords1Px.y,
  };
};
