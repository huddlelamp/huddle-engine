if (Meteor.isClient) {

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
    name: "MyHuddle",
    glyphId: "3",
  })
  .on("proximity", function(data) {

    var timestamp = Date.now();

    if (lastTimestamp) {
      var delta = timestamp - lastTimestamp;

      var cIdx = ++lastFpsIdx % maxLastFps;
      lastFps[cIdx] = (1000.0 / delta);

      var meanFps = ss.mean(lastFps);

      Session.set("huddleFps", meanFps.toFixed(0));
    }
    lastTimestamp = timestamp;

    var location = data.Location;
    Session.set("huddleLocation", location);

    if (!Session.get("huddleIsSampling")) {
      return;
    }

    var samples = Session.get("huddleSamples");
    samples.push({
      timestamp: timestamp,
      x: location[0],
      y: location[1],
      z: location[2]
    });
    Session.set("huddleSamples", samples);
  })
  .connect(settings.host, settings.port);
}
