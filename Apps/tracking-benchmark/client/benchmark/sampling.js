if (Meteor.isClient) {

  var settings = Meteor.settings.public;
  var benchmarkDuration = settings.benchmark.duration;
  var timerDuration = settings.timer.duration;

  // progress starts at 0
  Session.setDefault("progress", 0);

  doSampling = function(point, pointGrid) {

    // set point as await sampling.
    point.state = "await-sampling";

    // reset timer.
    Session.set("timer", timerDuration);

    var interval = Meteor.setInterval(function() {
      var currentTimer = Session.get("timer") - 1000;

      if (currentTimer <= 0) {
        Meteor.clearInterval(interval);
        processSampling(point, pointGrid);
      }

      Session.set("timer", currentTimer);
    }, 1000);
  }

  processSampling = function(point, pointGrid) {
    Session.set("huddleIsSampling", true);

    var updateSpeed = 500;
    var step = 100 / (benchmarkDuration / updateSpeed);

    // console.log("Step: {0}".format(step));

    Session.set("progress", step);
    var p = Meteor.setInterval(function() {
      Session.set("progress", Session.get("progress") + step);
    }, updateSpeed);

    Meteor.setTimeout(function() {
      Session.set("huddleIsSampling", false);

      Meteor.clearInterval(p);
      Session.set("progress", 0);

      var benchmarkName = Session.get("benchmarkName");
      var samples = Session.get("huddleSamples");

      Meteor.call("samplesBenchmark", benchmarkName, point.row, point.column, samples, function(err, result) {
        if (err) throw err;
        console.log(result);

        point.state = "sampled";
        $('#samplingViewModal').modal("hide");

        var completed = true;
        _.each(pointGrid, function(data, i) {
          if (data.state != "sampled") {
            completed = false;
          }
        });

        if (completed) {
          Meteor.call("endBenchmark", benchmarkName, function(err, result) {
            if (err) throw err;
            console.log(result);
          });
        }
      });

      // Reset huddle samples.
      Session.set("huddleSamples", []);

    }, benchmarkDuration);
  }
}
