if (Meteor.isServer) {
  Meteor.startup(function () {
    Meteor.methods({

      /**
       * Creates an elastic search index.
       *
       * @param {string} index Index name.
       */
      createIndex: function(index) {
        return ES.index.create(index);
      },

      /**
       * Deletes an elastic search index.
       *
       * @param {string} index Index name.
       */
      deleteIndex: function(index) {
        return ES.index.delete(index);
      },

      /**
       * Get index stats.
       */
      statsIndex: function() {
        return ES.index.stats();
      },

      /**
       * Enables attachments for index.
       *
       * @param {string} index Index name.
       * @param {boolean} enable Enable index (true/false).
       */
      enableAttachmentsForIndex: function(index, enable) {
        return ES.index.enableAttachments(index, enable);
      },

      /**
       * Adds an attachment to index.
       *
       * @param {string} index Index name.
       * @param {FileInfo} file File info (attachment file).
       */
      addAttachmentToIndex: function(index, file) {
        return ES.index.addAttachment(index, file);
      },

      /**
       * Searches the index using the given query.
       *
       * @param {string} index Index name.
       * @param {string} query Search query.
       */
      searchIndex: function(index, query) {
        return ES.index.search(index, query);
      },
    });

    // ES.startServer();
  });
}
