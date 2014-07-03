Package.describe({
  summary: "Huddle Orbiter.",
  internal: false
});

Npm.depends({
    'websocket': '1.0.8'
});

Package.on_use(function (api) {
    api.add_files('huddle-orbiter.js', 'server');
  	api.export('HuddleOrbiter', 'server');
});