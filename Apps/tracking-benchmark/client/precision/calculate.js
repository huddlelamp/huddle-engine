if (Meteor.isClient) {

  Session.setDefault("trackingImageWidth", 283);
  Session.setDefault("trackingImageHeight", 158);

  Session.setDefault("trackingAreaWidth", 102.0);
  Session.setDefault("trackingAreaHeight", 57.0);

  Session.setDefault("groundTruthMeasure", 10.0);

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
   * @param {Object} benchmark Benchmark object.
   */
  var calculatePrecision = function(benchmark) {

    var trackingImageWidth = Session.get("trackingImageWidth");
    var trackingImageHeight = Session.get("trackingImageHeight");

    var spots = benchmark.spots;

    var meansX = [];
    var meansY = [];

    var sdsX = [];
    var sdsY = [];

    $.each(spots, function(i, spot) {
      var samples = spot.samples;

      var xs = [];
      var ys = [];

      $.each(samples, function(i, sample) {
        var timestamp = sample.timestamp;
        var x = sample.x;
        var y = sample.y;
        var z = sample.z;

        xs.push(x);
        ys.push(y);
      });

      var meanX = ss.mean(xs);
      var meanY = ss.mean(ys);

      var sdX = ss.standard_deviation(xs);
      var sdY = ss.standard_deviation(ys);

      meansX.push(meanX);
      meansY.push(meanY);

      sdsX.push(sdX);
      sdsY.push(sdY);

      spot.meanX = meanX;
      spot.meanY = meanY;
      spot.sdX = sdX;
      spot.sdY = sdY;
    });

    // console.log("Precision MAX: X={0}, Y={1} [in px]".format(ss.max(sdsX) * trackingImageWidth, ss.max(sdsY) * trackingImageHeight));

    var x = (ss.max(sdsX) * trackingImageWidth);
    var y = (ss.max(sdsY) * trackingImageHeight);

    return [getMeasure("Precision Mean", "px", x, y)];
  };

  var calculateAccuracy = function(benchmark) {

    var trackingAreaWidth = Session.get("trackingAreaWidth");
    var trackingAreaHeight = Session.get("trackingAreaHeight");
    var groundTruth = Session.get("groundTruthMeasure");

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
        if (isNeighborSpot(spot, nSpot)) {
          neighborSpots.push(nSpot);

          if (spot.column != nSpot.column) {
            var x2 = nSpot.meanX * trackingAreaWidth;
            var dx = Math.abs(Math.abs(x1 - x2) - groundTruth);

            accX.push(dx);

            accuraciesX.push(dx);
          }
          else if (spot.row != nSpot.row) {
            var y2 = nSpot.meanY * trackingAreaHeight;
            var dy = Math.abs(Math.abs(y1 - y2) - groundTruth);

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
      getMeasure("Accuracy Mean", "cm", meanX, meanY),
      getMeasure("Accuracy Min", "cm", minX, minY),
      getMeasure("Accuracy Max", "cm", maxX, maxY),
      getMeasure("Accuracy SD", "cm", sdX, sdY)
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
        }

        var $samples = $spot.find("sample");

        $samples.each(function(i, xmlSample) {
          var $sample = $(xmlSample);

          var sample = {
            timestamp: new Date(parseInt($sample.attr("timestamp"))),
            x: parseFloat($sample.attr("x")),
            y: parseFloat($sample.attr("y")),
            z: parseFloat($sample.attr("z")),
          };

          spot.samples.push(sample);
        });

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
  var isNeighborSpot = function(spot1, spot2) {
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

    return isN;
  };

  Template.precisionCalculate.helpers({

    'conditions': function() {
      return Session.get("conditions");
    },

    'toFixed': function(value) {
      return value.toFixed(2);
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
        measures: measures
      });

      Session.set("conditions", conditions);
    });
  };

  /**
   *
   */
  Template.precisionCalculate.events({

    /**
     *
     */
    'click #calculate-btn': function(e, tmpl) {
      calculate("officelight");
      calculate("bright");
      calculate("dark");

      calculate("finger");
      calculate("1hand");
      calculate("2hands");

      calculate("rgb");
    },
  });
}
