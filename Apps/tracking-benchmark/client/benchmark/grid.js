if (Meteor.isClient) {

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

  /**
   * Creates a grid with the specified columns and rows and fits it into
   * width and height (the grid points are centered in width/height).
   *
   * @param {int} columns Grid columns.
   * @param {int} rows Grid rows.
   */
  var createGrid = function(columns, rows, width, height) {
    var data = [];

    var pointSize = 50;

    var maxWidth = width - 2 * pointSize;
    var maxHeight = height - 2 * pointSize;

    var stepX = maxWidth / columns;
    var stepY = maxHeight / rows;

    var column;
    var row;
    for (row = 0; row < rows; row++) {
      for (column = 0; column < columns; column++) {
        var index = column + (columns * row);
        data[index] = new Point(index, row, column, pointSize + column * stepX + stepX / 2, pointSize + row * stepY + stepY / 2);
      }
    }

    return data;
  };

  var settings = Meteor.settings.public;
  var benchmark = settings.benchmark;

  var pointGrid = createGrid(benchmark.columns, benchmark.rows, benchmark.width, benchmark.height);

  Session.setDefault("huddleLocation", []);
  Session.setDefault("gridWidth", benchmark.width);
  Session.setDefault("gridHeight", benchmark.height);

  Template.benchmarkGrid.helpers({

    benchmarkName: function() {
      return Session.get("benchmarkName");
    },

    huddleLocation: function() {
      var location = Session.get("huddleLocation");

      return "X={0}, Y={1}".format(location[0].toFixed(2), location[1].toFixed(2));
    },

    huddleAngle: function() {
      var angle = Session.get("huddleAngle");

      return "{0} (Radians)".format(angle.toFixed(2));
    },

    huddleState: function() {
      var state = Session.get("huddleState");

      switch (state) {
        case 0:
          return "not-tracked";
        case 1:
          return "tracked";
        case 2:
          return "occluded";
      }

      return "unknown";
    },

    gridData: function() {
      return pointGrid;
    },

    gridWidth: function() {
      return Session.get("gridWidth");
    },

    gridHeight: function() {
      return Session.get("gridHeight");
    },
  });

  Template.benchmarkGrid.events({
    'click circle, touchend circle': function(e, tmpl) {
      e.preventDefault();

      if (this.state == "sampled") {
        return;
      }

      $('#samplingViewModal').modal({
        show: true,
        backdrop: 'static'
      });

      doSampling(this, pointGrid);
    },
  });
}
