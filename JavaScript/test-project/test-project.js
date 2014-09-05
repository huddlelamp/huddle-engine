if (Meteor.isClient) {

  Template.main.rendered = function() {
    var maxLines = 100;
    var lines = 0;

    var huddle = Huddle.client({
      name: "MyHuddle",
    })
    .on("devicefound", function() {
      console.log("devicefound");
    })
    .on("devicelost", function() {
      console.log("devicelost");
    })
    .on("proximity", function(data) {
      // console.log(data);
      $('#console').val(function(_, val) {

        ++lines;

        if (lines > maxLines) {
          val = val.substring(val.indexOf('\n') + 1, val.length);
        }

        var json = JSON.stringify(data);
        if (val.trim() == "") {
            return json;
        }
        else {
          return val + "\r\n" + json;
        }
      });

      $('#console').scrollTop($('#console')[0].scrollHeight);
    })
    // .connect("huddle-orbiter.proxemicinteractions.org", 58629);
    .connect("134.34.226.168");
    // .connect("localhost");
  };
}

if (Meteor.isServer) {
  Meteor.startup(function () {
    // code to run on server at startup
  });
}
