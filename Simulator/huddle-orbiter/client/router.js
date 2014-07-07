if (Meteor.isClient) {

  Router.configure({
    layoutTemplate: 'layout',
    loadingTemplate: 'loading'
  });

  // simple route with
  // name 'about' that
  // matches '/about' and automatically renders
  // template 'about'
  Router.map(function () {
    this.route('orbit', {
      path: "/orbit"
    });

    this.route('settings', {
      path: "/settings"
    });

    this.route('notFound', {
      path: "*"
    })
  });
}
