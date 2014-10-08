if (Meteor.isClient) {

  Template.display.helpers({
    isDeveloperUser: function() {
      return Roles.userIsInRole(Meteor.user(), ["developer","admin"]);
    }
  });

  var matrixBack = function(tr) {
    var values = tr.split("(")[1].split(")")[0].split(",");
    var a = values[0];
    var b = values[1];
    var c = values[2];
    var d = values[3];

    var scale = Math.sqrt(a*a + b*b);

    // arc sin, convert from radians to degrees, round
    var sin = b/scale;
    // next line works for 30deg but not 130deg (returns 50);
    // var angle = Math.round(Math.asin(sin) * (180/Math.PI));
    return Math.round(Math.atan2(b, a) * (180/Math.PI));
  };

  /**
   *
   */
  Template.display.rendered = function() {
    var client = this.find(".orbit-client");

    $(client).draggable({
      containment: "parent",
      start: function(event) {

      },
      drag: function(event) {

        var id = this.id;
        var $this = $(this);
        var position = $this.position();
        var width = $this.width();
        var height = $this.height();

        var $orbit = $("#orbit");
        var orbitWidth = $orbit.width();
        var orbitHeight = $orbit.height();

        var x = (position.left + (width / 2)) / orbitWidth;
        var y = (position.top + (height / 2)) / orbitHeight;

        // set bounds for x and y between 0 and 1.
        x = x.clamp(0.0, 1.0);
        y = y.clamp(0.0, 1.0);

        var ratioX = orbitWidth / width;
        var ratioY = orbitHeight / height;

        Clients.update({_id: id}, { $set: {
              x: x,
              y: y,
              ratioX: ratioX,
              ratioY: ratioY,
            }
          });
      },
      stop: function(event) {

      }
    });
    $(client).css("position", "absolute");

    $(client).rotatable({
      start: function(event, ui) {

      },
      rotate: function(event, ui) {

        var id = this.id;
        var $this = $(this);
        var transform = $this.css("-webkit-transform");

        // console.log(transform);

        if (!transform || transform === "none") return;

        var angle = matrixBack(transform);

        Clients.update({_id: id}, { $set: {
              angle: angle
            }
          });
      },
      stop: function(event, ui) {

      }
    });
  };

  /**
   *
   */
  Template.orbit.clients = function() {
    return Clients.find();
  }

  /**
   *
   */
  Template.display.events({
    "click .cmd-client-identify-on": function() {
      var id = this.id;
      Meteor.call("identifyDevice", id, true, function(error, result) {
        console.log(result);
      });
    },
    "click .cmd-client-identify-off": function() {
      var id = this.id;
      Meteor.call("identifyDevice", id, false, function(error, result) {
        console.log(result);
      });
    },
    "click .cmd-client-showred-on": function() {
      var id = this.id;
      Meteor.call("showColor", id, "rgb(255,0,0)", true, function(error, result) {
        console.log(result);
      });
    },
    "click .cmd-client-showred-off": function() {
      var id = this.id;
      Meteor.call("showColor", id, "", false, function(error, result) {
        console.log(result);
      });
    }
  });
}
