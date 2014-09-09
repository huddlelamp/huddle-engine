Template.handlebarsCSS.deviceColor = function() {
    var thisDevice = Session.get('thisDevice');
    if (thisDevice === undefined) return '';

    var colorDeg = window.getDeviceColorDeg(thisDevice.id);
    // var info = DeviceInfo.findOne({ _id: thisDevice.id });
    // if (info === undefined || !info.colorDeg) return "";

    var color = window.degreesToColor(colorDeg);

    return 'rgb('+color.r+', '+color.g+', '+color.b+')';
};

Template.handlebarsCSS.darkerDeviceColor = function() {
    var thisDevice = Session.get('thisDevice');
    if (thisDevice === undefined) return '';

    var colorDeg = window.getDeviceColorDeg(thisDevice.id);
    // var info = DeviceInfo.findOne({ _id: thisDevice.id });
    // if (info === undefined || !info.colorDeg) return "";

    var color = new tinycolor(window.degreesToColor(colorDeg));
    color = color.darken(10).toRgb();

    return 'rgb('+color.r+', '+color.g+', '+color.b+')';
};

Template.handlebarsCSS.veryLightDeviceColor = function() {
    var thisDevice = Session.get('thisDevice');
    if (thisDevice === undefined) return '';

    var colorDeg = window.getDeviceColorDeg(thisDevice.id);
    // var info = DeviceInfo.findOne({ _id: thisDevice.id });
    // if (info === undefined || !info.colorDeg) return "";

    var color = new tinycolor(window.degreesToColor(colorDeg));
    while (color.getBrightness() < 245) {
      color = color.lighten(1);
    }
    color.setAlpha(0.3);
    // color.setAlpha(0.2);
    // color = color.toRgb();

    // return 'rgb('+color.r+', '+color.g+', '+color.b+')';
    return color.toRgbString();
};

Template.handlebarsCSS.opacityDeviceColor = function(opacity) {
    var thisDevice = Session.get('thisDevice');
    if (thisDevice === undefined) return '';

    var colorDeg = window.getDeviceColorDeg(thisDevice.id);
    // var info = DeviceInfo.findOne({ _id: thisDevice.id });
    // if (info === undefined || !info.colorDeg) return "";

    var color = new tinycolor(window.degreesToColor(colorDeg));
    color.setAlpha(opacity);
    // color.setAlpha(0.2);
    // color = color.toRgb();

    // return 'rgb('+color.r+', '+color.g+', '+color.b+')';
    return color.toRgbString();
};