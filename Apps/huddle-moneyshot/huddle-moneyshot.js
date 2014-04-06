Objects = new Meteor.Collection("objects");

if (Meteor.isClient) {
  UI.body.rendered = function () {
    console.log("rendered");  
  };

  Template.worldCanvas.objects = function() {
    return Objects.find({});
  };

  Template.worldObject.rendered = function(e) {
    console.log("rendered world canvas: ");

    this.$('div').draggable({
      start: function(e) {
        console.log("start: " + $(this).position().left);
      },
      drag: function(e) {
        var id = this.id;
        console.log("drag: " + id);

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
    });

    //template.findAll('.world-object').each(function(index, value) {
    //  console.log("value: " + value);
    //});
  }

  Template.worldCanvas.events({
    'click #world-canvas2': function(e) {
      console.log('clicked canvas');
 
      Objects.insert({
        x: e.clientX,
        y: e.clientY,
        width: 400,
        height: 100,
        angle: 0.12
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
