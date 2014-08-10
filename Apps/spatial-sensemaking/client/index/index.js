if (Meteor.isClient) {

  var logResult = function(err, result) {
    if (err) {
      console.error(err);
    }
    else {

      try {
        var message = JSON.parse(result);

        if (message.error) {
          console.error(message.error)
        }
        else if (message.acknowledged) {
          // console.log(message.acknowledged);
        }
        else {
          console.log(result);
        }
      }
      catch (e) {
        console.error(e);
      }
    }

    refreshIndices();
  };

  /**
   * Refreshes indices and reactively updates the user interface.
   */
  var refreshIndices = function() {
    Meteor.call('statsIndex', function(err, result) {

      if (err) {
        console.error(err);
      }
      else {
        try {
          var message = JSON.parse(result);

          // console.log(message);

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

  ///////////////////////////////////////////////
  // TEMPLATE 'index'
  ///////////////////////////////////////////////

  /**
   * Created function for template 'index'.
   */
  Template.index.created = function() {
    refreshIndices();
  };

  /**
   * Helpers for template 'index'.
   */
  Template.index.helpers({
    indices: function() {
      return Session.get("indices") || [];
    },
  });

  /**
   * Events for template 'index'.
   */
  Template.index.events({

    'click #index-create': function(e, tmpl) {
      var $indexName = tmpl.$('#index-name');
      var index = $indexName.val();
      Meteor.call("createIndex", index, logResult);
      $indexName.val("");
    },

    'keyup #index-name': function(e, tmpl) {
      if (e.keyCode == 13) {
        var $indexName = tmpl.$('#index-name');
        var index = $indexName.val();
        Meteor.call("createIndex", index, logResult);
        $indexName.val("");
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

      Meteor.call("enableAttachmentsForIndex", indexName, enabled, logResult);
    },

    'click .index-add-documents': function(e, tmpl) {
		  Session.set('indexInScope', this);
    },

    'click .glyphicon-pencil': function(e, tmpl) {
	    Session.set('indexInScope', this);
    }
  });

  ///////////////////////////////////////////////
  // TEMPLATE 'indexDeleteModal'
  ///////////////////////////////////////////////

  /**
   * Helpers for template 'indexDeleteModal'.
   */
  Template.indexDeleteModal.helpers({
  	name: function() {
  		return this.name;
  	},
  	indexInScope: function() {
  		return Session.get('indexInScope');
  	}
  });

  /**
   * Events for template 'indexDeleteModal'.
   */
  Template.indexDeleteModal.events({
  	'click .btn-index-delete': function(e, tmpl) {

      Meteor.call('deleteIndex', this.name, function(error) {
  			if (error) {
  				// optionally use a meteor errors package
  				if (typeof Errors === "undefined")
  					Log.error('Error: ' + error.reason);
  				else {
  					Errors.throw(error.reason);
  				}
  			}

        refreshIndices();
  			$("#index-delete-modal").modal("hide");
  		});
  	},
  });

  ///////////////////////////////////////////////
  // TEMPLATE 'indexAddDocumentsModal'
  ///////////////////////////////////////////////

  /**
   * Helpers for template 'indexAddDocumentsModal'.
   */
  Template.indexAddDocumentsModal.helpers({
    name: function() {
      return this.name;
    },
    indexInScope: function() {
      return Session.get('indexInScope');
    }
  });

  /**
   * Events for template 'indexAddDocumentsModal'.
   */
  Template.indexAddDocumentsModal.events({

    'click .btn-add-documents': function(e, tmpl) {
      e.preventDefault();

      var indexName = this.name;

      // Grab the file input control so we can get access to the selected files.
      var fileInput = tmpl.find('input[type=file]');

      // We'll assign each file in the loop to this variable.
      var file;

      for (var i = 0; i < fileInput.files.length; i++) {

        file = fileInput.files[i];

        // Read file into memory.
        FileInfo.read(file, function(err, file) {

          Meteor.call('addAttachmentToIndex', indexName, file, function(err, result) {

            if (err) {
              console.error(err);
            }
            else {
              logResult(err, result);
              $(fileInput).val("");
            }
            refreshIndices();
          });
        });
      }

      tmpl.$("#index-add-documents-modal").modal("hide");
    },
  });
}
