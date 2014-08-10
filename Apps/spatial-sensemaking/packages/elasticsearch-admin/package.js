Package.describe({
  summary: "An extension for elastic search."
});

// Npm.depends({
//   'phantomjs': '1.9.7-12'
// });

Package.on_use(function(api) {
  api.use("standard-app-packages", ["client", "server"]);
  api.use("iron-router", "client");
  api.use("underscore", "server");
  api.use("file-info", "server");
  api.use("elasticsearch", "server");

  api.add_files("client/admin/index/index.html", "client");
  api.add_files("client/admin/index/index.js", "client");

  api.add_files("server/elasticsearch-admin.js", "server");
  api.add_files("server/methods.js", "server");

  api.export("ESI", ["server"]);
});
