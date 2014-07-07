if (Meteor.isClient) {

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

        // console.log(position.left);

        var orbitWidth = $("#orbit").width();
        var orbitHeight = $('#orbit').height();

        var x = position.left / orbitWidth;
        var y = position.top / orbitHeight;

        // console.log(x + "," + y);

        Clients.update({_id: id}, { $set: {
              x: x,
              y: y,
            }
          });
      },
      stop: function(event) {

      }
    });

    $(client).rotatable({
      start: function(event, ui) {

      },
      rotate: function(event, ui) {

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
    'click .cmd-client-identify': function() {

      var id = 1;

      Meteor.call("identifyClient", id, function(error, result) {
        console.log(result);
      });
    },
  });
}
