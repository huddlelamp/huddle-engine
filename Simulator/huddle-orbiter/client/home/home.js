if (Meteor.isClient) {
  Template.exampleUsage.host = function() {
    var headerHost = headers.get('host').split(':');
    return headerHost[0];
  };

  Template.exampleUsage.port = function() {
    var user = Meteor.user();

    if (typeof(user.settings) !== 'undefined') {
      var userSettings = user.settings;
      return userSettings.orbiterPort;
    }

    return null;
  };

  Template.exampleUsage.rendered = function() {
    $('pre code').each(function(i, block) {
      hljs.highlightBlock(block);
    });
  };
}
