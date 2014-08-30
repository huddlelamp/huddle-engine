if (Meteor.isClient) {

  var settings = Meteor.settings.public.huddle;

  Session.setDefault("huddleIsSampling", false);
  Session.setDefault("huddleSamples", []);

  var huddle = Huddle.client("MyHuddle")
    .on("proximity", function(data) {

      var location = data.Location;
      Session.set("huddleLocation", location);

      if (!Session.get("huddleIsSampling")) {
        return;
      }

      var samples = Session.get("huddleSamples");
      samples.push({
        timestamp: Date.now(),
        x: location[0],
        y: location[1],
        z: location[2]
      });
      Session.set("huddleSamples", samples);
    })
    .connect(settings.host, settings.port);
}
