if (Meteor.isClient) {
  Template.snippets.initSnippet = function() {
    var that = this;
    Meteor.setTimeout(function() {
      //If the snippet is already visible we don't need to initialize it
      if ($("#snippet_"+that._id).css("display") !== "none") {
        return;
      }

      var top  = that.y;
      var left = that.x;

      if (top === undefined) top = getRandomInt(0, 600);
      if (left === undefined) left = getRandomInt(0, 600);

      $("#snippet_"+that._id).css({
        display: 'inline-block',
        top: top,
        left: left
      });
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

  var frontSnippet;
  var dragLastPoint;
  var draggedSnippet;
  var highlightedIndicator;
  Template.snippets.events({
    'touchstart .snippet, mousedown .snippet': function(e) {
      if (frontSnippet !== undefined) frontSnippet.css({'z-index': ''});

      var snippetID = this._id;
      frontSnippet = $("#snippet_"+snippetID);
      frontSnippet.css({'z-index': 7011});
    },

    'touchstart .snippetmover, mousedown .snippetmover': function(e) {
      e.preventDefault();

      // if (frontSnippet !== undefined) frontSnippet.css({'z-index': ''});

      var snippetID = this._id;

      draggedSnippet = $("#snippet_"+snippetID);
      dragLastPoint = getEventLocation(e, "client");

      // frontSnippet = draggedSnippet;
      // frontSnippet.css({'z-index': 7012});
    },

    'touchmove .snippetmover, mousemove .snippetmover, touchmove .deviceIndicator, mousemove .deviceIndicator': function(e) {
      // e.preventDefault();
      
      if (dragLastPoint === undefined) return;

      var snippetID = this._id;
      var snippet = $("#snippet_"+snippetID);

      var currentPoint = getEventLocation(e, "client");
      var deltaX = currentPoint.x - dragLastPoint.x;
      var deltaY = currentPoint.y - dragLastPoint.y;

      dragLastPoint = currentPoint;

      snippet.css({
        top: parseInt(snippet.css('top'))   + deltaY,
        left: parseInt(snippet.css('left')) + deltaX
      });
      markSnippetDirty(this);

      var hitTarget = document.elementFromPoint(currentPoint.x, currentPoint.y);
      if ($(e.target).hasClass("deviceIndicator")) {
        highlightedIndicator = e.target;
        Template.deviceIndicators.highlightIndicator(e.target);
      } else 
      if ($(hitTarget).hasClass("deviceIndicator")) {
        highlightedIndicator = hitTarget;
        Template.deviceIndicators.highlightIndicator(hitTarget);
      } else if (highlightedIndicator !== undefined) {
        Template.deviceIndicators.unhighlightIndicator(highlightedIndicator);
        highlightedIndicator = undefined;
      }

      if ($(hitTarget).attr("id") === "openWorldView") {

      }
    },

    'touchend .snippetmover, mouseup .snippetmover, touchend .deviceIndicator, mouseup .deviceIndicator': function(e) {
      var lastHitTarget = document.elementFromPoint(dragLastPoint.x, dragLastPoint.y);
      dragLastPoint = undefined;

      var snippetContent = $(draggedSnippet).children(".snippetcontent").text();

      //Check if snippet is let go above a device indicator. If so, move the snippet to that device
      // if ($(e.target).hasClass("deviceIndicator")) {
      //   // var snippetData = Snippets.findOne({_id:snippetID});
      //   Template.deviceIndicators.sendThroughIndicator(e.target, snippetContent, this.sourcedoc);
      //   // var snippetID = $(draggedSnippet).attr("id").replace("snippet_", "");
      //   Snippets.remove({_id : this._id});
      // } else 
      if ($(lastHitTarget).hasClass("deviceIndicator")) {
        Template.deviceIndicators.sendThroughIndicator(lastHitTarget, snippetContent, this.sourcedoc);
        // var snippetID = $(draggedSnippet).attr("id").replace("snippet_", "");
        Snippets.remove({_id : this._id});

        var thisDevice = Session.get('thisDevice');
        Logs.insert({
          timestamp  : Date.now(),
          route      : Router.current().route.name,
          deviceID   : thisDevice.id,  
          actionType : "deleteSnippet",
          actionSubsource : "share",
          snippetID  : this._id
        });
      }

      //Check if the snippet is let go above the open world view button. If so, open the world view
      //to share this snippet
      if ($(lastHitTarget).attr("id") === "openWorldView") {
        Session.set("worldViewSnippetDoc", this.sourcedoc);
        Session.set("worldViewSnippetToSend", snippetContent);
        Session.set("worldViewSnippetRemove", this._id);
        $("#openWorldView").click();
      }

      // draggedSnippet.css({'z-index': ''});
      draggedSnippet = undefined;
    },

    'touchend .snippetsharer': function(e) {
      showSharePopup(e.target, this);
    },

    'touchend .snippetdeleter': function(e) {
      var snippetID = this._id;
      $(e.target).hide();
      $("#snippet_"+snippetID+" .snippetdeleterconfirmation").show({duration: 400});
    },

    'touchend .snippetdeleterconfirmation .btn-danger': function(e) {
      Snippets.remove({_id: this._id});

      var thisDevice = Session.get('thisDevice');
      Logs.insert({
        timestamp  : Date.now(),
        route      : Router.current().route.name,
        deviceID   : thisDevice.id,  
        actionType : "deleteSnippet",
        actionSource : "snippets",
        actionSubsource : "button",
        snippetID  : this._id
      });
    },

    'touchend .snippetdeleterconfirmation .btn-cancel': function(e) {
      var snippetID = this._id;
      $("#snippet_"+snippetID+" .snippetdeleterconfirmation").hide({
        duration: 400,
        complete: function() {
          $("#snippet_"+snippetID+" .snippetdeleter").show();
        }
      });
    }
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

var removeSnippet = function(snippetID) {
  Snippets.remove({_id : snippetID});
};

var dirtyTimer = {};
function markSnippetDirty(snippet) {
  if (dirtyTimer[snippet._id] !== undefined) return;

  dirtyTimer[snippet._id] = Meteor.setTimeout(function() {
    dirtyTimer[snippet._id] = undefined;

    if ($("#snippet_"+snippet._id).length === 0 || $("#snippet_"+snippet._id).css("display") === "none") {
      return;
    }

    var y = parseInt($("#snippet_"+snippet._id).css('top'));
    var x = parseInt($("#snippet_"+snippet._id).css('left'));
    var existing = Snippets.findOne({_id : snippet._id});

    if (existing.x === x && existing.y === y) return; 

    Snippets.update(
      {_id: snippet._id}, 
      {$set: {
        y: y,
        x: x
      }}
    );

    var thisDevice = Session.get('thisDevice');
    Logs.insert({
      timestamp  : Date.now(),
      route      : Router.current().route.name,
      deviceID   : thisDevice.id,  
      actionType : "movedSnippet",
      snippetID  : snippet._id,
      position   : {x: x, y: y}
    });
  }, 2500);
}

var showSharePopup = function(el, snippet) {
  var otherDevices = Template.detailDocumentTemplate.otherDevices();
  var text = snippet.text;

  var content = $("<span>Send text snippet to:</span>");
  content.append("<br />");
  content.append("<br />");

  for (var i=0; i<otherDevices.length; i++) {
    var device = otherDevices[i];
    var info = DeviceInfo.findOne({ _id: device.id });
    if (info === undefined || info.colorDeg === undefined) return;

    var color = new tinycolor(window.degreesToColor(info.colorDeg)).toRgbString();

    var link = $("<button />");
    link.attr("deviceid", device.id);
    link.addClass("btn shareDevice noDeviceCustomization popupClickable");
    link.css('border-color', color);

    link.on('click', function(e2) {
      var targetID = $(this).attr("deviceid");
      if (targetID === undefined) return;

      huddle.broadcast("addtextsnippet", { target: targetID, doc: snippet.sourcedoc, snippet: text } );
      // pulseIndicator(e.currentTarget);
      // showSendConfirmation(e.currentTarget, "The selected text was sent to the device.");
      Snippets.remove({_id : snippet._id});

      $(el).popover('hide');

      var thisDevice = Session.get('thisDevice');
      Logs.insert({
        timestamp       : Date.now(),
        route           : Router.current().route.name,
        deviceID        : thisDevice.id,  
        actionType      : "shareSnippet",
        actionSource    : "snippets",
        actionSubsource : "button",
        targetDeviceID  : targetID,
        documentID      : snippet.sourcedoc,
        snippet         : text,
      });
      Logs.insert({
        timestamp  : Date.now(),
        route      : Router.current().route.name,
        deviceID   : thisDevice.id,  
        actionType : "deleteSnippet",
        actionSource : "snippets",
        actionSubsource : "share",
        snippetID  : snippet._id
      });
    });
    content.append(link);
  } 

  // showPopover(el, content, {placement: "top", container: "body"});
  
  $(el).popover('destroy');
  $(el).popover({
    trigger   : "manual",
    placement : "top",
    content   : content,
    container : "body",
    html      : true,
  });

  //We want to close popups when the user clicks basically anywhere outside of them
  //If showPopup() is used in a click event handler, though, this would cause the
  //popup to close immediatly, therefore we setup the event handlers on body in the
  //next run loop
  Meteor.setTimeout(function() {
    $("body").off('touchstart');
    $("body").on('touchstart', function(e) {
      if ($(e.target).hasClass("popupClickable") === false) {
        e.preventDefault();
      }

      //Don't hide the popup if an element inside of it was touched
      var popover = $("#"+$(el).attr("aria-describedby"));
      if (popover.length > 0 && $.contains(popover[0], e.target)) {
        return;
      }

      $(el).popover('hide');
      $("body").off('touchstart');
    });
  }, 1);

  $(el).popover('show');
};

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
