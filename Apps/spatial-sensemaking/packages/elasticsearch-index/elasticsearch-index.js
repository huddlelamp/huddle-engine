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

  var ChildProcess = Npm.require('child_process');

  // Required to send async function calls sync to clients.
  var Future = Npm.require('fibers/future');

  var curl = function(cmd, callback) {
    var bash = "curl " + cmd;

    // console.log(bash);

    ChildProcess.execFile(
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

  var executeCmd = function(cmd) {
    var future = new Future();

    curl(cmd, function(err, result) {

      if (err) {
        console.error(err);
        future.return(err);
      }
      else {
        future.return(result);
      }
    });

    return future.wait();
  }

  _.extend(ES, {

    protocol: null,
    host: "localhost",
    port: 9200,
    server: function() {
      var url = ES.host + ":" + ES.port;

      if (ES.protocol)
        url = ES.protocol + "://" + url;

      return url;
    },
    attachmentName: "attachment",

    index: {

      /**
       * Adds index.
       *
       * @param {string} index Index name.
       */
      create: function(index) {
        var data = {
          settings: {
            index: {
              number_of_shards: 1,
              number_of_replicas: 0
            }
          }
        };

        var cmd = "-X PUT '{0}/{1}/' -d '{2}'".format(
          ES.server(),
          index,
          JSON.stringify(data)
        );

        return executeCmd(cmd);
      },

      /**
       * Deletes index.
       *
       * @param {string} index Index name.
       */
      delete: function(index) {
        var cmd = "-X DELETE '{0}/{1}/'".format(
          ES.server(),
          index
        );

        return executeCmd(cmd);
      },

      /**
       * Returns the index statistics.
       */
      stats: function() {
        var cmd = "'{0}/_stats'".format(
          ES.server()
        );

        return executeCmd(cmd);
      },

      /**
       * Enables attachments for index.
       *
       * @param {string} index Index name.
       * @param {boolean} enable Enable index (true/false).
       */
      enableAttachments: function(index, enable) {
        var data = {
          attachment: {
            properties: {
              file: {
                type: "attachment",
                fields: {
                  title: {
                    store: "yes"
                  },
                  file: {
                    term_vector: "with_positions_offsets",
                    store: "yes"
                  }
                }
              }
            }
          }
        };

        var cmd = "-X PUT '{0}/{1}/{2}/_mapping' -d '{3}'".format(
          ES.server(),
          index,
          ES.attachmentName,
          JSON.stringify(data)
        );

        return executeCmd(cmd);
      },

      /**
       * Adds attachment to index.
       *
       * @param {string} index Index name.
       * @param {FileInfo} file File info.
       */
      addAttachment: function(index, file) {

        if (file == null)
          throw "file is empty";

        var buffer = new Buffer(file.source);
        var base64 = buffer.toString('base64');

        // console.log(base64);

        var data = {
          file: base64
        };

        var cmd = "-X POST '{0}/{1}/{2}/' -d '{3}'".format(
          ES.server(),
          index,
          ES.attachmentName,
          JSON.stringify(data)
        );

        return executeCmd(cmd);
      },

      /**
       * Removes file from index.
       *
       * @param {string} index Index name.
       * @param {FileInfo} File info.
       */
      removeFile: function(index, fileInfo) {
        throw "Not yet implemented";
      },

      /**
       * Searches the index using the given query.
       *
       * @param {string} index Index name.
       * @param {string} query Search query.
       */
      search: function(index, query) {
        // curl "localhost:9200/_search?pretty=true" -d '{
        //   "fields" : ["title"],
        //   "query" : {
        //     "query_string" : {
        //       "query" : "amplifier"
        //     }
        //   },
        //   "highlight" : {
        //     "fields" : {
        //       "file" : {}
        //     }
        //   }
        // }'

        var data = {
          fields: ["title"],
          query: {
            query_string: {
              query: "amplifier"
            }
          },
          highlight: {
            fields: {
              file: {}
            }
          }
        };

        var cmd = "'{0}/_search?pretty=true' -d '{1}'".format(
          ES.server(),
          JSON.stringify(data)
        );

        return executeCmd(cmd);
      },
    },
  });
}
