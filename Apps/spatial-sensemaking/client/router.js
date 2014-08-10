if (Meteor.isClient) {

  Router.configure({
    layoutTemplate: 'layout',
    loadingTemplate: 'loading'
  });

  Router.map(function() {

    this.route('searchIndex', {
      path: '/'
    });

    this.route('searchIndex', {
      path: '/search'
    });

    this.route('task', {
      path: '/task'
    });

    this.route('index', {
      path: '/admin/index'
    });

    this.route('accounts', {
      path: '/admin/accounts'
    });

    this.route('notFound', {
      path: '*'
    });
  });
}
