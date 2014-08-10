if (Meteor.isClient) {
  Router.map(function() {

    this.route('indexAdmin', {
      path: '/admin/index'
    });

    this.route('createIndex', {
      path: '/create-index'
    });

    this.route('searchIndex', {
      path: '/search-index'
    });

    this.route('home', {
      path: '*'
    });
  });
}
