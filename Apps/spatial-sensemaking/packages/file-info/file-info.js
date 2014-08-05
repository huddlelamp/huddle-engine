FileInfo = function(options) {
  options = options || {};
  this.name = options.name;
  this.type = options.type;
  this.size = options.size;
  this.source = options.source;
};

FileInfo.fromJSONValue = function(value) {
  return new FileInfo({
    name: value.name,
    type: value.type,
    size: value.size,
    source: EJSON.fromJSONValue(value.source)
  });
};

FileInfo.prototype = {
  constructor: FileInfo,

  typeName: function() {
    return "FileInfo";
  },

  equals: function(other) {
    return
    this.name == other.name &&
    this.type == other.type &&
    this.size == other.size;
  },

  clone: function() {
    return new FileInfo({
      name: this.name,
      type: this.type,
      size: this.size,
      source: this.source
    });
  },

  toJSONValue: function() {
    return {
      name: this.name,
      type: this.type,
      size: this.size,
      source: EJSON.toJSONValue(this.source)
    };
  }
};

EJSON.addType("FileInfo", FileInfo.fromJSONValue);

if (Meteor.isClient) {
  _.extend(FileInfo.prototype, {
    read: function (file, callback) {
      var reader = new FileReader();
      var fileInfo = this;

      callback = callback || function() {};

      reader.onload = function() {
        fileInfo.source = new Uint8Array(reader.result);
        callback(null, fileInfo);
      };

      reader.onerror = function() {
        callback(reader.error);
      };

      reader.readAsArrayBuffer(file);
    }
  });

  _.extend(FileInfo, {
    read: function(file, callback) {
      return new FileInfo(file).read(file, callback);
    }
  });
}
