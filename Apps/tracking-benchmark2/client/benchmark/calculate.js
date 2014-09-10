if (Meteor.isClient) {

  Session.setDefault("trackingImageWidth", 283);
  Session.setDefault("trackingImageHeight", 159);

  Session.setDefault("trackingAreaWidth", 1020);
  Session.setDefault("trackingAreaHeight", 570);

  Session.setDefault("groundTruthMeasure", 100);

  Session.setDefault("measurementUnit", "mm");

  Session.setDefault("conditions", []);

  /**
   *
   */
  var getMeasure = function(type, unit, x, y) {
    return {
      type: type,
      unit: unit,
      x: x,
      y: y
    };
  };

  /**
   *
   */
  var getPrecisionMeasure = function(type, unit, x, y, angle, reliability) {
    return {
      type: type,
      unit: unit,
      x: x,
      y: y,
      angle: angle,
      reliability: reliability,
    };
  };

  /**
   * @param {Object} benchmark Benchmark object.
   */
  var calculatePrecision = function(benchmark) {
    var trackingAreaWidth = Session.get("trackingAreaWidth");
    var trackingAreaHeight = Session.get("trackingAreaHeight");

    var spots = benchmark.spots;

    var meansX = [];
    var meansY = [];
    var meansAngle = [];

    var sdsX = [];
    var sdsY = [];
    var sdsAngle = [];

    var sampleCount = 0;
    var validSampleCount = 0;

    $.each(spots, function(i, spot) {
      var samples = spot.samples;

      var xs = [];
      var ys = [];
      var angles = [];

      sampleCount += samples.length;

      $.each(samples, function(i, sample) {
        var timestamp = sample.timestamp;
        var trackingState = sample.trackingState;
        var x = sample.x;
        var y = sample.y;
        var z = sample.z;
        var angle = sample.angle;

        // not a valid sample because device was not tracking.
        if (trackingState == 0) {
          return;
        }

        ++validSampleCount;

        xs.push(x);
        ys.push(y);
        angles.push(angle);
      });

      var meanX = ss.mean(xs);
      var meanY = ss.mean(ys);
      var meanAngle = ss.mean(angles);

      var sdX = ss.standard_deviation(xs);
      var sdY = ss.standard_deviation(ys);
      var sdAngle = ss.standard_deviation(angles);

      meansX.push(meanX);
      meansY.push(meanY);
      meansAngle.push(meanAngle);

      sdsX.push(sdX);
      sdsY.push(sdY);
      sdsAngle.push(sdAngle);

      spot.meanX = meanX;
      spot.meanY = meanY;
      spot.meanAngle = meanAngle;
      spot.sdX = sdX;
      spot.sdY = sdY;
      spot.sdAngle = sdAngle;
    });

    // console.log("Precision MAX: X={0}, Y={1} [in px]".format(ss.max(sdsX) * trackingImageWidth, ss.max(sdsY) * trackingImageHeight));

    // var meanX = (ss.max(meansX) * trackingAreaWidth);
    // var meanY = (ss.max(meansY) * trackingAreaHeight);

    var sdX = (ss.max(sdsX) * trackingAreaWidth);
    var sdY = (ss.max(sdsY) * trackingAreaHeight);
    var sdAngle = ss.max(sdsAngle);
    var reliability = validSampleCount / sampleCount;

    return [
      // getMeasure("Precision Mean (Max)", "cm", meanX, meanY),
      getPrecisionMeasure("Precision Max (of all SD)", "mm", sdX, sdY, sdAngle, reliability),
      getPrecisionMeasure("Precision Mean (of all SD)", "mm", ss.mean(sdsX) * trackingAreaWidth, ss.mean(sdsY) * trackingAreaHeight, ss.mean(sdsAngle), reliability)
    ];
  };

  var calculateAccuracy = function(benchmark) {

    var trackingAreaWidth = Session.get("trackingAreaWidth");
    var trackingAreaHeight = Session.get("trackingAreaHeight");
    var groundTruth = Session.get("groundTruthMeasure");
    var measurementUnit = Session.get("measurementUnit");

    var spots = benchmark.spots;

    var accuraciesX = [];
    var accuraciesY = [];

    $.each(spots, function(i, spot) {

      var x1 = spot.meanX * trackingAreaWidth;
      var y1 = spot.meanY * trackingAreaHeight;

      // console.log("{0}:{1}".format(a1, b1));

      var neighborSpots = [];

      var accX = [];
      var accY = [];

      $.each(benchmark.spots, function(j, nSpot) {

        // Get all neighboring spots to calculate distance.
        if (isValidNeighborSpot(spot, nSpot)) {
          neighborSpots.push(nSpot);

          if (spot.column != nSpot.column) {
            var x2 = nSpot.meanX * trackingAreaWidth;
            var dx = Math.abs(Math.abs(x1 - x2) - groundTruth);

            console.log("Benchmark: {0} | Accuracy Error for spots columns {1}-{2}: {3} {4}".format(benchmark.name, spot.column, nSpot.column, dx.toFixed(2), measurementUnit));

            accX.push(dx);

            accuraciesX.push(dx);
          }
          else if (spot.row != nSpot.row) {
            var y2 = nSpot.meanY * trackingAreaHeight;
            var dy = Math.abs(Math.abs(y1 - y2) - groundTruth);

            console.log("Benchmark: {0} | Accuracy Error for spots rows    {1}-{2}: {3} {4}".format(benchmark.name, spot.row, nSpot.row, dy.toFixed(2), measurementUnit));

            accY.push(dy);

            accuraciesY.push(dy);
          }
        }
      });

      // console.log("{0}:{1} | X={2}, Y={3}".format(spot.column, spot.row, ss.max(accX), ss.max(accY)));

      // console.log(neighborSpots.length);
    });

    var meanX = ss.mean(accuraciesX);
    var meanY = ss.mean(accuraciesY);

    var minX = ss.min(accuraciesX);
    var minY = ss.min(accuraciesY);

    var maxX = ss.max(accuraciesX);
    var maxY = ss.max(accuraciesY);

    var sdX = ss.standard_deviation(accuraciesX);
    var sdY = ss.standard_deviation(accuraciesY);

    return [
      getMeasure("Accuracy Mean", "mm", meanX, meanY),
      getMeasure("Accuracy Min", "mm", minX, minY),
      getMeasure("Accuracy Max", "mm", maxX, maxY),
      getMeasure("Accuracy SD", "mm", sdX, sdY)
    ];
  };

  /**
   * Parses an xml file at the given url into a benchmark JSON file.
   *
   * @param {string} url Url to benchmark xml file.
   */
  var parseBenchmark = function(url, callback) {
    var benchmark = {
      name: "",
      lux: 0,
      spots: [],
    };

    HTTP.get(url, function(err, result) {
      if (err) throw err;

      var xmlDocument = $.parseXML(result.content);

      var $benchmark = $(xmlDocument).find("benchmark");

      benchmark.name = $benchmark.attr("name");
      benchmark.lux = $benchmark.attr("lux");

      var $spots = $benchmark.find("spot");
      $spots.each(function(i, xmlSpot) {
        var $spot = $(xmlSpot);
        var spot = {
          column: parseInt($spot.attr("column")),
          row: parseInt($spot.attr("row")),
          samples: [],
          valid: false,
        }

        var $samples = $spot.find("sample");

        var hasValidSample = false;
        $samples.each(function(i, xmlSample) {
          var $sample = $(xmlSample);

          var sample = {
            timestamp: new Date(parseInt($sample.attr("timestamp"))),
            trackingState: parseInt($sample.attr("tracking-state")),
            x: parseFloat($sample.attr("x")),
            y: parseFloat($sample.attr("y")),
            z: parseFloat($sample.attr("z")),
            angle: parseInt($sample.attr("angle"))
          };

          if (sample.trackingState > 0)
            hasValidSample = true;

          spot.samples.push(sample);
        });

        if (spot.samples.length > 0)
          spot.valid = hasValidSample;

        benchmark.spots.push(spot);
      });

      if (typeof callback === 'function') {
        callback(benchmark);
      }
    });
  };

  /**
   * Checks if spot1 and spot2 are neighbors.
   *
   * @param {Object} spot1 Spot1.
   * @param {Object} spot2 Spot2.
   */
  var isValidNeighborSpot = function(spot1, spot2) {
    // var isNColumn = (spot2.column - 1 == spot1.column || spot2.column + 1 == spot1.column)
    // var isNRow = (spot2.row - 1 == spot1.row || spot2.row + 1 == spot1.row);
    //
    // var isSColumn = (spot2.column == spot1.column);
    // var isSRow = (spot2.row == spot1.row);

    // [N1] [N2] [N3]
    // [N4] [N5] [N6]
    // [N7] [N8] [N9]

    var isN1 = false; //var isN1 = (spot2.column + 1 == spot1.column && spot2.row + 1 == spot1.row);
    var isN2 = (spot2.column == spot1.column && spot2.row + 1 == spot1.row);
    var isN3 = false; //var isN3 = (spot2.column - 1 == spot1.column && spot2.row + 1 == spot1.row);
    var isN4 = (spot2.column + 1 == spot1.column && spot2.row == spot1.row);
    var isN5 = false; //var isN5 = (spot2.column == spot1.column && spot2.row == spot1.row); // same spot
    var isN6 = (spot2.column - 1 == spot1.column && spot2.row == spot1.row);
    var isN7 = false; //var isN7 = (spot2.column + 1 == spot1.column && spot2.row - 1 == spot1.row);
    var isN8 = (spot2.column == spot1.column && spot2.row - 1 == spot1.row);
    var isN9 = false; //var isN9 = (spot2.column - 1 == spot1.column && spot2.row - 1 == spot1.row);

    var isN = isN1 || isN2 || isN3 || isN4 || isN5 || isN6 || isN7 || isN8 || isN9;

    // if (isN) {
    //   console.log("{0}:{1} | {2}:{3}".format(spot1.column, spot1.row, spot2.column, spot2.row));
    // }

    return isN && spot1.valid && spot2.valid;
  };

  Template.benchmarkCalculate.helpers({

    'conditions': function() {
      return Session.get("conditions");
    },

    'toFixed': function(value) {
      if (value)
        return value.toFixed(2);
      return undefined;
    },

    'toPercentage': function(value) {
      if (value)
        return (value * 100.0).toFixed(1);
      return undefined;
    },
  });

  var calculate = function(condition) {
    var url = "https://dl.dropboxusercontent.com/u/3063782/benchmark_{0}.xml".format(condition);
    parseBenchmark(url, function(benchmark) {
      var precision = calculatePrecision(benchmark);
      var accuracy = calculateAccuracy(benchmark);

      var conditions = Session.get("conditions");

      var measures = precision.concat(accuracy);

      conditions.push({
        name: benchmark.name,
        lux: benchmark.lux,
        measures: measures
      });

      Session.set("conditions", conditions);
    });
  };

  /**
   *
   */
  Template.benchmarkCalculate.events({

    /**
     *
     */
    'click #calculate-btn': function(e, tmpl) {
      Session.set("conditions", []);

      calculate("officelight");
      calculate("officelight_rgb");

      calculate("1finger");
      calculate("1hand");
      calculate("2hands");

      calculate("bright");
      calculate("dark");
    },
  });
}
