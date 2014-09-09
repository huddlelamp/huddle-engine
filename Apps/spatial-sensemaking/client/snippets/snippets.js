if (Meteor.isClient) {
  Template.snippets.initSnippetPosition = function() {
    var that = this;
    Meteor.setTimeout(function() {
      if ($("#snippet_"+that._id).css("display") !== "none") {
        return;
    }

      var top  = that.y || 0;
      var left = that.x || 0;

      if (top === undefined) top = getRandomInt(0, 600);
      if (left === undefined) left = getRandomInt(0, 600);
    
      $("#snippet_"+that._id).css({
        top: top,
        left: left
      });
      $("#snippet_"+that._id).css('display', 'inline-block');
      markSnippetDirty(that);
    }, 1);
  };

  Template.snippets.snippets = function() {
    var thisDevice = Session.get('thisDevice');
    if (thisDevice === undefined) return [];

    var snippets = Snippets.find({ device: thisDevice.id });
    return snippets.fetch();
  };

  Template.snippets.otherDevices = function() {
    return Session.get("otherDevices") || [];
  };

  var dragLastPoint;
  var draggedSnippet;
  var highlightedIndicator;
  Template.snippets.events({
    'touchstart .snippet, mousedown .snippet': function(e) {
      e.preventDefault();
      dragLastPoint = getEventLocation(e, "client");
      draggedSnippet = e.target;
    },

    'touchmove .snippet, mousemove .snippet, touchmove .deviceIndicator, mousemove .deviceIndicator': function(e) {
      // e.preventDefault();
      
      if (dragLastPoint === undefined) return;

      var currentPoint = getEventLocation(e, "client");
      var deltaX = currentPoint.x - dragLastPoint.x;
      var deltaY = currentPoint.y - dragLastPoint.y;

      dragLastPoint = currentPoint;

      $("#snippet_"+this._id).css({
        top: parseInt($("#snippet_"+this._id).css('top'))   + deltaY,
        left: parseInt($("#snippet_"+this._id).css('left')) + deltaX
      });
      markSnippetDirty(this);

      var hitTarget = document.elementFromPoint(currentPoint.x, currentPoint.y);
      if ($(e.target).hasClass("deviceIndicator")) {
        highlightedIndicator = e.target;
        Template.deviceIndicators.highlightIndicator(e.target);
      } else if ($(hitTarget).hasClass("deviceIndicator")) {
        highlightedIndicator = hitTarget;
        Template.deviceIndicators.highlightIndicator(hitTarget);
      } else if (highlightedIndicator !== undefined) {
        Template.deviceIndicators.unhighlightIndicator(highlightedIndicator);
        highlightedIndicator = undefined;
      }
    },

    'touchend .snippet, mouseup .snippet, touchend .deviceIndicator, mouseup .deviceIndicator': function(e) {
      var lastHitTarget = document.elementFromPoint(dragLastPoint.x, dragLastPoint.y);
      dragLastPoint = undefined;

      if ($(e.target).hasClass("deviceIndicator")) {
        Template.deviceIndicators.sendThroughIndicator(e.target, $(draggedSnippet).text());
        var snippetID = $(draggedSnippet).attr("id").replace("snippet_", "");
        Snippets.remove({_id : snippetID});
      } else if ($(lastHitTarget).hasClass("deviceIndicator")) {
        Template.deviceIndicators.sendThroughIndicator(lastHitTarget, $(draggedSnippet).text());
        var snippetID = $(draggedSnippet).attr("id").replace("snippet_", "");
        Snippets.remove({_id : snippetID});
      }

      draggedSnippet = undefined;
    },
  });

  Template.snippets.helpers({
    'thisDeviceBorderColorCSS': function() {
      return window.thisDeviceBorderColorCSS();
    },

    'deviceColorCSS': function() {
      return 'background-color: '+window.deviceColorCSS(this);
    },

    'deviceSizePositionCSS': function() {
      return window.deviceSizePositionCSS(this);
    },
  });
}

var dirtyTimer = {};
function markSnippetDirty(snippet) {
  if (dirtyTimer[snippet._id] !== undefined) return;

  dirtyTimer[snippet._id] = Meteor.setTimeout(function() {
    dirtyTimer[snippet._id] = undefined;

    if ($("#snippet_"+snippet._id).length === 0 || $("#snippet_"+snippet._id).css("display") === "none") {
      return;
    }

    Snippets.update(
      {_id: snippet._id}, 
      {$set: {
        y: parseInt($("#snippet_"+snippet._id).css('top')),
        x: parseInt($("#snippet_"+snippet._id).css('left'))
      }}
    );
  }, 2500);
}

function getEventLocation(e, type) 
{
  if (type === undefined) type = "page";

  var pos = { x: e[type+"X"], y: e[type+"Y"] };
  if (pos.x === undefined || pos.y === undefined)
  {
    pos = { x: e.originalEvent.targetTouches[0][type+"X"], y: e.originalEvent.targetTouches[0][type+"Y"] };
  }

  return pos;
}

function getRandomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}
