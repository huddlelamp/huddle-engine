if (Meteor.isClient) {
  // counter starts at 0
  Session.setDefault("apps", []);

  Template.main.rendered = function() {

    var apps = [];

    apps.push({
      name: "Peephole",
      description: "My description for simple peephole.",
      url: "http://localhost:3000/?host=192.168.1.125&port=1948&name=Huddle",
      imageUrl: "images/peephole.jpg",
      imageAlt: "Simple Peephole",
    });

    apps.push({
      name: "Flick Browser",
      description: "My description for flick browser.",
      url: "#",
      imageUrl: "images/flick-browser.jpg",
      imageAlt: "Flick Browser",
    });

    apps.push({
      name: "Pick, Drag, & Drop",
      description: "My description for pick, drag, and drop.",
      url: "#",
      imageUrl: "images/pick-drag-and-drop.jpg",
      imageAlt: "Pick, Drag, & Drop",
    });

    apps.push({
      name: "Spatially-Aware Menus and Modes",
      description: "My description for tracking benchmark app.",
      url: "http://localhost:3000/?host=192.168.1.125&port=1948&name=Huddle",
      imageUrl: "images/spatially-aware-menus-and-modes.jpg",
      imageAlt: "Spatially-Aware Menus and Modes",
    });

    apps.push({
      name: "Tracking Benchmark",
      description: "My description for tracking benchmark app.",
      url: "http://localhost:3000/?host=192.168.1.125&port=1948&name=Huddle",
      imageUrl: "images/tracking-benchmark.png",
      imageAlt: "Tracking Benchmark",
    });

    apps.push({
      name: "Spatial Sensemaking",
      description: "My description for spatial sensemaking.",
      url: "http://localhost:3000/?host=192.168.1.125&port=1948&name=Huddle",
      imageUrl: "images/spatial-sensemaking.png",
      imageAlt: "Spatial Sensemaking",
    });

    Session.set("apps", apps);
  };

  Template.main.helpers({
    apps: function () {
      return Session.get("apps");
    },
  });
}

if (Meteor.isServer) {
  Meteor.startup(function () {
    // code to run on server at startup
  });
}
