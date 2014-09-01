if (Meteor.isClient) {
  function Point(id, x, y) {
    this._id = id;
    this._x = x;
    this._y = y;
    this._sampled = false;
  };

  Point.prototype = {
    get x() {
      return this._x;
    },
    set x(value) {
      console.log("my value is: " + value);
      this._x = value;
    }
  };
}
