var DemoAppController = Class.create({
    initialize: function(host, port, mapContainerSelector) {
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

        this.createQrCodeView();
        this.huddle = this.createHuddle(host, port);
        this.map = this.createMap();
        this.initializeListeners();
    },

    createQrCodeView: function() {
        this.qrCodeContainer = $('<div id="qrcode-container" class="huddle-fullscreen"><div id="qrcode"></div></div>');
        $('body').append(this.qrCodeContainer);

        var windowWidth = $(window).width();
        var windowHeight = $(window).height();

        var qrCodeSize = windowWidth > windowHeight ? windowHeight : windowWidth;

        $('#qrcode').qrcode({
            width: qrCodeSize - 100,
            height: qrCodeSize - 100,
            text: this.deviceId
        });

        this.qrCodeContainer.width(windowWidth);
        this.qrCodeContainer.height(windowHeight);
    },

    createHuddle: function (host, port) {
        var controller = this;

        var huddle = new Huddle(this.deviceId, function (data) {
            //console.log(data);

            if (data.Type) {
                switch (data.Type) {
                    case 'Proximity':
                        if (controller.isUpdateProxemics)
                            controller.processProximity(data.Data);//.bind(controller);
                        break;
                    case 'Digital':
                        if (data.Data.Value)
                            controller.qrCodeContainer.show();
                        else
                            controller.qrCodeContainer.hide();
                        break;
                    case 'Broadcast':
                        if (data.panBy) {
                            var x = data.panBy.x;
                            var y = data.panBy.y;

                            controller.map.panBy(x, y);
                        }
                        else {
                            controller.isUpdateProxemics = data.isUpdateProxemics;
                        }

                        break;
                }
            } else {
                console.log("echo");
            }
        });
        huddle.reconnect = true;
        huddle.connect(host, port);

        return huddle;
    },

    createMap: function () {
        $('<div id="map-canvas"></div>').appendTo($(this.mapContainerSelector));

        var centerLatLng = new google.maps.LatLng(47.6774887, 9.1642378);
        var mapOptions = {
            center: centerLatLng,
            zoom: 13,
            disableDefaultUI: true,
            draggable: false
        };
        return new google.maps.Map(document.getElementById("map-canvas"), mapOptions);
    },

    initializeListeners: function () {
        $(this.mapContainerSelector).on("mousedown", { controller: this }, this.dragstart);
        //$(this.mapContainerSelector).on("touchdown", { controller: this }, dragstart);

        $(this.mapContainerSelector).on("mousemove", { controller: this }, this.dragmove);
        //$(this.mapContainerSelector).on("touchmove", { controller: this }, dragmove);

        $(this.mapContainerSelector).on("mouseup", { controller: this }, this.dragend);
        //$(this.mapContainerSelector).on("touchend", { controller: this }, dragend);
    },

    dragstart: function (e) {
        e.preventDefault();

        var controller = e.data.controller;
        controller.isDragging = true;
        controller.dragPosition.x = e.screenX;
        controller.dragPosition.y = e.screenY;

        controller.isUpdateProxemics = false;
        var broadcast = '"isUpdateProxemics": false';
        controller.huddle.broadcast(broadcast);
    },

    dragmove: function (e) {
        e.preventDefault();

        var controller = e.data.controller;

        if (!controller.isDragging) return;

        var deltaX = controller.dragPosition.x - e.screenX;
        var deltaY = controller.dragPosition.y - e.screenY;

        controller.dragPosition.x = e.screenX;
        controller.dragPosition.y = e.screenY;

        controller.map.panBy(deltaX, deltaY);

        var broadcast = '"panBy": {{"x": {0}, "y": {1}}}'.format(deltaX, deltaY);
        controller.huddle.broadcast(broadcast);
    },

    dragend: function (e) {
        e.preventDefault();

        var controller = e.data.controller;
        controller.isDragging = false;
    },

    getParameterByName: function (name) {
        name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
        var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
            results = regex.exec(location.search);
        return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
    },

    processProximity: function (data) {
        var location = data.Location.split(",");
        var x = location[0];
        var y = location[1];
        var angle = data.Orientation % 360;

        if (angle < 0)
            angle += 360;

        if (angle > 45 && angle < 135) {
            $('#tool-palette-container').show();
        }
        else {
            $('#tool-palette-container').hide();
        }

        if (angle > 215 && angle < 315) {
            $('#note-container').show();
        }
        else {
            $('#note-container').hide();
        }

        //var point = new google.maps.Point(x * -10, y * 100);

        //var latLng = projection.fromPointToLatLng(point);

        if (!this.isUpdateProxemics) return;

        var zoom = this.map.getZoom();
        var offsetX = y / zoom * 8;
        var offsetY = (1.0 - x) / zoom * 8;

        //var latLng2 = new google.maps.LatLng(centerLatLng.d - offsetX, centerLatLng.e - offsetY);
        var latLng2 = new google.maps.LatLng(47.7083395 - offsetX, 9.1517065 - offsetY);

        //map.panTo(latLng2);
        this.map.setCenter(latLng2);
    },
});