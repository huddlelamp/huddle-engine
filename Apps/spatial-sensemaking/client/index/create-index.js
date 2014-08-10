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
          console.log(message.acknowledged);
        }
        else {
          console.log(result);
        }
      }
      catch (e) {
        console.error(e);
      }
    }
  };

  Template.createIndex.events({
    'submit form': function(e, tmpl) {
      e.preventDefault();

      var indexName = tmpl.$('#index-name').val();

      // Grab the file input control so we can get access to the selected files.
      var fileInput = tmpl.find('input[type=file]');

      // Grab the form so we can reset it after successful file upload.
      var form = e.currentTarget;

      // We'll assign each file in the loop to this variable.
      var file;

      for (var i = 0; i < fileInput.files.length; i++) {

        file = fileInput.files[i];

        // Read file into memory.
        FileInfo.read(file, function(err, file) {

          Meteor.call('addAttachmentToIndex', indexName, file, function(err, result) {

            if (err) {
              Errors.throw(err.reason);
            }
            else {
              logResult(err, result);
              form.reset();
            }
          });
        });
      }
    },

    'click #index-create': function(e, tmpl) {

      var index = tmpl.$('#index-name').val();

      Meteor.call("createIndex", index, logResult);
    },

    'click #index-delete': function(e, tmpl) {

      var index = tmpl.$('#index-name').val();

      Meteor.call("deleteIndex", index, logResult);
    },

    'click #index-stats': function(e, tmpl) {
      Meteor.call("statsIndex", logResult);
    },

    'click #index-attachments-enabled': function(e, tmpl) {

      var index = tmpl.$('#index-name').val();
      var enabled = tmpl.$('#index-attachments-enabled').prop('checked');

      console.log(enabled);

      Meteor.call("enableAttachmentsForIndex", index, enabled, logResult);
    },
  });
}
