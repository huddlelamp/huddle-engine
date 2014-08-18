if (Meteor.isClient) {

  $(function() {
    var transformDeviceData = function(data) {
      var newData = {
        id: data.Identity,
        topLeft: {
          x: data.Location[0] || 0,
          y: data.Location[1] || 0,
        },
        ratio: {
          x: data.RgbImageToDisplayRatio.X || 1,
          y: data.RgbImageToDisplayRatio.Y || 1
        },
        angle: data.Orientation || 0,
      };

      newData.width = 1.0/newData.ratio.x;
      newData.height = 1.0/newData.ratio.y;

      newData.topRight = {
        x: newData.topLeft.x + newData.width, 
        y: newData.topLeft.y
      };

      newData.bottomRight = {
        x: newData.topLeft.x + newData.width, 
        y: newData.topLeft.y + newData.height
      };

      newData.bottomLeft = {
        x: newData.topLeft.x, 
        y: newData.topLeft.y + newData.height
      };

      newData.center = { 
        x: newData.topLeft.x + newData.width/2.0, 
        y: newData.topLeft.y + newData.height/2.0
      };

      return newData;
    };

    var huddle = Huddle.client("MyHuddleName")
      .on("proximity", function(data) {
        var location = data.Location;
        var x = location[0];
        var y = location[1];
        var angle = data.Orientation;

        Session.set('thisDevice', transformDeviceData(data));

        var otherDevices = [];
        data.Presences.forEach(function(presence) {
          otherDevices.push(transformDeviceData(presence));
        });
        Session.set('otherDevices', otherDevices);
      });
    huddle.connect("huddle-orbiter.proxemicinteractions.org", 47060);
  });

}