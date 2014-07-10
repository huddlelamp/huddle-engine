if (Meteor.isClient) {
  Template.main.rendered = function() {
    $(function() {

      // disable touch to avoid moving scroll view
      document.ontouchstart = function (e) {
          e.preventDefault();
      };

      var controller = new AppController("localhost", 4711, '#map-container');

      var windowWidth = $(window).width();
      var windowHeight = $(window).height();

      $.each(['#f00', '#ff0', '#0f0', '#0ff', '#00f', '#f0f', '#000', '#fff'], function () {
          $('#tools').append("<a href='#note' data-color='" + this + "' style='background: " + this + ";'></a> ");
      });
      $.each([3, 5, 10, 15], function () {
          $('#tools').append("<a href='#note' data-size='" + this + "' style='background: #ccc'>" + this + "</a> ");
      });

      var $notes = $('#note');
      $notes.sketch();
      $notes.get(0).setAttribute('width', windowWidth);
      $notes.get(0).setAttribute('height', windowHeight);

      //MapTypeId.ROADMAP displays the default road map view. This is the default map type.
      //MapTypeId.SATELLITE displays Google Earth satellite images
      //MapTypeId.HYBRID displays a mixture of normal and satellite views
      //MapTypeId.TERRAIN

      $('.toggle-button').on('mouseup', function (event) {
          event.preventDefault();

          var id = this.id;
          //console.log(id);
          switch (id) {
              case 'map-id-roadmap':
                  controller.changeMapType(google.maps.MapTypeId.ROADMAP);
                  break;
              case 'map-id-satellite':
                  controller.changeMapType(google.maps.MapTypeId.SATELLITE);
                  break;
              case 'map-id-hybrid':
                  controller.changeMapType(google.maps.MapTypeId.HYBRID);
                  break;
              case 'map-id-terrain':
                  controller.changeMapType(google.maps.MapTypeId.TERRAIN);
                  break;
          }
      });

      $('.toggle-button').on('touchstart', function (event) {
          event.preventDefault();

          var id = this.id;
          //console.log(id);
          switch (id) {
              case 'map-id-roadmap':
                  controller.changeMapType(google.maps.MapTypeId.ROADMAP);
                  break;
              case 'map-id-satellite':
                  controller.changeMapType(google.maps.MapTypeId.SATELLITE);
                  break;
              case 'map-id-hybrid':
                  controller.changeMapType(google.maps.MapTypeId.HYBRID);
                  break;
              case 'map-id-terrain':
                  controller.changeMapType(google.maps.MapTypeId.TERRAIN);
                  break;
          }
      });

      $('#tool-palette').css('transform', 'rotate(270deg)');
    });
  };
}

if (Meteor.isServer) {
  Meteor.startup(function () {
    // code to run on server at startup
  });
}
