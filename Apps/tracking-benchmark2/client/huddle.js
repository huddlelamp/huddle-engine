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

  var huddleOff = getParameterByName("huddle");
  if (huddleOff && huddleOff == "off") {
    return;
  }

  var settings = Meteor.settings.public.huddle;

  Session.setDefault("huddleIsSampling", false);
  Session.setDefault("huddleSamples", []);
  Session.setDefault("huddleFps", 0);

  var lastTimestamp = undefined;
  var lastFps = [];
  var lastFpsIdx = 0;
  var maxLastFps = 30;

  for (var i = 0; i < maxLastFps; i++) {
    lastFps[i] = 0.0;
  }

  var huddle = Huddle.client({
    name: "BenchmarkClient",
    // glyphId: "1",
  })
  .on("proximity", function(data) {

    var timestamp = Date.now();

    if (lastTimestamp) {
      var delta = timestamp - lastTimestamp;

      lastFpsIdx = ++lastFpsIdx % maxLastFps;
      lastFps[lastFpsIdx] = (1000.0 / delta);

      var meanFps = ss.mean(lastFps);

      Session.set("huddleFps", meanFps.toFixed(0));
    }
    lastTimestamp = timestamp;

    var state = data.State;
    Session.set("huddleState", state);

    var location = data.Location;
    Session.set("huddleLocation", location);

    // var angle = (Math.PI * 180) / data.Orientation ;
    // Correct angle to ease later data analysis.
    var angle = data.Orientation > 90.0 ? data.Orientation - 360.0 : data.Orientation;
    Session.set("huddleAngle", angle);

    if (!Session.get("huddleIsSampling")) {
      return;
    }

    var samples = Session.get("huddleSamples");
    samples.push({
      timestamp: timestamp,
      state: state,
      x: location[0],
      y: location[1],
      z: location[2],
      angle: angle
    });
    Session.set("huddleSamples", samples);
  });

  if (settings.port) {
    huddle.connect(settings.host, settings.port);
  }
  else {
    huddle.connect(settings.host);
  }
}
