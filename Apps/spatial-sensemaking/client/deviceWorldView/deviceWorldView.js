Template.deviceWorldView.rendered = function() {
  $("#openWorldView").popover({
    trigger   : "manual",
    placement : "top",
    content   : $("#worldViewWrapper"),
    container : "body",
    html      : true,
  });
};

Template.deviceWorldView.deviceBorderColorCSS = function() {
  var colorDeg = window.getDeviceColorDeg(this.id);
  // var info = DeviceInfo.findOne({ _id: this.id });
  // if (info === undefined || !info.colorDeg) return "";

  var color = window.degreesToColor(colorDeg);

  return 'border-color: rgb('+color.r+', '+color.g+', '+color.b+');';
};

Template.deviceWorldView.deviceBackgroundColorCSS = function() {
  var colorDeg = window.getDeviceColorDeg(this.id);
  // var info = DeviceInfo.findOne({ _id: this.id });
  // if (info === undefined || !info.colorDeg) return "";

  var thisDevice = Session.get('thisDevice');

  var color = window.degreesToColor(colorDeg);

  alpha = 0.35;
  if (this.id === thisDevice.id) alpha = 0.1;
  return 'background-color: rgba('+color.r+', '+color.g+', '+color.b+', '+alpha+');';
};

Template.deviceWorldView.deviceSizeAndPosition = function() {
  var width  = $("#worldViewWrapper").width() / this.ratio.x;
  var height = $("#worldViewWrapper").height() / this.ratio.y;
  var x      = ($("#worldViewWrapper").width() - width) * this.topLeft.x;
  var y      = ($("#worldViewWrapper").height() - height) * this.topLeft.y;
  return 'width: '+width+'px; height: '+height+'px; top: '+y+'px; left: '+x+'px;';
};

Template.deviceWorldView.devicePosition = function() {
  var width  = $("#worldViewWrapper").width() / this.ratio.x;
  var height = $("#worldViewWrapper").height() / this.ratio.y;
  var x      = ($("#worldViewWrapper").width() - width) * this.topLeft.x;
  var y      = ($("#worldViewWrapper").height() - height) * this.topLeft.y;
  return 'top: '+y+'px; left: '+x+'px;';
};

Template.deviceWorldView.thisDevice = function() {
  return Session.get("thisDevice") || undefined;
};

Template.deviceWorldView.otherDevices = function() {
  return Session.get("otherDevices") || [];
};

Template.deviceWorldView.events({
  'touchend #openWorldView, mouseup #openWorldView': function() {
    Session.set("worldViewSnippetToSend", Template.detailDocumentTemplate.currentlySelectedContent());
  },

  'click #openWorldView': function(e) {
    Template.deviceWorldView.show();
  },

  'click .worldDevice': function(e) {
    // e.preventDefault();

    var targetID = $(e.currentTarget).attr("deviceid");
    if (targetID === undefined) return;

    var text = Session.get("worldViewSnippetToSend");
    // var text = Template.detailDocumentTemplate.currentlySelectedContent();

    Meteor.setTimeout(function() {
      Template.deviceWorldView.hide();
    }, 1000);

    if (text !== undefined && text.length > 0) {
      //If a text selection exists, send it
      huddle.broadcast("addtextsnippet", { target: targetID, snippet: text } );
      // pulseDevice(e.currentTarget);
      showSendConfirmation(e.currentTarget, "The selected text was sent to the device.");
    } else {
      //If no selection was made but a document is open, send that
      var doc = Session.get("detailDocument");
      if (doc !== undefined) {
        huddle.broadcast("showdocument", { target: targetID, documentID: doc._id } );
        // pulseDevice(e.currentTarget);
        showSendConfirmation(e.currentTarget, "The document "+doc._id+" is displayed on the device.");
      } else {
        //If no document is open but a query result is shown, send that
        var lastQuery = Session.get('lastQuery');
        var lastQueryPage = Session.get('lastQueryPage');
        if (lastQuery !== undefined) {
          huddle.broadcast("dosearch", {target: targetID, query: lastQuery, page: lastQueryPage });
          // pulseDevice(e.currentTarget);
          showSendConfirmation(e.currentTarget, "Search results were sent to the device.");
        }
      }
    }
  }
});

