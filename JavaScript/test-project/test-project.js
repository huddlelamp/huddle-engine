if (Meteor.isClient) {

  Template.main.rendered = function() {
    console.log("rendered");

    var huddle = Huddle.client("MyHuddle")
    .on("proximity", function(data) {
      // console.log(data);
      $('#log').val(function(_, val) {
        return JSON.stringify(data) + "\r\n" + val;
      });
    })
    .connect("merkur184.inf.uni-konstanz.de", 58629);
  };
}

if (Meteor.isServer) {
  Meteor.startup(function () {
    // code to run on server at startup
  });
}
