if (Meteor.isServer) {
  Meteor.startup(function () {
    var Future = Npm.require('fibers/future');
    var fs = Npm.require('fs');

    Meteor.methods({

      startBenchmark: function(name, lux) {
        var future = new Future();

        var filename = 'benchmark_{0}.xml'.format(name);

        var start = '<benchmark name="{0}" lux="{1}">\r\n'.format(name, lux);

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
          logEntry += '    <sample n="{0}" timestamp="{1}" tracking-state="{2}" x="{3}" y="{4}" z="{5}" angle="{6}" />\r\n'.format(i, sample.timestamp, sample.state, sample.x, sample.y, sample.z, sample.angle);
        });

        logEntry += '  </spot>\r\n';

        var filename = 'benchmark_{0}.xml'.format(name);
        fs.appendFile(filename, logEntry, function (err) {
          if (err) throw err;
          future.return(true);
        });

        return future.wait();
      },
    });
  });
}
