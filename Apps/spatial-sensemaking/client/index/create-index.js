if (Meteor.isClient) {
  Template.createIndex.events({
    'submit form': function(e, tmpl) {
      e.preventDefault();

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

          Meteor.call('addDocumentToIndex', file, function(err, result) {

            if (err) {
              throw err;
            }
            else {
              form.reset();
            }
          });
        });
      }
    },
  });
}
