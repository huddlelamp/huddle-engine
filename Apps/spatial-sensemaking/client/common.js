if (Meteor.isClient) {
  Template.navigation.helpers({
    "active": function(path) {
        var router = Router.current();
        if (router && router.route.name === path) {
          return "active";
        }
        return "";
      }
  });  


  Huddle.on("showdocument", function(data) {
    //If this page has no detail document template, we are screwed :-)
    if (!Template.detailDocumentTemplate) return;

    var thisDevice = Session.get('thisDevice');
    if (data.target !== thisDevice.id) return;

    ElasticSearch.get(data.documentID, function(err, result) {
      if (err) {
        console.error(err);
      }
      else {
        Template.detailDocumentTemplate.open(result.data); 
      }
    });
  });

  Huddle.on("addtextsnippet", function(data) {
    var thisDevice = Session.get('thisDevice');
    if (data.target !== thisDevice.id) return;

    //TODO also insert source document and the device that sent the snippet
    Snippets.insert({ device: thisDevice.id, text: data.snippet });
  });
}