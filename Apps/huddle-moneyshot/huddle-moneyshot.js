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
  Objects.find({}).observe({
    changed: function (newDocument, oldDocument) {
      $visual = $('#' + newDocument._id);
      $visual.data('visualProperties', newDocument);
    },
  });

    window.peepholeMetadata = {
      scaleX: 1.0,
      scaleY: 1.0
    };
    var canvasScaleFactor = 1;

    var huddle = new Huddle(id, function (data) {
        if (data.Type) {

            if (debug) return;

            switch (data.Type) {
                case 'Proximity':
                //return;
                    var location = data.Data.Location.split(",");
                    var x = location[0];
                    var y = location[1];
                    var angle = data.Data.Orientation;
                    var ratio = data.Data.RgbImageToDisplayRatio;

                    // Indonesien Jakarta
                    //var x = 0.769;
                    //var y = 0.402;

                    var scaleX = ((ratio.X * windowWidth) / canvasWidth);
                    var scaleY = ((ratio.Y * windowHeight) / canvasHeight);

                    window.peepholeMetadata.scaleX = 1 / (ratio.Y / canvasScaleFactor);
                    window.peepholeMetadata.scaleY = 1 / (ratio.Y / canvasScaleFactor);

                    window.orientationDevice = angle;

                    var deviceCenterToDeviceLeft = ((windowWidth / ratio.Y) / 2);
                    var deviceCenterToDeviceTop = ((windowHeight / ratio.Y) / 2);

                    var tx = -1 * x * canvasWidth;
                    var ty = -1 * y * canvasHeight;

                    var txd = tx + deviceCenterToDeviceLeft;
                    var tyd = ty + deviceCenterToDeviceTop;

                    var transform = ' scale(' + scaleX + ',' + scaleY + ')';
                    transform += ' translate(' + txd + 'px,' +tyd + 'px)';
                    
                    // Translate to center, rotate around center point, and translate back
                    transform += ' translate(' + (-1 * tx) + 'px,' + (-1 * ty) + 'px)';
                    transform += 'rotate(' + -(angle) + 'deg)';
                    transform += ' translate(' + (1 * tx) + 'px,' + (1 * ty) + 'px)';

                    $worldCanvas.css('-webkit-transform-origin', '0px 0px 0px');
                    $worldCanvas.css('-webkit-transform', transform);

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
      huddle.connect("192.168.1.119", 4711);
  };

  Template.worldCanvas.objects = function() {
    return Objects.find({});
  };

  Template.worldObject.rendered = function(e) {
    console.log("rendered world canvas: ");

      $div = this.$('#' + this.data._id);

      $div.interactive({
        visualProperties: this.data,
        peepholeMetadata: window.peepholeMetadata,
        modelUpdated: function(model) {
          //console.log('model updated' + this);

          var id = this.get(0).id;
        
          var $visual = $(this);

          var position = $visual.position();

          var transformOrigin = $visual.css('-webkit-transform-origin');
          var transform = $visual.css('-webkit-transform');

          var vp = $visual.data('visualProperties');
          
          Objects.update({_id: id}, { $set: {
              lockedBy: vp.lockedBy,
              x: vp.x,
              y: vp.y,
              rotation: vp.rotation,
              scale: vp.scale,
              transform: transform,
              transformOrigin: transformOrigin
          }});
        }
      });
/*
      $div.gesture({
        drag: true,
        scale: true,
        rotate: true,
        touchtarget: null,

        scaleX: 4.354254727017134,
        scaleY: 4.314985689142452
      });

      $div.gestureInit();

      $div.on('touch_move', function(e) {
        var id = this.id;
        
        var position = $(this).position();

        var transformOrigin = $(this).css('-webkit-transform-origin');
        var transform = $(this).css('-webkit-transform');
        
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
      */
  };

  Template.worldCanvas.events({
    'click #world-canvas2': function(e) {
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
