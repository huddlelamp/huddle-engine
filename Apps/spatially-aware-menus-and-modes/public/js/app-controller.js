(function($) {
  var AppController = function (host, port, mapContainerSelector) {
      this.mapContainerSelector = mapContainerSelector;

      this.deviceId = this.getParameterByName('id');
      if (!this.deviceId || this.deviceId == 'undefined')
          this.deviceId = 0;

      this.isDragging = false;
      this.dragPosition = {
          x: 0,
          y: 0
      };
      this.isUpdateProxemics = true;

      this.activationAngle = 75;

      this.huddle = this.createHuddle(host, port);
      this.map = this.createMap(this.deviceId);
      this.worldMapCenter = this.map.getCenter();
      this.latestWorldMapCenter = this.map.getCenter();

      this.mapOffset = {
          x: 0,
          y: 0
      };

      this.lastPosition = {
          x: 0,
          y: 0
      };

      this.initializeListeners();
  };

  AppController.prototype.createHuddle = function (host, port) {
      var controller = this;

      var huddle = Huddle.client("")
          .on("displaymove", function (data) {
              if (controller.isUpdateProxemics)
                  controller.processProximity(data);
          })
          .on("panBy", function (data) {
              var x = data.x;
              var y = data.y;

              controller.map.panBy(x, y);
          })
          .on("isUpdateProxemics", function(data) {
              controller.isUpdateProxemics = data.value;
          })
          .on("mapTypeId", function (data) {
              controller.map.setMapTypeId(data.mapId);
          })
          .on("worldMapCenter", function (data) {
              controller.worldMapCenter = new google.maps.LatLng(data.lat, data.lng);
          });
      huddle.connect(host, port);

      return huddle;
  };

  AppController.prototype.createMap = function (deviceId) {

      var latitude = this.getParameterByName('lat');
      if (!latitude || latitude == 'undefined') {
          if (deviceId == 5) {
              latitude = 47.6083395;
          }
          else {
              latitude = 47.7083395;
          }
      }

      var longitude = this.getParameterByName('lng');
      if (!longitude || longitude == 'undefined') {
          if (deviceId == 3) {
              longitude = 8.9317065;
          } else {

              longitude = 9.1517065;
          }
      }

      var zoom = this.getParameterByName('zoom');
      if (!zoom || zoom == 'undefined')
          zoom = 13;

      switch (deviceId) {
          case 3:
              //latitude -= 5;
              break;
          case 4:
              break;
          case 5:
              break;
          default:
      }

      var mapTypeId = this.getParameterByName('mtype');
      if (!mapTypeId || mapTypeId == 'undefined')
          mapTypeId = google.maps.MapTypeId.ROADMAP;

      var $mapCanvas = $('<div id="map-canvas"></div>').appendTo($(this.mapContainerSelector));

      var centerLatLng = new google.maps.LatLng(latitude, longitude);
      var mapOptions = {
          center: centerLatLng,
          mapTypeId: mapTypeId,
          zoom: zoom,
          disableDefaultUI: true,
          draggable: false
      };
      return new google.maps.Map($mapCanvas.get(0), mapOptions);
  };

  AppController.prototype.changeMapType = function (mapTypeId) {
      this.map.setMapTypeId(mapTypeId);

      var msg = '{"mapId": "{0}"}'.format(mapTypeId);
      this.huddle.broadcast("mapTypeId", msg);
  };

  AppController.prototype.initializeListeners = function () {
      $(this.mapContainerSelector).on("mousedown", { controller: this }, this.dragstart);
      //$(this.mapContainerSelector).on("touchdown", { controller: this }, dragstart);

      $(this.mapContainerSelector).on("mousemove", { controller: this }, this.dragmove);
      //$(this.mapContainerSelector).on("touchmove", { controller: this }, dragmove);

      $(this.mapContainerSelector).on("mouseup", { controller: this }, this.dragend);
      //$(this.mapContainerSelector).on("touchend", { controller: this }, dragend);
  };

  AppController.prototype.dragstart = function (e) {
      e.preventDefault();

      var controller = e.data.controller;
      controller.isDragging = true;
      controller.dragPosition.x = e.screenX;
      controller.dragPosition.y = e.screenY;

      controller.isUpdateProxemics = false;
      var msg = '{"value": false}';
      controller.huddle.broadcast("isUpdateProxemics", msg);
  };

  AppController.prototype.dragmove = function (e) {
      e.preventDefault();

      var controller = e.data.controller;

      if (!controller.isDragging) return;

      var deltaX = controller.dragPosition.x - e.screenX;
      var deltaY = controller.dragPosition.y - e.screenY;

      controller.dragPosition.x = e.screenX;
      controller.dragPosition.y = e.screenY;

      controller.map.panBy(deltaX, deltaY);

      var msg = '{"x": {0}, "y": {1}}'.format(deltaX, deltaY);
      controller.huddle.broadcast("panBy", msg);
  };

  AppController.prototype.dragend = function (e) {
      e.preventDefault();

      var controller = e.data.controller;

      console.log(controller.worldMapCenter);

      var localMapCenter = controller.map.getCenter();

      var globalMapCenter = new google.maps.LatLng(localMapCenter.lat() + controller.mapOffset.x, localMapCenter.lng() + controller.mapOffset.y);

      console.log("WorldMap: {0}, GlobalMap: {1}".format(localMapCenter.toString(), globalMapCenter.toString()));

      //controller.worldMapCenter.d -= controller.latestWorldMapCenter.d;
      //controller.worldMapCenter.e -= controller.latestWorldMapCenter.e;

      controller.worldMapCenter = globalMapCenter;

      var broadcastWmc = '{"lat": {0}, "lng": {1}}'.format(globalMapCenter.lat(), globalMapCenter.lng());
      controller.huddle.broadcast("worldMapCenter", broadcastWmc);

      controller.isDragging = false;

      controller.isUpdateProxemics = true;
      var msg = '{"value": true}';
      controller.huddle.broadcast("isUpdateProxemics", msg);
  };

  AppController.prototype.getParameterByName = function (name) {
      name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
      var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
          results = regex.exec(location.search);
      return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
  };

  AppController.prototype.processProximity = function (data) {
      var location = data.Location;
      var x = location[0];
      var y = location[1];
      var angle = data.Orientation % 360;

      var distant = false;
      var presences = data.Presences;
      for (var i = 0; i < presences.length; i++) {
          //console.log(presences[i]);

          var distance = presences[i].Distance;
          if (distance < 0.5) {
              distant = false;
              break;
          } else {
              distant = true;
          }
      }

      if (angle < 0)
          angle += 360;

      //if (angle > this.activationAngle && angle < this.activationAngle + 45) {
      if (angle > this.activationAngle && angle < this.activationAngle + 45) {
          $('#tool-palette-container').show();
      }
      else {
          $('#tool-palette-container').hide();
      }

      //if (angle > (360 - this.activationAngle - 45) && angle < (360 - this.activationAngle)) {
      if (distant) {
          $('#note-container').show();
      }
      else {
          $('#note-container').hide();
      }

      if (!this.isUpdateProxemics) return;

      //if (Math.abs(this.lastPosition.x - x) < 0.001 || Math.abs(this.lastPosition.y - y) < 0.001)
      //    return;

      this.lastPosition.x = x;
      this.lastPosition.y = y;

      var zoom = this.map.getZoom();
      var offsetX = y / zoom * 8;
      var offsetY = (1.0 - x) / zoom * 8;

      this.mapOffset.x = offsetX;
      this.mapOffset.y = offsetY;

      this.latestWorldMapCenter = new google.maps.LatLng(this.worldMapCenter.lat() - offsetX, this.worldMapCenter.lng() - offsetY);

      //this.map.panTo(this.latestWorldMapCenter);

      //console.log("World: {0}".format(this.latestWorldMapCenter));

      this.map.setCenter(this.latestWorldMapCenter);
  };

  // Make AppController globally available.
  window.AppController = AppController;
})(jQuery);
