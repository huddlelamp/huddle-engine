if (Meteor.isClient) {

  Router.configure({
    layoutTemplate: 'layout',
    loadingTemplate: 'loading',
  });

  Router.map(function() {

    this.route('benchmarkSettings', {
      path: '/',
    });

    this.route('benchmarkSettings', {
      path: '/benchmark/settings',
    });

    this.route('benchmarkGrid', {
      path: '/benchmark/grid',
      onBeforeAction: function() {
        if (!Session.get("benchmarkName")) {
          Router.go("benchmarkSettings");
        }
      },
    });

    this.route('precisionCalculate', {
      path: '/precision/calculate',
    });

    this.route('notFound', {
      path: '*',
    });
  });
}
