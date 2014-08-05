if (Meteor.isServer) {
  Meteor.startup(function () {
    Meteor.methods({
      addDocumentToIndex: function(file) {
        ES.index.addFile(file);
      },
    });
  });
}
