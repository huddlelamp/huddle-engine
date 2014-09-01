if (Meteor.isClient) {

  Session.setDefault("gridWidth", 600);
  Session.setDefault("gridHeight", 400);

  var benchmarkName = "default";
  var benchmarkDuration = 10000;
  var timerDuration = 5000;

  var checkForDeps = function(propName) {
    if (!this._deps[propName]) {
      this._deps[propName] = new Deps.Dependency();
    }
  };

  var addDependencyProperty = function(propName) {
    Object.defineProperty(Point.prototype, propName, {
      get: function() {
        checkForDeps.call(this, propName);
        this._deps[propName].depend();
        return this["_" + propName];
      },
      set: function(value) {
        checkForDeps.call(this, propName);
        this["_" + propName] = value;
        this._deps[propName].changed();
      }
    });
  };

  var Point = function(id, row, column, x, y) {
    this._deps = [];
    this._id = id;
    this._row = row;
    this._column = column;
    this._x = x;
    this._y = y;
    this.state = "not-sampled";
  };

  addDependencyProperty("id");
  addDependencyProperty("row");
  addDependencyProperty("column");
  addDependencyProperty("x");
  addDependencyProperty("y");
  addDependencyProperty("state");

  var gridData = [];

  // build points
  var columns = 2;
  var rows = 1;

  var gridWidth = Session.get("gridWidth");
  var gridHeight = Session.get("gridHeight");

  var pointSize = 50;

  var maxWidth = gridWidth - 1 * pointSize;
  var maxHeight = gridHeight - 1 * pointSize;

  var stepX = maxWidth / columns;
  var stepY = maxHeight / rows;

  var column;
  var row;
  for (row = 0; row < rows; row++) {
    for (column = 0; column < columns; column++) {
      var index = column + (columns * row);
      gridData[index] = new Point(index, row, column, pointSize + column * stepX, pointSize + row * stepY);
    }
  }

  var doSample = false;
  var samples = [];

  var huddle = Huddle.client("MyHuddle")
    .on("proximity", function(data) {
      if (!doSample) return;

      samples.push({
        timestamp: Date.now(),
        x: data.Location[0],
        y: data.Location[1],
        z: data.Location[2]
      });
    })
    .connect("huddle-orbiter.proxemicinteractions.org", 58629);
    // .connect("134.34.226.168", 4711);

  // counter starts at 0
  Session.setDefault("progress", 0);

  Meteor.call("startBenchmark", benchmarkName, function(err, result) {
    if (err) throw err;
    console.log(result);
  });

  var doSampling = function(point) {

    // set point as await sampling.
    point.state = "await-sampling";

    // reset timer.
    Session.set("timer", timerDuration);

    var interval = Meteor.setInterval(function() {
      var currentTimer = Session.get("timer") - 1000;

      if (currentTimer <= 0) {
        Meteor.clearInterval(interval);
        processSampling(point);
      }

      Session.set("timer", currentTimer);
    }, 1000);
  };

  var processSampling = function(point) {
    doSample = true;

    var updateSpeed = 100;
    var step = updateSpeed / (benchmarkDuration / updateSpeed);
    var p = Meteor.setInterval(function() {
      Session.set("progress", Session.get("progress") + step);
    }, updateSpeed);

    Meteor.setTimeout(function() {

      doSample = false;

      Meteor.clearInterval(p);
      Session.set("progress", 0);

      // console.log("Row: {0}, Column: {1}".format(point.row, point.column));
      // console.log(samples);

      Meteor.call("samplesBenchmark", benchmarkName, point.row, point.column, samples, function(err, result) {
        if (err) throw err;
        console.log(result);

        point.state = "sampled";
        $('#samplingViewModal').modal("hide");

        var completed = true;
        _.each(gridData, function(data, i) {
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
      samples.length = 0;
    }, benchmarkDuration);
  }

  Template.main.helpers({

    gridData: function() {
      return gridData;
    },

    gridWidth: function() {
      return Session.get("gridWidth");
    },

    gridHeight: function() {
      return Session.get("gridHeight");
    },
  });

  Template.main.events({
    'click, touchend circle': function(e, tmpl) {
      console.log(this);

      $('#samplingViewModal').modal("show");

      doSampling(this);
    },
  });
}

if (Meteor.isServer) {
  Meteor.startup(function () {
    // code to run on server at startup

    var Future = Npm.require('fibers/future');
    var fs = Npm.require('fs');

    Meteor.methods({

      startBenchmark: function(name) {
        var future = new Future();

        var filename = 'benchmark_{0}.xml'.format(name);

        var start = '<benchmark name="{0}">\r\n'.format(name);

        fs.appendFile(filename, start, function (err) {
          if (err) throw err;
          future.return(true);
        });

        return future.wait();
      },

      endBenchmark: function(name) {
        var future = new Future();

        var filename = 'benchmark_{0}.xml'.format(name);

        var start = '</benchmark>\r\n'.format(name);

        fs.appendFile(filename, start, function (err) {
          if (err) throw err;
          future.return(true);
        });

        return future.wait();
      },

      /**
       * Logs samples to a file.
       */
      samplesBenchmark: function(name, row, column, samples) {
        var future = new Future();

        var logEntry = '  <spot row="{0}" column="{1}">\r\n'.format(row, column);

        _.each(samples, function(sample, i) {
          logEntry += '    <sample n="{0}" timestamp="{1}" x="{2}" y="{3}" z="{4}" />\r\n'.format(i, sample.timestamp, sample.x, sample.y, sample.z);
        });

        logEntry += '  </spot>\r\n';

        var filename = 'benchmark_{0}.xml'.format(name);
        fs.appendFile(filename, logEntry, function (err) {
          if (err) throw err;

          console.log(__meteor_bootstrap__.serverDir);

          future.return("value");
        });

        return future.wait();
      },
    });
  });
}
