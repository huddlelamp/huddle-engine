/** @constructor */
var MercatorProjection = function() {
    this.pixelOrigin_ = new google.maps.Point(MercatorProjection.TILE_SIZE / 2, MercatorProjection.TILE_SIZE / 2);
    this.pixelsPerLonDegree_ = MercatorProjection.TILE_SIZE / 360;
    this.pixelsPerLonRadian_ = MercatorProjection.TILE_SIZE / (2 * Math.PI);
};

MercatorProjection.TILE_SIZE = 256;

MercatorProjection.prototype.fromLatLngToPoint = function (latLng, optPoint) {
    var me = this;
    var point = optPoint || new google.maps.Point(0, 0);
    var origin = me.pixelOrigin_;

    point.x = origin.x + latLng.lng() * me.pixelsPerLonDegree_;

    // Truncating to 0.9999 effectively limits latitude to 89.189. This is
    // about a third of a tile past the edge of the world tile.
    var siny = this.bound(Math.sin(this.degreesToRadians(latLng.lat())), -0.9999,
        0.9999);
    point.y = origin.y + 0.5 * Math.log((1 + siny) / (1 - siny)) *
        -me.pixelsPerLonRadian_;
    return point;
};

MercatorProjection.prototype.fromPointToLatLng = function (point) {
    var me = this;
    var origin = me.pixelOrigin_;
    var lng = (point.x - origin.x) / me.pixelsPerLonDegree_;
    var latRadians = (point.y - origin.y) / -me.pixelsPerLonRadian_;
    var lat = radiansToDegrees(2 * Math.atan(Math.exp(latRadians)) -
        Math.PI / 2);
    return new google.maps.LatLng(lat, lng);
};

MercatorProjection.prototype.bound = function(value, optMin, optMax) {
    if (optMin != null) value = Math.max(value, optMin);
    if (optMax != null) value = Math.min(value, optMax);
    return value;
};

MercatorProjection.prototype.degreesToRadians = function(deg) {
    return deg * (Math.PI / 180);
};

MercatorProjection.prototype.radiansToDegrees = function(rad) {
    return rad / (Math.PI / 180);
};

// Make MercatorProjection globally available
window.MercatorProjection = MercatorProjection;
