if (Meteor.isServer) {

  var addRoleIfNotExist = function(role) {
    if(!Meteor.roles.findOne({ name: role }))
        Roles.createRole(role);
  };

  Meteor.startup(function() {

    addRoleIfNotExist("admin");
    addRoleIfNotExist("user");

    // Support for playing D&D: Roll 3d6 for dexterity
    Accounts.onCreateUser(function(options, user) {

      // Set empty profile.
      if (!user.profile) {
        user.profile = { };
      }

      // We still want the default hook's 'profile' behavior.
      if (options.profile) {
        user.profile = options.profile;
      }

      // set default user color
      // user.profile.color = 'pink';

      // add default role user to new user
      user.roles = ['user'];

      // set first user as admin
      if (Meteor.users.find().count() < 1)
        user.roles = ['admin'];

      return user;
    });
  });
}
