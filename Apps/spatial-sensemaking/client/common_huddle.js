if (Meteor.isClient) {

  Huddle.on("showdocument", function(data) {
    var thisDevice = Session.get('thisDevice');
    if (data.target !== thisDevice.id) return;

    $.fancybox({
      type: "iframe",
      href: "/documentPopup/"+encodeURIComponent(data.documentID)+"/"+encodeURIComponent(Session.get("lastQuery")),
      autoSize: false,
      autoResize: false,
      height: "952px",
      width: "722px"
    });
  });

  Huddle.on("addtextsnippet", function(data) {
    var thisDevice = Session.get('thisDevice');
    if (data.target !== thisDevice.id) return;

    //TODO also insert source document and the device that sent the snippet
    Snippets.insert({ device: thisDevice.id, text: data.snippet });
    // console.warn("TODO, SHOULD ADD A SNIPPET: "+data.snippet);
  });

}