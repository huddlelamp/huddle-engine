IndexSettings = new Meteor.Collection("index-settings");

IndexSettings.getActiveIndex = function() {
  var settings = IndexSettings.findOne({ name: "default" });

  // Return default index set in config/settings.json
  if (!settings || !settings.index) return Meteor.settings.public.elasticSearch.index;

  return settings.index;
};

IndexSettings.setActiveIndex = function(name) {
  var settings = IndexSettings.findOne({ name: "default" });

  var id;
  if (!settings) {
    id = IndexSettings.insert({
      name: "default"
    });
  }
  else {
    id = settings._id;
  }

  IndexSettings.update({ _id: id }, { $set: {
    index: name
  }});
};

if (Meteor.isClient) {
  Meteor.subscribe("index-settings");
}

if (Meteor.isServer) {
  Meteor.startup(function() {

    Meteor.publish("index-settings", function() {
      return IndexSettings.find();
    });

    IndexSettings.allow({
      insert: function (userId, doc) {
        return true;
      },
      update: function (userId, doc, fields, modifier) {
        return true;
      },
      remove: function (userId, doc) {
        return true;
      },
    });
  });
}