function pulseDevice(device) {
  $(device).css('transform', 'scale(1.5, 1.5)');
  Meteor.setTimeout(function() {
    $(device).css('transform', '');
  }, 300);
}

function showSendConfirmation(device, text) {
  $("#worldViewSendText").text(text);

  Meteor.setTimeout(function() {
    // var eWidth = $("#worldViewSendText").width() + parseInt($("#worldViewSendText").css('padding-left')) + parseInt($("#worldViewSendText").css('padding-right'));
    // var eHeight = $("#worldViewSendText").height() + parseInt($("#worldViewSendText").css('padding-top')) + parseInt($("#worldViewSendText").css('padding-bottom'));

    // var deviceWidth = $(device).width();
    // var deviceHeight = $(device).height();

    // var top = parseInt($(device).position().top + deviceHeight/2.0) - eHeight/2.0;
    // var left = parseInt($(device).position().left + deviceWidth/2.0) - eWidth/2.0;
    
    var top = $(document).height() / 2.0;
    var left = $(document).width() / 2.0;
    $("#worldViewSendText").css({ 
      opacity: 1.0, 
      position: "absolute",
      // top: top, 
      // left: left
      bottom: 100,
      right: 50
    });

    Meteor.setTimeout(function() {
      $("#worldViewSendText").css("opacity", 0);
    }, 2000);
  }, 1);
}

//
// "PUBLIC" METHODS
//

Template.deviceWorldView.show = function() {
  $("#worldViewWrapper").css("display", "block");
  $("#openWorldView").popover("show");

  //Remove padding from this popup, we don't want it
  Meteor.setTimeout(function() {
    var popover = $("#"+$("#openWorldView").attr("aria-describedby")+" .popover-content");
    popover.css('padding', 0);
  }, 1);

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
      var popover = $("#"+$("#openWorldView").attr("aria-describedby"));
      if (popover.length > 0 && $.contains(popover[0], e.target)) {
        return;
      }

      Template.deviceWorldView.hide();
    });
  }, 1);

  $(".worldDevice").off("click");
  $(".worldDevice").on('click', function(e) {
    var targetID = $(this).attr("deviceid");
    if (targetID === undefined) return;

    var text = Session.get("worldViewSnippetToSend");

    if (text !== undefined && text.length > 0) {
      //If a text selection exists, send it
      huddle.broadcast("addtextsnippet", { target: targetID, snippet: text } );
      // pulseDevice(this);
      showSendConfirmation($("#openWorldView"), "The selected text was sent to the device.");
    } else {
      //If no selection was made but a document is open, send that
      var doc = Session.get("detailDocument");
      if (doc !== undefined) {
        huddle.broadcast("showdocument", { target: targetID, documentID: doc._id } );
        // pulseDevice(this);
        showSendConfirmation($("#openWorldView"), "The document "+doc._id+" is displayed on the device.");
      } else {
        //If no document is open but a query result is shown, send that
        var lastQuery = Session.get('lastQuery');
        var lastQueryPage = Session.get('lastQueryPage');
        if (lastQuery !== undefined) {
          huddle.broadcast("dosearch", {target: targetID, query: lastQuery, page: lastQueryPage });
          // pulseDevice(this);
          showSendConfirmation($("#openWorldView"), "Search results were sent to the device.");
        }
      }
    }

    Template.deviceWorldView.hide();
  });
};

Template.deviceWorldView.hide = function() {
  $("body").off('touchstart');
  $("#worldViewWrapper").css("display", "none").appendTo("body");
  $("#openWorldView").popover("hide");
};