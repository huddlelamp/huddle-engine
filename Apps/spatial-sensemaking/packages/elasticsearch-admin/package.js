Package.describe({
  summary: "An extension for elastic search."
});

// Npm.depends({
//   'phantomjs': '1.9.7-12'
// });

Package.on_use(function(api) {
  api.use("standard-app-packages", ["client", "server"]);
  api.use("http", ["client", "server"]);
  api.use("iron-router", "client");
  api.use("underscore", "server");
  api.use("file-info", "server");

  if (api.export) {
    api.export("IndexSettings", ["client", "server"]);
    api.export("ElasticSearch", ["client"]);
  }

  api.add_files("lib/elasticsearch.js", "client");

  api.add_files("model/index-model.js", ["client", "server"]);

  api.add_files("client/admin/index/index.html", "client");
  api.add_files("client/admin/index/index.js", "client");
});
