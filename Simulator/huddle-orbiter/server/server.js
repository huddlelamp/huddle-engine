if (Meteor.isServer) {
  Meteor.startup(function () {

    // if (Meteor.users.findOne("oreuNJgGqTBSF2mix"))
    //     Roles.addUsersToRoles("oreuNJgGqTBSF2mix", ['admin', 'user', 'developer']);

    //// create a couple of roles if they don't already exist (THESE ARE NOT NEEDED -- just for the demo)
    // if(!Meteor.roles.findOne({name: "secret"}))
    //     Roles.createRole("secret");
    //
    // if(!Meteor.roles.findOne({name: "double-secret"}))
    //     Roles.createRole("double-secret");

    Meteor.publish("user-data", function () {
      if (this.userId) {
        return Meteor.users.find({_id: this.userId},
                                 {
                                   fields: {
                                     settings: 1
                                    }
                                  });
      }
      else {
        this.ready();
      }
    });

    var orbiter;

    Clients.remove({});
    Meteor.publish("clients-subscription", function() {

      console.log(this.userId);

      return Clients.find({userId: this.userId});
    });

    Settings.remove({});
    Meteor.publish("settings-subscription", function() {
      return Settings.find();
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
