Objects = new Meteor.Collection("objects");

if (Meteor.isClient) {
  function getParameterByName(name) {
      name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
      var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
          results = regex.exec(location.search);
      return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
  }

  UI.body.rendered = function () {
    console.log("UI.body.rendered");

    // disable touch to avoid moving scroll view
    document.ontouchstart = function(e) {
      e.preventDefault();
    };

    //this.$('#world-canvas').visualizeTouches();
    
    var id = getParameterByName("id");
    var debug = getParameterByName("debug");
    if (debug == 'undefined')
      debug = false;

    console.log("Client Id: " + id);

    var glyphContainer = this.$('#glyph-container');
    var glyph = this.$('#glyph');
    glyph.css('background-image', 'url(glyphs/' + id + '.png)');

    var windowWidth = $(window).width();
    var windowHeight = $(window).height();

    $worldCanvas = $('#world-canvas');
    var canvasWidth = $worldCanvas.width();
    var canvasHeight = $worldCanvas.height();

    var devicePixelRatio = window.devicePixelRatio || 1.0;
    
    // TEST BEGIN
    /*
    var ratio = 0.22;

    var scaleX = 1 / ((canvasWidth * ratio) / width);

    //var scaleX = 1 / ((canvasWidth * ratio) / width); 

    var angle = 0.0;
    var centerX = 2048;
    var centerY = 1536;
    var tx = 6000;
    var ty = 2000;

    console.log("test: " + scaleX);

    var translateTransform = '';//'translate(-' + tx + 'px,-' + ty + 'px)';
    var rotateTransform = '';//'rotate(' + -(angle) + 'deg)';
    var scaleTransform = 'scale(' + scaleX + ',' + scaleX + ')';

    var transform = translateTransform + ' ' + rotateTransform + ' ' + scaleTransform;
    //var transform = scaleTransform + ' ' + rotateTransform + ' ' + scaleTransform;
    var transformOrigin =  (tx + centerX) + 'px ' + (ty + centerY) + 'px 0'

    
    $worldCanvas.css('-webkit-transform-origin', '0% 0% 0');
    $worldCanvas.css('-webkit-transform', transform);
    */
    /// TEST END

/*
    var $overview = $($worldCanvas.get(0).cloneNode(true));
    //var canvasBackgroundImage = $overview.css('background-image');
    //var canvasBackgroundRepeat = $overview.css('background-repeat');
    //var canvasBackgroundSize = $overview.css('background-size');
    $overview.attr('id', 'world-canvas-copy');
    //$overview.css('-webkit-transform', 'scale(0.03,0.03)');
    $overview.css('height', '100%');
    //$overview.css('background-color', 'green');
    //$overview.css('background-image', canvasBackgroundImage);
    //$overview.css('background-repeat', canvasBackgroundRepeat);
    //$overview.css('background-size', canvasBackgroundSize);
    $('#world-canvas-overview').append($overview);
*/
    var huddle = new Huddle(id, function (data) {
        if (data.Type) {

            if (debug) return;

            switch (data.Type) {
                case 'Proximity':
//                    console.log('proximity data: ' + JSON.stringify(data.Data));

                    var location = data.Data.Location.split(",");
                    var x = location[0];
                    var y = location[1];
                    var angle = data.Data.Orientation;
                    var ratio = data.Data.RgbImageToDisplayRatio;

                    // Indonesien Jakarta
                    //var x = 0.769;
                    //var y = 0.402;

                    console.log(ratio.X);

                    var scaleX = ((ratio.X * windowWidth) / canvasWidth);
                    var scaleY = ((ratio.Y * windowHeight) / canvasHeight);
 
                    //console.log("Rgb Image to Device Ratio: " + ratio.X + "," + ratio.Y);

                    //var ratioPeepholeToWorldX = canvasWidth * ratio.X;
                    //var ratioPeepholeToWorldY = canvasHeight * ratio.Y;

                    //var sx = (1280 / canvasWidth) / ratio.X;
                    //var sy = (720 / canvasHeight) / ratio.Y;

                    //var scaleX = 1 / (canvasWidth * ratio.X) / width;
                    //var scaleY = 1 / (canvasHeight * ratio.Y) / height;

                    //if (id == 9 || id == 12) {
                    //  sx *= 0.38;
                    //  sy *= 0.38;
                    //}

                    //var centerX = parseInt(width / 2);
                    //var centerY = parseInt(height / 2);
                    //var tx = -1 * (x * canvasWidth - (width / 2)) / sx;
                    //var ty = -1 * (y * canvasHeight - (height / 2)) / sy;
                    //var tx = -1 * x * canvasWidth + (width * x / 2);
                    //var ty = -1 * y * canvasHeight + (height * y / 2);

                    var deviceCenterToDeviceLeft = ((windowWidth / ratio.Y) / 2);
                    var deviceCenterToDeviceTop = ((windowHeight / ratio.Y) / 2);

                    var tx = -1 * x * canvasWidth;
                    var ty = -1 * y * canvasHeight;

                    var txd = tx + deviceCenterToDeviceLeft;
                    var tyd = ty + deviceCenterToDeviceTop;

                    
                    var rotateTransform = 'rotate(' + -(angle) + 'deg)';
                    

                    //var transform = translateTransform + ' ' + rotateTransform; + ' ' + scaleTransform;
                    //var transform = translateTransform + ' ' + rotateTransform + ' ' + scaleTransform;
                    var transform = ' scale(' + scaleX + ',' + scaleY + ')';
                    transform += ' translate(' + txd + 'px,' +tyd + 'px)';
                    //transform += ' translate(' + (-1 * tx) + 'px,' + (-1 * ty) + 'px)';
                    //transform += ' scale(' + 0.5 + ',' + 0.5 + ')';
                    //transform += ' translate(' + (1 * tx) + 'px,' + (1 * ty) + 'px)';

                    //console.log('asdf' + transform);

                    //var transformOrigin =  (tx + centerX) + 'px ' + (ty + centerY) + 'px 0'

                    //var transformOrigin =  '0px 0px 0';

                    $worldCanvas.css('-webkit-transform-origin', '0px 0px 0px');
                    $worldCanvas.css('-webkit-transform', transform);
                    //$worldCanvas.css({
                    //  left: tx + 'px',
                    //  top: ty + 'px'
                    //});

                    //console.log('transform ' + transform);

                    break;
                case 'Digital':
                    if (data.Data.Value)
                      glyphContainer.show();
                    else
                      glyphContainer.hide();
                    break;
                case 'Broadcast':
                    
                    break;
            }
        }
        else {
            console.log("echo");
        }
      });
      huddle.reconnect = true;
      huddle.connect("192.168.1.121", 4711);
  };

  Template.worldCanvas.objects = function() {
    return Objects.find({});
  };

  Template.worldObject.rendered = function(e) {
    console.log("rendered world canvas: ");

    /*
    this.$('div').draggable({
      start: function(e) {
        console.log("start: " + $(this).position().left);
      },
      drag: function(e) {
        var id = this.id;
        
        var position = $(this).position();

        Objects.update({_id: id}, {$set: {
            x: position.left,
            y: position.top
          }}, 
          function(res) {
            return console.log(res);
          });
      },
      stop: function(e) {
        console.log("stop: " + e);
      }
      */

      $div = this.$('div');

      $div.gesture({
        drag: true,
        scale: true,
        rotate: true,
        touchtarget: null
      });

      $div.gestureInit();

      $div.on('touch_move', function(e) {
        var id = this.id;
        
        var position = $(this).position();

        var transformOrigin = $(this).css('-webkit-transform-origin');
        var transform = $(this).css('-webkit-transform');

        console.log("transform: " + transform);

        Objects.update({_id: id}, {$set: {
            x: position.left,
            y: position.top,
            angle: e.rotation,
            scale: e.scale,
            transform: transform,
            transformOrigin: transformOrigin
          }}); 
      });

      $div.on('gesture_move', function(e) {
        console.log("I was moved with gesture");

        var id = this.id;
        
        var position = $(this).position();

        var transformOrigin = $(this).css('-webkit-transform-origin');
        var transform = $(this).css('-webkit-transform');

        console.log("transform: " + transform);

        Objects.update({_id: id}, {$set: {
            x: position.left,
            y: position.top,
            angle: e.rotation,
            scale: e.scale,
            transform: transform,
            transformOrigin: transformOrigin
          }}); 
      });

      $div.touchInit();
  };

  Template.worldObject.events({
    'click': function(e) {
      console.log("click me more!!!");
    },

    'gesture_move2': function(e) {
      console.log('asdfasfasf');
        var id = this.id;
        
        var position = $(this).position();

        Objects.update({_id: id}, {$set: {
            x: position.left,
            y: position.top
          }}, 
          function(res) {
            return console.log(res);
          });
    }
  });

  Template.worldCanvas.events({
    'click #world-canvas': function(e) {
      console.log('clicked canvas');
 
      Objects.insert({
        x: e.offsetX,
        y: e.offsetY,
        width: 200,
        height: 200,
        angle: 0.0,
        scale: 1.0
      });

      e.preventDefault();
    },

    'mousedown .world-object': function(e) {
      console.log('echo mouse down');
    },

    'mousemove .world-object': function(e) {
      //console.log(e.clientX);


    }
  });
}

if (Meteor.isServer) {
  Meteor.startup(function () {
    // code to run on server at startup
  });
}
