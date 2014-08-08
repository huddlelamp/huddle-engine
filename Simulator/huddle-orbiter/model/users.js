/**
 * Gets user.
 *
 * @param {string} id User id.
 */
getUser = function(id) {
  return Meteor.users.findOne({ _id: id });
}

if (Meteor.isClient) {
  Deps.autorun(function() {
    Meteor.subscribe("user-data");
  });
}

if (Meteor.isServer) {
  Meteor.publish("user-data", function() {
    if (!this.userId) return null;
    return Meteor.users.find({}, { fields:
      {
        _id: 1,
        emails: 1,
        roles: 1,
        profile: 1,
      }
    });
  });

  Meteor.users.allow({
    update: function(userId, user, fields, modifier) {
      if (userId != user._id) return false;

      if (fields.length == 1 && fields[0] == "settings") return true;

      return false;
    }
  });
}
