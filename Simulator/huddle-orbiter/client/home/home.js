if (Meteor.isClient) {
  Template.home.host = function() {
    var headerHost = headers.get('host').split(':');
    return headerHost[0];
  };

  Template.home.port = function() {
    var user = Meteor.user();

    if (typeof(user.settings) !== 'undefined') {
      var userSettings = user.settings;
      return userSettings.orbiterPort;
    }

    return null;
  };
}
