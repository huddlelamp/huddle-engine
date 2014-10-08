if (Meteor.isServer) {
  Meteor.startup(function() {

    // Support for playing D&D: Roll 3d6 for dexterity
    Accounts.onCreateUser(function(options, user) {
      // We still want the default hook"s "profile" behavior.
      if (options.profile)
        user.profile = options.profile;

      // add default role user to new user
      user.roles = ["user"];

      // set first user as admin
      if (Meteor.users.find().count() < 1)
        user.roles = ["admin"];

      return user;
    });
  });
}
