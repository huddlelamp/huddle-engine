// how long to let phantomjs run before we kill it
var REQUEST_TIMEOUT = 15*1000;
// maximum size of result HTML. node's default is 200k which is too
// small for our docs.
var MAX_BUFFER = 5*1024*1024; // 5MB

if (Meteor.isServer) {

  /**
   * Helper function to use string format, e.g., as known from C#
   * var awesomeWorld = "Hello {0}! You are {1}.".format("World", "awesome");
   *
   * TODO Enclose the format prototype function in HuddleClient JavaScript API.
   * Source: http://stackoverflow.com/questions/1038746/equivalent-of-string-format-in-jquery
   */
  String.prototype.format = function () {
      var args = arguments;
      return this.replace(/\{\{|\}\}|\{(\d+)\}/g, function (m, n) {
          if (m == "{{") { return "{"; }
          if (m == "}}") { return "}"; }
          return args[n];
      });
  };

  var childProcess = Npm.require('child_process');

  var execCurl = function(cmd, callback) {
    var bash = "curl " + cmd;

    childProcess.execFile(
      '/bin/bash',
      ['-c', (bash)],
      {
        timeout: REQUEST_TIMEOUT,
        maxBuffer: MAX_BUFFER
      }, function (error, stdout, stderr) {

        if (error) {
          if (typeof callback == 'function')
            callback(error, undefined);
        }
        else {
          if (typeof callback == 'function')
            callback(undefined, stdout);
        }
      });
  };

  _.extend(ES, {

    host: "localhost",
    port: 9200,
    server: function() {
      return ES.host + ":" + ES.port
    },

    index: {

      /**
       * Adds index.
       *
       * @param {string} index Index name.
       */
      create: function(index) {
        var data = {
          settings: {
            number_of_shards: 3,
            number_of_replicas: 2
          }
        }

        var cmd = "-XPUT 'http://{0}/{1}/' -d '{2}'".format(
          ES.server(),
          index,
          JSON.stringify(data);
        );

        console.log(cmd);
      },

      /**
       * Deletes index.
       *
       * @param {string} index Index name.
       */
      delete: function(index) {
        var cmd = "-XDELETE 'http://{0}/{1}/'".format(
          ES.server(),
          index
        );

        execCurl(cmd, function(err, result) {
          if (err) {
            console.log(err);
          }
          else {
            console.log("Index " + index + " deleted");
          }
        });
      },

      /**
       * Add file to index.
       *
       * @param {string} index Index name.
       * @param {FileInfo} File info.
       */
      addFile: function(index, fileInfo) {

        var buffer = new Buffer(fileInfo.source);
        var base64 = buffer.toString('base64');

        // console.log(base64);

        var cmd = "-XPOST 'http://{0}/{1}/attachment/' -d '{\"file\": \"{2}\"}'".format(
          ES.server(),
          index,
          base64
        );

        execCurl(cmd, function(err, result) {
          if (err) {
            console.log(err);
          }
          else {
            console.log("Added " + fileInfo.name + " to index " + index);
          }
        });
      },

      /**
       * Remove file from index.
       *
       * @param {string} index Index name.
       * @param {FileInfo} File info.
       */
      removeFile: function(index, fileInfo) {
        console.log('echo');
      },
    },
  });

  // console.log(ES.index.addFile({
  //   source: "asdf4 4t34tq4aa 34 43 q4#44^6754^5"
  // }));
}
