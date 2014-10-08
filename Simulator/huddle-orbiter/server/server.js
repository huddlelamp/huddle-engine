if (Meteor.isServer) {
  Meteor.startup(function () {

    // Log environment settings to console on server startup.
    console.log("PORT: " + process.env.PORT);
    console.log("ROOT_URL: " + process.env.ROOT_URL);
    console.log("MONGO_URL: " + process.env.MONGO_URL);
    console.log("MAIL_URL: " + process.env.MAIL_URL);

    // if (Meteor.users.findOne("oreuNJgGqTBSF2mix"))
    //     Roles.addUsersToRoles("oreuNJgGqTBSF2mix", ["admin", "user", "developer"]);

    //// create a couple of roles if they don"t already exist (THESE ARE NOT NEEDED -- just for the demo)
    // if(!Meteor.roles.findOne({name: "secret"}))
    //     Roles.createRole("secret");
    //
    // if(!Meteor.roles.findOne({name: "double-secret"}))
    //     Roles.createRole("double-secret");

    var orbiter;

    Clients.remove({});
    Meteor.publish("clients-subscription", function() {
      return Clients.find({userId: this.userId});
    });

    Settings.remove({});
    Meteor.publish("settings-subscription", function() {
      return Settings.find();
    });
  });
}
