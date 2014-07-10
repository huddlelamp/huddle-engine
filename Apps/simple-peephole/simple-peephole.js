if (Meteor.isClient) {
  Template.main.rendered = function() {
    $(function() {

      // disable touch to avoid moving scroll view
      document.ontouchstart = function(e) {
        e.preventDefault();
      };

      var windowWidth = $(window).width();
      var windowHeight = $(window).height();

      jQuery.fn.rotate = function (degrees) {
        $(this).css({
          '-webkit-transform': 'rotate(' + degrees + 'deg)',
          '-moz-transform': 'rotate(' + degrees + 'deg)',
          '-ms-transform': 'rotate(' + degrees + 'deg)',
          'transform': 'rotate(' + degrees + 'deg)'
        });
      };

      var huddle = Huddle.client("simple-peephole")
        .on("proximity", function(data) {
          var location = data.Location;
          var x = location[0];
          var y = location[1];
          var angle = data.Orientation;

          data.Presences.forEach(function(presence) {
              var id2 = presence.Identity;
              var location2 = presence.Location.split(",");
              var x2 = location2[0];
              var y2 = location2[1];

              var $presence = $('#presence-' + id2);

              if (!$presence.length) {
                  $('<div id="presence-' + id2 + '" class="huddle-presence">' + id2 + '</div>').appendTo($('#presences-container'));
              } else {
                  $presence.html(JSON.stringify(presence));

                  $presence.css('height', ($(window).height()) + "px");
                  $presence.css('top', "-" + ($(window).height() / 2) + "px");
                  $presence.rotate(presence.Orientation);
              }
          });

          var width = $('#peephole-canvas').width(); // 3840
          var height = $('#peephole-canvas').height(); // 2160

          var offsetLeft = width * x - (windowWidth / 2);
          var offsetTop = height * y - (windowHeight / 2);

          $('#peephole-canvas').css('left', -offsetLeft + "px");
          $('#peephole-canvas').css('top', -offsetTop + "px");
        });
        huddle.reconnect = true;
        huddle.connect("localhost", 4711);
    });
  };
}

if (Meteor.isServer) {
  Meteor.startup(function () {
    // code to run on server at startup
  });
}
