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
      path: '/search/:_query?/:_page?',
      waitOn : function() {
        return Meteor.subscribe('index-settings') && Meteor.subscribe('past-queries');
      },
      action : function() {
        if (this.ready()) {
          this.render();
        }
      }
    });

    this.route('snippets', {
      path: '/snippets'
    });

    this.route('task', {
      path: '/task'
    });
    this.route('accounts', {
      path: '/admin/accounts'
    });

    this.route('index', {
      path: '/admin/index'
    });

    // this.route('settings', {
    //   path: '/admin/settings'
    // });

    this.route('notFound', {
      path: '*'
    });
  });
}
