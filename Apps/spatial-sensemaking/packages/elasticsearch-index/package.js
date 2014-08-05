Package.describe({
  summary: "An extension for elastic search."
});

// Npm.depends({
//   'phantomjs': '1.9.7-12'
// });

Package.on_use(function(api) {
  api.use(["underscore", "file-info", "elasticsearch"], ["server"]);
  api.add_files(["elasticsearch-index.js"], ["server"]);
	api.export("ESI", ["server"]);
});
