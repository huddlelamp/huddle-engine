DocumentMeta = new Meteor.Collection("document-meta");

DocumentMeta._upsert = function(id, changes) {
  var exists = !!(DocumentMeta.findOne({ _id:id }));

  if (exists === false) {
    var newDoc = {
      _id       : id,
      favorited : false
    };
    DocumentMeta.insert(newDoc);
  } 
  
  return DocumentMeta.update(id, changes);
};

if (Meteor.isClient) {
  Meteor.subscribe("document-meta");
}

if (Meteor.isServer) {
  Meteor.startup(function() {

    Meteor.publish("document-meta", function() {
      return DocumentMeta.find();
    });

    DocumentMeta.allow({
      insert: function () {
        return true;
      },
      update: function () {
        return true;
      },
      remove: function () {
        return true;
      },
    });
  });
}
