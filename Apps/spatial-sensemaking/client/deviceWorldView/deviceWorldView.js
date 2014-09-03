Template.deviceWorldView.rendered = function() {
  Template.deviceWorldView.hide(false);
};

Template.deviceWorldView.deviceBorderColorCSS = function() {
  var info = DeviceInfo.findOne({ _id: this.id });
  if (info === undefined || !info.colorDeg) return "";

  var color = window.degreesToColor(info.colorDeg);

  return 'border-color: rgb('+color.r+', '+color.g+', '+color.b+');';
};

Template.deviceWorldView.deviceBackgroundColorCSS = function() {
  var info = DeviceInfo.findOne({ _id: this.id });
  if (info === undefined || !info.colorDeg) return "";

  var thisDevice = Session.get('thisDevice');

  var color = window.degreesToColor(info.colorDeg);

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

Template.deviceWorldView.thisDevice = function() {
  return Session.get("thisDevice") || undefined;
};

Template.deviceWorldView.otherDevices = function() {
  return Session.get("otherDevices") || [];
};

Template.deviceWorldView.events({
  'touchend .worldDevice, mouseup .worldDevice': function(e, tmpl) {
    e.preventDefault();

    var targetID = $(e.currentTarget).attr("deviceid");
    if (targetID === undefined) return;

    var text = Session.get("worldViewSnippetToSend");

    Meteor.setTimeout(function() {
      Template.deviceWorldView.hide();
    }, 1000);

    if (text !== undefined && text.length > 0) {
      //If a text selection exists, send it
      huddle.broadcast("addtextsnippet", { target: targetID, snippet: text } );
      pulseDevice(e.currentTarget);
      showSendConfirmation(e.currentTarget, "The selected text was sent to the device.");
    } else {
      //If no selection was made but a document is open, send that
      var doc = Session.get("detailDocument");
      if (doc !== undefined) {
        huddle.broadcast("showdocument", { target: targetID, documentID: doc._id } );
        pulseDevice(e.currentTarget);
        showSendConfirmation(e.currentTarget, "The document "+doc._id+" is displayed on the device.");
      } else {
        //If no document is open but a query result is shown, send that
        var lastQuery = Session.get('lastQuery');
        var lastQueryPage = Session.get('lastQueryPage');
        if (lastQuery !== undefined) {
          huddle.broadcast("dosearch", {target: targetID, query: lastQuery, page: lastQueryPage });
          pulseDevice(e.currentTarget);
          showSendConfirmation(e.currentTarget, "Search results were sent to the device.");
        }
      }
    }
  },
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
    var eWidth = $("#worldViewSendText").width() + parseInt($("#worldViewSendText").css('padding-left')) + parseInt($("#worldViewSendText").css('padding-right'));
    var eHeight = $("#worldViewSendText").height() + parseInt($("#worldViewSendText").css('padding-top')) + parseInt($("#worldViewSendText").css('padding-bottom'));

    var deviceWidth = $(device).width();
    var deviceHeight = $(device).height();

    var top = parseInt($(device).position().top + deviceHeight/2.0) - eHeight/2.0;
    var left = parseInt($(device).position().left + deviceWidth/2.0) - eWidth/2.0;
    $("#worldViewSendText").css({ 
      opacity: 1.0, 
      top: top, 
      left: left
    });

    Meteor.setTimeout(function() {
      $("#worldViewSendText").css("opacity", 0);
    }, 2000);
  }, 1);
}

//
// "PUBLIC" METHODS
//

Template.deviceWorldView.show = function(animated) {
  if (animated === undefined) animated = true;

  //When the world view is shown, we remember the currently selected text snippet
  //in the document details if there is any.
  //We do that because a tap in the world view can clear the selection which means
  //we cannot retrieve it later
  if (Template.detailDocumentTemplate) {
    Session.set("worldViewSnippetToSend", Template.detailDocumentTemplate.currentlySelectedContent());
  }

  // if ($("#worldViewWrapper").css("display") !== "none") return;

  //Make sure the view is hidden properly before showing it
  Template.deviceWorldView.hide(false);

  $("#worldViewWrapper").css("display", "");

  var duration = animated ? 500 : 0;
  $("#worldViewWrapper").animate({
    top: "0px"
  }, duration);
  // $("#worldViewWrapper").slideUp(duration);
};

Template.deviceWorldView.hide = function(animated) {
  if (animated === undefined) animated = true;

  Session.set("worldViewSnippetToSend", undefined);

  // if ($("#worldViewWrapper").css("display") === "none") return;

  var duration = animated ? 500 : 0;
  $("#worldViewWrapper").animate({
    top: $(document).height()+"px"
  }, duration, function() {
    $("#worldViewWrapper").css("display", "none");
  });
  // $("#worldViewWrapper").slideDown(duration);
};

//
// EVENTS
//

Template.deviceWorldView.events({
  'click #closeButton, touchdown #closeButton': function() {
    Template.deviceWorldView.hide();
  }
});