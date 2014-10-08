Package.describe({
  summary: "HuddleOrbiter opens a web server and streams huddle data to connected clients.",
  version: "1.0.0"
});

Package.onUse(function(api) {
  api.versionsFrom('METEOR@0.9.3.1');
  api.export('HuddleOrbiter', 'server');
  api.addFiles('raedle:huddle-orbiter.js');
});

Package.onTest(function(api) {
  api.use('tinytest');
  api.use('raedle:huddle-orbiter');
  api.addFiles('raedle:huddle-orbiter-tests.js');
});

Npm.depends({
  websocket: "1.0.8"
})
