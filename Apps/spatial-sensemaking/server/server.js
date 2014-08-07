if (Meteor.isServer) {
  Meteor.startup(function () {
    Meteor.methods({

      createIndex: function(index) {
        console.log("Create index " + index);
        ES.index.create(index);
      },

      deleteIndex: function(index) {
        console.log("Delete index " + index);
        ES.index.delete(index);
      },

      addDocumentToIndex: function(file) {
        ES.index.addFile("test", file);
      },
    });
  });
}
