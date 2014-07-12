if (Meteor.isServer) {
  Meteor.startup(function() {
    Clients.allow({
      insert: function (userId, doc) {
        // the user must be logged in, and the document must be owned by the user
        // return doc.userId == userId;
        return true;
      },
      update: function (userId, doc, fields, modifier) {
        // can only change your own documents
        // return doc.userId == userId;
        return true;
      },
      remove: function (userId, doc) {
        // can only remove your own documents
        // return doc.userId == userId;
        return true;
      },
    });

    Settings.allow({
      insert: function (userId, doc) {
        // the user must be logged in, and the document must be owned by the user
        // return doc.userId == userId;
        return true;
      },
      update: function (userId, doc, fields, modifier) {
        // can only change your own documents
        // return doc.userId == userId;
        return true;
      },
      remove: function (userId, doc) {
        // can only remove your own documents
        // return doc.userId == userId;
        return true;
      },
    });
  });
}
