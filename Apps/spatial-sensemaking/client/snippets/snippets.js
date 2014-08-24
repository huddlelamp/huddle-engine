if (Meteor.isClient) {
  Template.snippets.snippets = function() {
    var thisDevice = Session.get('thisDevice');

    var snippets = Snippets.find({ device: thisDevice.id });
    return snippets.fetch();
  };

  Template.snippets.otherDevices = function() {
    return Session.get("otherDevices") || [];
  };

  Template.snippets.helpers({
    'thisDeviceBorderColorCSS': function() {
      return window.thisDeviceBorderColorCSS();
    },

    'deviceColorCSS': function() {
      return 'background-color: '+window.deviceColorCSS(this);
    },

    'deviceSizePositionCSS': function() {
      return window.deviceSizePositionCSS(this);
    },
  });
}
