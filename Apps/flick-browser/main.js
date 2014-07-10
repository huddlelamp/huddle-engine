Objects = new Meteor.Collection("objects");

//
// CLIENT
//
if (Meteor.isClient) {
  UI.body.rendered = function() {
    //Prevent iFrames from being their own client because of Meteor injection stuff
    if (endsWith(document.URL, ".html") || endsWith(document.URL, ".com")) return;

    document.ontouchstart = function(e) {
      e.preventDefault();
    };

    var type = getParameterByName("type");

    var client = new Client(type);
    client.connect("localhost", 4711)
  };
}

//
// SERVER
//
if (Meteor.isServer) {
  Meteor.startup(function () {
    // code to run on server at startup
  });
}
