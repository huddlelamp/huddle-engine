Template.deviceWorldView.rendered = function() {
  $("#openWorldView").popover({
    trigger   : "manual",
    placement : "top",
    content   : $("#worldViewWrapper"),
    container : "body",
    html      : true,
  });//
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
  // var width  = $("#worldViewWrapper").width() / this.ratio.x;
  // var height = $("#worldViewWrapper").height() / this.ratio.y;
  // var x      = ($("#worldViewWrapper").width()) * this.topLeft.x;
  // var y      = ($("#worldViewWrapper").height()) * this.topLeft.y;
  
  var width = (1/this.ratio.x)*100.0;
  var height = (1/this.ratio.y)*100.0;
  var x = this.topLeft.x*100.0;
  var y = this.topLeft.y*100.0;

  return 'width: '+width+'%; height: '+height+'%; top: '+y+'%; left: '+x+'%;';
};

Template.deviceWorldView.thisDevice = function() {
  return Session.get("thisDevice") || undefined;
};

Template.deviceWorldView.otherDevices = function() {
  return Session.get("otherDevices") || [];
};

Template.deviceWorldView.events({
  'touchend #openWorldView': function() {
    Session.set("worldViewSnippetToSend", Template.detailDocumentTemplate.currentlySelectedContent());
  },

  'click #openWorldView': function(e) {
    Template.deviceWorldView.show();
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
    var sourcedocID = Session.get("worldViewSnippetDoc");

    var doc = Session.get("detailDocument");
    if (sourcedocID === undefined) {
      if (doc !== undefined) sourcedocID = doc._id;
    }
    if (text !== undefined && text.length > 0 && sourcedocID !== undefined) {
      //If a text selection exists, send it
      huddle.broadcast("addtextsnippet", { target: targetID, doc: sourcedocID, snippet: text } );
      // pulseDevice(this);
      showSendConfirmation($("#openWorldView"), "The selected text was sent to the device.");

      var thisDevice = Session.get('thisDevice');
      var actionSource = (Router.current().route.name === "searchIndex") ? "detailDocument" : "snippets";
      Logs.insert({
        timestamp       : Date.now(),
        route           : Router.current().route.name,
        deviceID        : thisDevice.id,  
        actionType      : "shareSnippet",
        actionSource    : actionSource, //must be, only source for sharesnippet
        actionSubsource : "worldView",
        targetDeviceID  : targetID,
        documentID      : sourcedocID,
        snippet         : text,
      });

      var remove = Session.get("worldViewSnippetRemove");
      if (remove !== undefined && remove !== false) {
        Snippets.remove({_id : remove});
      }
      Session.set("worldViewSnippetRemove", undefined);
      Session.set("worldViewSnippetDoc", undefined);
    } else {
      //If no selection was made but a document is open, send that
      if (doc !== undefined) {
        huddle.broadcast("showdocument", { target: targetID, documentID: doc._id } );
        // pulseDevice(this);
        showSendConfirmation($("#openWorldView"), "The document "+doc._id+" is displayed on the device.");

        var thisDevice = Session.get('thisDevice');
        Logs.insert({
          timestamp       : Date.now(),
          route           : Router.current().route.name,
          deviceID        : thisDevice.id,  
          actionType      : "shareDocument",
          actionSource    : "detailDocument",
          actionSubsource : "worldView",
          targetDeviceID  : targetID,
          documentID      : doc._id
        });
      } else {
        //If no document is open but a query result is shown, send that
        var lastQuery = Session.get('lastQuery');
        var lastQueryPage = Session.get('lastQueryPage');
        var route = Router.current().route.name;
        if (lastQuery !== undefined && route === "searchIndex") {
          // huddle.broadcast("dosearch", {target: targetID, query: lastQuery, page: lastQueryPage });
          // pulseDevice(this);
          // 
          huddle.broadcast("go", {
            target: targetID,
            template: "searchIndex",
            params: {
              _query: lastQuery,
              _page: lastQueryPage
            }
          });
          showSendConfirmation($("#openWorldView"), "Search results were sent to the device.");

          var thisDevice = Session.get('thisDevice');
          Logs.insert({
            timestamp      : Date.now(),
            route          : Router.current().route.name,
            deviceID       : thisDevice.id,  
            actionType     : "shareResults",
            actionSource   : "search",
            actionSubsource: "worldView",
            targetDeviceID : targetID,
            query          : lastQuery,
            page           : lastQueryPage
          });
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