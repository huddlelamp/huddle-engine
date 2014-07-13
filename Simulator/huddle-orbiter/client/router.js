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
    this.route('home', {
      path: "/"
    });

    this.route('orbit', {
      path: "/orbit"
    });

    this.route('settings', {
      path: "/settings"
    });

    this.route('faq', {
      path: "/faq"
    });

    this.route('help', {
      path: "/help"
    });

    this.route('accounts', {
      path: "/admin/accounts"
    });

    this.route('notFound', {
      path: "*"
    })
  });
}
