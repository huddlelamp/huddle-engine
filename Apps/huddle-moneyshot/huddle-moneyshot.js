Objects = new Meteor.Collection("objects");

if (Meteor.isClient) {
  UI.body.rendered = function () {
    console.log("UI.body.rendered");

    // disable touch to avoid moving scroll view
    document.ontouchstart = function(e) {
      e.preventDefault();
    };

    this.$('#world-canvas').visualizeTouches();
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
    'click #world-canvas2': function(e) {
      console.log('clicked canvas');
 
      Objects.insert({
        x: 0,
        y: 0,
        width: 200,
        height: 200,
        angle: 0.0,
        scale: 1.0
      });
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
