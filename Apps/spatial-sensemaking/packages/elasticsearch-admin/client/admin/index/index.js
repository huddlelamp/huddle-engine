if (Meteor.isClient) {

  /**
   * Check if index server is alive.
   */
  // Meteor.setInterval(function() {
  //   ElasticSearch.ping(function(err, result) {
  //     if (err) {
  //       Session.set("index-server-status", "label-danger");
  //     }
  //     else {
  //       Session.set("index-server-status", "label-success");
  //     }
  //   });
  // }, 1000);

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
          tmpl.$('#index-add-documents-progressbar').css({
            width: '0%'
          }).
          html('');
          var $fileInput = tmpl.$('input[type=file]');
          $fileInput.val("");
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

    pingIndexServer: function() {
      return Session.get("index-server-status");
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

    'click .index-add-documents': function(e, tmpl) {
		  Session.set('indexInScope', this);
    },

    'click .glyphicon-pencil': function(e, tmpl) {
	    Session.set('indexInScope', this);
    }
  });

  ///////////////////////////////////////////////
  // TEMPLATE 'elasticSearchAdminTableRow'
  ///////////////////////////////////////////////

  /**
   * Helpers for template 'elasticSearchAdminTableRow'.
   */
  Template.elasticSearchAdminTableRow.helpers({
    'isIndexActive': function(name) {
      var activeIndex = IndexSettings.getActiveIndex();
      return activeIndex === name;
    },

    'isAttachmentsEnabled': function(name) {
      ElasticSearch.getMapping(name, function(err, result) {
        var mapping = result.data;

        var isAttachmentsEnabled = false;
        if (mapping.hasOwnProperty(name)) {
          var m = mapping[name];
          isAttachmentsEnabled = (m.mappings && m.mappings.attachment);
        }

        Session.set(name + "Mapping", isAttachmentsEnabled);
      });
      return Session.get(name + "Mapping");
    },
  });

  /**
   * Events for template 'elasticSearchAdminTableRow'.
   */
  Template.elasticSearchAdminTableRow.events({
    'click .active-index': function(e, tmpl) {
      IndexSettings.setActiveIndex(this.name);
    },

    'click .index-attachments-enabled': function(e, tmpl) {
      var indexName = this.name;

      var selector = 'input[type=checkbox][index-name="' + indexName + '"]';
      var enabled = tmpl.find(selector).checked;

      if (enabled) {
        ElasticSearch.putMapping(indexName, {
          attachment: {
            properties: {
              file: {
                type: "attachment",
                fields: {
                  file: { term_vector: "with_positions_offsets", store: "yes" },
                  title: { store: "yes" },
                  date: { store: "yes" },
                  author: { store: "yes" },
                  keywords: { store: "yes" },
                  content_type: { store: "yes" },
                  content_length: { store: "yes" },
                  language: { store: "yes" },
                }
              }
            }
          }
        }, function(err, result) {
          if (err) {
            console.log(err);
          }
          else {
            Session.set(indexName + "Mapping", true);
          }
        });
      }
      else {
        ElasticSearch.deleteMapping(indexName, function(err, result) {
          if (err) {
            console.log(err);
          }
          else {
            Session.set(indexName + "Mapping", false);
          }
        });
      }
    },
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
