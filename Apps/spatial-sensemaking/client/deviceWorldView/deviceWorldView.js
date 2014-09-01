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
  var width = $("#worldViewWrapper").width() / this.ratio.x;
  var height = $("#worldViewWrapper").height() / this.ratio.y;
  var x = ($("#worldViewWrapper").width() - width) * this.topLeft.x;
  var y = ($("#worldViewWrapper").height() - height) * this.topLeft.y;
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

    Template.deviceWorldView.hide();

    if (text !== undefined && text.length > 0) {
      huddle.broadcast("addtextsnippet", { target: targetID, snippet: text } );
    } else {
      //If no selection was made, show the entire document
      var doc = Session.get("detailDocument");
      if (doc === undefined) return;
      huddle.broadcast("showdocument", { target: targetID, documentID: doc._id } );
    }
  },
});

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

  var duration = animated ? 500 : 0;
  $("#worldViewWrapper").animate({
    top: "0px"
  }, duration);
};

Template.deviceWorldView.hide = function(animated) {
  if (animated === undefined) animated = true;

  Session.set("worldViewSnippetToSend", undefined);

  var duration = animated ? 500 : 0;
  $("#worldViewWrapper").animate({
    top: $(document).height()+"px"
  }, duration);
};

//
// EVENTS
//

Template.deviceWorldView.events({
  'click #closeButton, touchdown #closeButton': function() {
    Template.deviceWorldView.hide();
  }
});