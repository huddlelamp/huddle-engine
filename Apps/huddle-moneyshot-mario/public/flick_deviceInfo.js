// Represents huddle-related information about a device such as location, size and more



function DeviceInfo(identity, data, isThisDevice) {
  this.isThisDevice = (isThisDevice == undefined) ? false : isThisDevice;

  this.identity = identity;

  if (!DeviceInfo.thisWindowSize) {
    DeviceInfo.thisWindowSize = {'x': $(window).width(), 'y': $(window).height()};
  }

  this.updateData(data);
}

DeviceInfo.prototype = {
  updateData: function(data) {
    if (data == undefined) {
      this.center  = undefined;
      this.angle   = undefined;
      this.ratio   = undefined;

      return;
    }

    var locationArray = data.Location.split(",");
    this.center = new Coordinates(locationArray[0], locationArray[1]);

    this.angle = data.Orientation;
    this.ratio = data.RgbImageToDisplayRatio;

    if (!this.ratio) return;

    //Calculate and cache the device size in world coordinates of this device's screen
    if (!DeviceInfo.thisWindowSizeWorld) {
      DeviceInfo.thisWindowSizeWorld = {
        'x': 1/this.ratio.X,
        'y': 1/this.ratio.Y
      };
    }

    //Calculate and cache the pixel-to-world-ratio for this device
    //This ratio tells us how many world "pixels" are equal to one device screen pixel
    if (!DeviceInfo.pixelToWorldRatio) {
      DeviceInfo.pixelToWorldRatio = {
        'x': DeviceInfo.thisWindowSizeWorld.x/DeviceInfo.thisWindowSize.x,
        'y': DeviceInfo.thisWindowSizeWorld.y/DeviceInfo.thisWindowSize.y
      };
    }
  }, // updateData end
}; //DeviceInfo protype end
