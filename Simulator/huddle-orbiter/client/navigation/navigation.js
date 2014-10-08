if (Meteor.isClient) {
  Template.navigation.helpers({
      // check if user is an admin
      isUser: function() {
          return Roles.userIsInRole(Meteor.user(), ["admin", "user"]);
      },
      // check if user is an admin
      isAdminUser: function() {
          return Roles.userIsInRole(Meteor.user(), ["admin"]);
      }
  });

  /**
   *
   */
  Template.navigation.connectedClients = function () {
    return Clients.find().count();
  };
}
