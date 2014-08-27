/*
The MIT License (MIT)

Copyright (c) 2014 Benjamin Stauss

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

// (function() {
function hexToRGB(hexValue) {
  var hex = hexValue.substr(1);
  var rHex = hex.slice(0,2);
  var gHex = hex.slice(2,4);
  var bHex = hex.slice(4,6);
  
  var color = {
    r: parseInt(rHex, 16),
    g: parseInt(gHex, 16),
    b: parseInt(bHex, 16)
  };
  
  return color;
}

function rgbToHex(color) {
  var hexString = "#" + (color.r.toString(16).length === 1 ? "0"+color.r.toString(16) : color.r.toString(16));
  hexString += (color.g.toString(16).length === 1 ? "0"+color.g.toString(16) : color.g.toString(16));
  hexString += (color.b.toString(16).length === 1 ? "0"+color.b.toString(16) : color.b.toString(16));
  
  return hexString;
}

function XYZColorspace() {
  this.toXYZConstant = (1 / 0.17697);
  this.toXYZMatrix = [
  [0.49, 0.31, 0.2],
  [0.17697, 0.8124, 0.01063],
  [0, 0.01, 0.99]
  ];
  this.toRGBMatrix = [
  [0.41847, -0.15866, -0.082835],
  [-0.091169, 0.25243, 0.015708],
  [0.0009209, -0.0025498, 0.1786]
  ];
}

XYZColorspace.prototype = {
  toXYZ: function(rgbValue) {
    var xyzColor = {
      x: (this.toXYZConstant * ((this.toXYZMatrix[0][0] * (rgbValue.r / 255)) + (this.toXYZMatrix[0][1] * (rgbValue.g / 255)) + (this.toXYZMatrix[0][2] * (rgbValue.b / 255)))),
      y: (this.toXYZConstant * ((this.toXYZMatrix[1][0] * (rgbValue.r / 255)) + (this.toXYZMatrix[1][1] * (rgbValue.g / 255)) + (this.toXYZMatrix[1][2] * (rgbValue.b / 255)))),
      z: (this.toXYZConstant * ((this.toXYZMatrix[2][0] * (rgbValue.r / 255)) + (this.toXYZMatrix[2][1] * (rgbValue.g / 255)) + (this.toXYZMatrix[2][2] * (rgbValue.b / 255))))
    };
    
    return xyzColor;
  },
  toRGB: function(xyzValue) {
    var rgbColor = {
      r: 255 * ((this.toRGBMatrix[0][0] * xyzValue.x) + (this.toRGBMatrix[0][1] * xyzValue.y) + (this.toRGBMatrix[0][2] * xyzValue.z)),
      g: 255 * ((this.toRGBMatrix[1][0] * xyzValue.x) + (this.toRGBMatrix[1][1] * xyzValue.y) + (this.toRGBMatrix[1][2] * xyzValue.z)),
      b: 255 * ((this.toRGBMatrix[2][0] * xyzValue.x) + (this.toRGBMatrix[2][1] * xyzValue.y) + (this.toRGBMatrix[2][2] * xyzValue.z))
    };
    rgbColor.r = Math.round(((rgbColor.r > 255) ? 255 : (rgbColor.r < 0 ? 0 : rgbColor.r)));
    rgbColor.g = Math.round(((rgbColor.g > 255) ? 255 : (rgbColor.g < 0 ? 0 : rgbColor.g)));
    rgbColor.b = Math.round(((rgbColor.b > 255) ? 255 : (rgbColor.b < 0 ? 0 : rgbColor.b)));
    
    return rgbColor;
  }
};

function HSVColorspace() {
  
}

HSVColorspace.prototype = {
  toRGB: function(hsvValue) {
    var _c = hsvValue.v * hsvValue.s;
    var _h = hsvValue.h/60;
    var _x = _c * (1 - Math.abs((_h % 2) - 1));
    var _m = hsvValue.v - _c;
    var rgbColor = {
      r: 0,
      g: 0,
      b: 0
    };
    
    if(0 <= _h && _h < 1) {
      rgbColor.r = _c;
      rgbColor.g = _x;
      rgbColor.b = 0;
    } else if(1 <= _h && _h < 2) {
      rgbColor.r = _x;
      rgbColor.g = _c;
      rgbColor.b = 0;
    } else if(2 <= _h && _h < 3) {
      rgbColor.r = 0;
      rgbColor.g = _c;
      rgbColor.b = _x;
    } else if(3 <= _h && _h < 4) {
      rgbColor.r = 0;
      rgbColor.g = _x;
      rgbColor.b = _c;
    } else if(4 <= _h && _h < 5) {
      rgbColor.r = _x;
      rgbColor.g = 0;
      rgbColor.b = _c;
    } else if(5 <= _h && _h < 6) {
      rgbColor.r = _c;
      rgbColor.g = 0;
      rgbColor.b = _x;
    }
    
    for(var key in rgbColor) {
      rgbColor[key] += _m;
      rgbColor[key] *= 255;
      // rgbColor[key] = Math.floor(rgbColor[key]);
    }
    
    return rgbColor;
  },
  toHSV: function(rgbValue) {
    for(var key in rgbValue) {
      rgbValue[key] /= 255;
    }
    
    var _M = Math.max(rgbValue.r, rgbValue.g, rgbValue.b);
    var _m = Math.min(rgbValue.r, rgbValue.g, rgbValue.b);
    var _c = _M - _m;
    var _h;
    
    
    if(_c === 0) {
      _h = 0;
    } else if(_M === rgbValue.r) {
      _h = ((rgbValue.g - rgbValue.b)/_c) % 6;
    } else if(_M === rgbValue.g) {
      _h = ((rgbValue.b - rgbValue.r)/_c) + 2;
    } else if(_M === rgbValue.b) {
      _h = ((rgbValue.r - rgbValue.g)/_c) + 4;
    }
    _h *= 60;
    
    var hsvColor = {
      h: _h,
      v: _M,
      s: _M === 0 ? 0 : _c/_M
    };
    
    return hsvColor;
  }
};

function HSLColorspace() {
  
}

HSLColorspace.prototype = {
  toRGB: function(hslValue) {
    var _c = (1 - Math.abs((2 * hslValue.l) - 1)) * hslValue.s;
    var _h = hslValue.h/60;
    var _x = _c * (1 - Math.abs((_h % 2) - 1));
    var _m = hslValue.l - (0.5 * _c);
    var rgbColor = {
      r: 0,
      g: 0,
      b: 0
    };
    
    if(0 <= _h && _h < 1) {
      rgbColor.r = _c;
      rgbColor.g = _x;
      rgbColor.b = 0;
    } else if(1 <= _h && _h < 2) {
      rgbColor.r = _x;
      rgbColor.g = _c;
      rgbColor.b = 0;
    } else if(2 <= _h && _h < 3) {
      rgbColor.r = 0;
      rgbColor.g = _c;
      rgbColor.b = _x;
    } else if(3 <= _h && _h < 4) {
      rgbColor.r = 0;
      rgbColor.g = _x;
      rgbColor.b = _c;
    } else if(4 <= _h && _h < 5) {
      rgbColor.r = _x;
      rgbColor.g = 0;
      rgbColor.b = _c;
    } else if(5 <= _h && _h < 6) {
      rgbColor.r = _c;
      rgbColor.g = 0;
      rgbColor.b = _x;
    }
    
    for(var key in rgbColor) {
      rgbColor[key] += _m;
      rgbColor[key] *= 255;
      rgbColor[key] = Math.floor(rgbColor[key]);
    }
    
    return rgbColor;
  },
  toHSL: function(rgbValue) {
    for(var key in rgbValue) {
      rgbValue[key] /= 255;
    }
    
    var _M = Math.max(rgbValue.r, rgbValue.g, rgbValue.b);
    var _m = Math.min(rgbValue.r, rgbValue.g, rgbValue.b);
    var _c = _M - _m;
    var _h;
    
    if(_M === rgbValue.r) {
      _h = ((rgbValue.g - rgbValue.b)/_c) % 6;
    } else if(_M === rgbValue.g) {
      _h = ((rgbValue.b - rgbValue.r)/_c) + 2;
    } else if(_M === rgbValue.b) {
      _h = ((rgbValue.r - rgbValue.g)/_c) + 4;
    }
    _h *= 60;
    
    var hslColor = {
      h: _h,
      s: _c/(1-Math.abs(_M+_m-1)),
      l: 0.5 * (_M+_m)
    };
    
    return hslColor;
  }
};
// })();