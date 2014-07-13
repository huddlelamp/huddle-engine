if (Meteor.isClient) {

  /**
   * Adds an additional function rotate as jQuery function.
   */
  $.fn.rotate = function (degrees) {
    $(this).css({
      '-webkit-transform': 'rotate(' + degrees + 'deg)',
      '-moz-transform': 'rotate(' + degrees + 'deg)',
      '-ms-transform': 'rotate(' + degrees + 'deg)',
      'transform': 'rotate(' + degrees + 'deg)'
    });
  };

  /**
   * Render the main content.
   */
  Template.main.rendered = function() {

    var windowWidth = $(window).width();
    var windowHeight = $(window).height();

    /**
     * Move the canvas according to the current proximity.
     *
     * @param {Object} proximity Proximity data that moves the canvas.
     */
    var moveCanvas = function(proximity) {
      var location = proximity.Location;
      var x = location[0];
      var y = location[1];
      var angle = proximity.Orientation;

      var width = $('#peephole-canvas').width(); // 3840
      var height = $('#peephole-canvas').height(); // 2160

      var offsetLeft = width * x - (windowWidth / 2);
      var offsetTop = height * y - (windowHeight / 2);

      $('#peephole-canvas').css('left', -offsetLeft + "px");
      $('#peephole-canvas').css('top', -offsetTop + "px");
    };

    /**
     * Render presence location indicators for other presences. An indicator is
     * a blue line that points in the direction of the presence.
     *
     * @param {Object} presences Other presences.
     */
    var renderPresences = function(presences) {
      presences.forEach(function(presence) {
          var id2 = presence.Identity;
          var location2 = presence.Location;
          var x2 = location2[0];
          var y2 = location2[1];

          var $presence = $('#presence-' + id2);

          if (!$presence.length) {
              $('<div id="presence-' + id2 + '" class="huddle-presence"></div>').appendTo($('#presences-container'));
          }
          else {
              // $presence.html(JSON.stringify(presence));

              var containerWidth = $('#presences-container').width();
              var containerHeight = $('#presences-container').height();

              var presenceWidth = $presence.width();

              var presenceLeft = (containerWidth / 2) - (presenceWidth / 2);
              var presenceTop = (containerHeight / 2);

              // $presence.css('height', presenceHeight + "px");
              $presence.css('left', presenceLeft + "px");
              $presence.css('top', presenceTop + "px");
              $presence.css('height', $(window).width() + "px");
              $presence.rotate(presence.Orientation - 180);
          }
      });

      // TODO Remove presence location indicators for presences that are not available anyome.
    };

    /**
     * Connects Huddle client to host and port with the given name.
     *
     * @param {string} host Huddle Engine host.
     * @param {int} port Huddle Engine port.
     * @param {string} [name] Huddle client's name.
     */
    var hutHutHut = function(host, port, name) {
      var huddle = Huddle.client(name)
        .on("proximity", function(data) {

          // move canvas
          moveCanvas(data);

          // render presence indicators
          renderPresences(data.Presences);
        });
        huddle.connect(host, port);
    };
  }

  /**
   * Render the connection dialog.
   */
  Template.connectionDialog.rendered = function() {
    $('#connection-dialog').modal({
      backdrop: false,
      keyboard: true,
      show: true
    });
    $('#connection-dialog').on('hidden.bs.modal', function (e) {
      var host = $('#client-host').val();
      var port = parseInt($('#client-port').val());
      var name = $('#client-name').val();

      hutHutHut(host, port, name);
    });
  };
}

if (Meteor.isServer) {
  Meteor.startup(function () {
    // code to run on server at startup
  });
}
