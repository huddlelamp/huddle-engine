if (Meteor.isClient) {

  /**
   * Refreshes indices and reactively updates the user interface.
   */
  var refreshIndices = function() {
    ElasticSearch.stats(function(err, result) {

      if (err) {
        console.error(err);
      }
      else {
        try {
          var message = result.data;

          var indices = [];
          for (var name in message.indices) {
            indices.push({
              name: name,
              primaries: message.indices[name].primaries,
              total: message.indices[name].total
            });
          }

          Session.set("indices", indices);
        }
        catch (e) {
          console.error(e);
        }
      }
    });
  };

  /**
   * Adds an attachment to the index.
   */
  var addAttachmentToIndex = function(name, files, count, processed, tmpl) {
    ElasticSearch.addAttachment(name, files[processed], function(err, result) {
      if (err) {
        console.error(err);
      }
      else {
        // console.log(result);
      }

      ++processed;
      var progress = parseInt((processed / count) * 100);

      tmpl.$('#index-add-documents-progressbar')
        .css({
          width: progress + "%"
        })
        .html(processed + ' of ' + count + ' files uploaded (' + progress + '%)');

      if (count > processed) {
        addAttachmentToIndex(name, files, count, processed, tmpl);
      }
      else {
        Meteor.setTimeout(function() {
          refreshIndices();
          tmpl.$('#index-add-documents-modal').modal('hide');
        }, 1000);
      }
    });
  };

  ///////////////////////////////////////////////
  // TEMPLATE 'elasticSearchAdmin'
  ///////////////////////////////////////////////

  /**
   * Created function for template 'elasticSearchAdmin'.
   */
  Template.elasticSearchAdmin.created = function() {
    refreshIndices();
  };

  /**
   * Helpers for template 'elasticSearchAdmin'.
   */
  Template.elasticSearchAdmin.helpers({
    indices: function() {
      return Session.get("indices") || [];
    },
  });

  /**
   * Events for template 'elasticSearchAdmin'.
   */
  Template.elasticSearchAdmin.events({

    'click #index-create': function(e, tmpl) {
      var $indexName = tmpl.$('#index-name');
      var index = $indexName.val();

      ElasticSearch.createIndex(index, function(err, result) {
        refreshIndices();
        $indexName.val("");
      });
    },

    'keyup #index-name': function(e, tmpl) {
      if (e.keyCode == 13) {
        var $indexName = tmpl.$('#index-name');
        var index = $indexName.val();

        ElasticSearch.createIndex(index, function(err, result) {
          refreshIndices();
          $indexName.val("");
        });
      }
    },

    'click .refresh-indices': function(e, tmpl) {
      refreshIndices();
    },

    'click .index-delete': function(e, tmpl) {
      Session.set('indexInScope', this);
    },

    'click .index-attachments-enabled': function(e, tmpl) {

      var indexName = this.name;

      var selector = 'input[type=checkbox][index-name="' + indexName + '"]';
      var enabled = tmpl.find(selector).checked;

      ElasticSearch.enableAttachments(indexName, function(err, result) {
        console.log(result);
      });
    },

    'click .index-add-documents': function(e, tmpl) {
		  Session.set('indexInScope', this);
    },

    'click .glyphicon-pencil': function(e, tmpl) {
	    Session.set('indexInScope', this);
    }
  });

  ///////////////////////////////////////////////
  // TEMPLATE 'elasticSearchAdminDeleteIndexModal'
  ///////////////////////////////////////////////

  /**
   * Helpers for template 'elasticSearchAdminDeleteIndexModal'.
   */
  Template.elasticSearchAdminDeleteIndexModal.helpers({
  	name: function() {
  		return this.name;
  	},
  	indexInScope: function() {
  		return Session.get('indexInScope');
  	}
  });

  /**
   * Events for template 'elasticSearchAdminDeleteIndexModal'.
   */
  Template.elasticSearchAdminDeleteIndexModal.events({
  	'click .btn-index-delete': function(e, tmpl) {

      ElasticSearch.deleteIndex(this.name, function(err, result) {
        refreshIndices();
        $("#index-delete-modal").modal("hide");
      });
  	},
  });

  ///////////////////////////////////////////////
  // TEMPLATE 'elasticSearchAdminAddDocumentsToIndexModal'
  ///////////////////////////////////////////////

  /**
   * Helpers for template 'elasticSearchAdminAddDocumentsToIndexModal'.
   */
  Template.elasticSearchAdminAddDocumentsToIndexModal.helpers({
    name: function() {
      return this.name;
    },
    indexInScope: function() {
      return Session.get('indexInScope');
    }
  });

  /**
   * Events for template 'elasticSearchAdminAddDocumentsToIndexModal'.
   */
  Template.elasticSearchAdminAddDocumentsToIndexModal.events({

    'click .btn-add-documents': function(e, tmpl) {
      e.preventDefault();

      var indexName = this.name;

      // Grab the file input control so we can get access to the selected files.
      var fileInput = tmpl.find('input[type=file]');

      // We'll assign each file in the loop to this variable.
      var file;
      var files = fileInput.files;
      var count = files.length;
      var processed = 0;

      if (files.length > 0) {
        addAttachmentToIndex(indexName, files, count, 0, tmpl);
      }
    },
  });
}
