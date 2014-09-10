if (Meteor.isClient) {

  /**
   *
   */
  var createBenchmark = function(benchmarkName, benchmarkLux) {
    Meteor.call("startBenchmark", benchmarkName, benchmarkLux, function(err, result) {
      if (err) throw err;
      console.log(result);

      // Show grid after benchmark is created.
      Session.set("benchmarkName", benchmarkName);
      Router.go("benchmarkGrid");
    });
  }

  Template.benchmarkSettings.events({

    /**
     * Starts a new benchmark with the given benchmark name. On the server a
     * benchmark_<BENCHMARK_NAME>.xml will be created.
     */
    'click #start-benchmark-btn': function(e, tmpl) {
      var benchmarkName = tmpl.$('#benchmark-name').val();
      var benchmarkLux = tmpl.$('#benchmark-lux').val();
      createBenchmark(benchmarkName, benchmarkLux);
    },

    'keyup #benchmark-name': function(e, tmpl) {
      if (e.keyCode != 13) return;

      var benchmarkName = tmpl.$('#benchmark-name').val();
      var benchmarkLux = tmpl.$('#benchmark-lux').val();
      createBenchmark(benchmarkName, benchmarkLux);
    },
  });
}
