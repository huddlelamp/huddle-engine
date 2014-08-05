Package.describe({
  summary: "Simple file uploading for Meteor.",
  internal: false
});

Package.on_use(function (api) {
  api.use(["underscore", "ejson"], ["client", "server"]);
  api.add_files(["file-info.js"], ["client", "server"]);
  api.export("FileInfo", ["client", "server"]);
});
