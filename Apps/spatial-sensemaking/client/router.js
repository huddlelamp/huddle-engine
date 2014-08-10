Router.map(function(){

  this.route('index', {
    path: '/index'
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
