DeviceInfo = new Meteor.Collection("device-info");

DeviceInfo._upsert = function(id, changes) {
  var exists = !!(DeviceInfo.findOne({ _id:id }));

  if (exists === false) {
    var newDoc = { _id : id };
    DeviceInfo.insert(newDoc);
  }

  return DeviceInfo.update(id, changes);
};

if (Meteor.isClient) {
  Meteor.subscribe("device-info");
}

if (Meteor.isServer) {
  Meteor.startup(function() {

    Meteor.publish("device-info", function() {
      return DeviceInfo.find();
    });

    DeviceInfo.allow({
      insert: function() {
        return true;
      },
      update: function() {
        return true;
      },
      remove: function() {
        return true;
      },
    });
  });
}
