if (Meteor.isClient) {

  var isUserLoggedInWithRoles = function(roles, route) {

    // Need to load user like this because Meteor.user() might only have id.
    var userId = Meteor.userId();
    var user = getUser(userId);

    // console.log(userId + "," + user);

    if ((user == null ||
      !Roles.userIsInRole(user, roles)) && route) {
        Router.go(route);
    }

    if (user != null) {
      if (Roles.userIsInRole(user, roles)) {
        return true;
      }
    }
    return false;
  };

  var isAdminLoggedIn = function() {
    isUserLoggedInWithRoles(["admin"], "home");
  };

  var isUserLoggedIn = function() {
    isUserLoggedInWithRoles(["admin","user"], "home");
  };

  var routeHome = function() {
    console.log("route home");
    Router.go("home");
  };

  Router.configure({
    layoutTemplate: "layout",
    loadingTemplate: "loading"
  });

  // simple route with
  // name "about" that
  // matches "/about" and automatically renders
  // template "about"
  Router.map(function () {
    this.route("home", {
      path: "/"
    });

    this.route("orbit", {
      path: "/orbit",
      onBeforeAction: isUserLoggedIn,
    });

    this.route("settings", {
      path: "/settings",
      onBeforeAction: isUserLoggedIn,
    });

    this.route("faq", {
      path: "/faq"
    });

    this.route("help", {
      path: "/help"
    });

    this.route("accounts", {
      path: "/admin/accounts",
      onBeforeAction: isAdminLoggedIn,
    });

    this.route("notFound", {
      path: "*",
      // onAfterAction: routeHome,
    });
  });
}
