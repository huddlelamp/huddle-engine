if (Meteor.isClient) {

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

  ElasticSearch = (function(options) {

    this.options = options;

    /**
     *
     */
    var getBaseUrl = function() {
      var baseUrl = "{0}:{1}".format(this.options.host, this.options.port);

      if (this.options.protocol) {
        baseUrl = "{0}://{1}".format(this.options.protocol, baseUrl);
      }

      return baseUrl;
    }.bind(this);

    /**
     * Creates an index.
     *
     * @param {string} name Index name.
     * @param {Object} name Index settings.
     * @param {Function} callback Callback function(err, result) { }
     */
    this.createIndex = function(name, options, callback) {

      // Swap arguments if second argument is a function because second and
      // third parameter can be optional.
      if (typeof(options) == 'function') {
        callback = options;
        options = {};
      }

      var data = _.extend({
        settings: {
          index: {
            number_of_shards: 1,
            number_of_replicas: 0
          }
        }
      }, options);

      var url = "{0}/{1}".format(getBaseUrl(), name);

      HTTP.put(url, { data: data }, callback);
    };

    /**
     * Deletes index.
     *
     * @param {string} name Index name.
     * @param {Function} callback Callback function(err, result) { }
     */
    this.deleteIndex = function(name, callback) {
      var url = "{0}/{1}/".format(getBaseUrl(), name);

      HTTP.del(url, callback);
    };

    /**
     * Returns the index statistics.
     *
     * @param {Function} callback Callback function(err, result) { }
     */
    this.stats = function(callback) {
      var url = "{0}/_stats".format(getBaseUrl());

      HTTP.get(url, callback);
    };

    /**
     * Puts a mapping to index.
     *
     * @param {string} name Index name.
     * @param {Object} mapping Mapping.
     * @param {Function} callback Callback function(err, result) { }
     */
    this.putMapping = function(name, mapping, callback) {
      var url = "{0}/{1}/{2}/_mapping".format(getBaseUrl(), name, this.options.attachmentsPath);
      HTTP.put(url, { data: mapping }, callback);
    }

    /**
     * Deletes a mapping from index.
     *
     * @param {string} name Index name.
     * @param {string} mapping Mapping name.
     * @param {Function} callback Callback function(err, result) { }
     */
    this.deleteMapping = function(name, callback) {
      var url = "{0}/{1}/{2}/_mapping".format(getBaseUrl(), name, this.options.attachmentsPath);
      HTTP.del(url, callback);
    };

    /**
     * Returns the mapping for an index.
     *
     * @param {string} name Index name.
     * @param {Function} callback Callback function(err, result) { }
     */
    this.getMapping = function(name, callback) {
      var url = "{0}/{1}/{2}/_mapping".format(getBaseUrl(), name, this.options.attachmentsPath);
      HTTP.get(url, callback);
    };

    /**
     * Adds attachment to index.
     *
     * @param {string} name Index name.
     * @param {File} attachment Attachment as file.
     * @param {Function} callback Callback function(err, result) { }
     */
    this.addAttachment = function(name, attachment, callback) {
      var url = "{0}/{1}/{2}".format(getBaseUrl(), name, this.options.attachmentsPath);

      // Read file into memory.
      FileInfo.read(attachment, function(err, file) {

        // console.log(file);

        // workaround because of larger files lead to RangeError: Maximum call stack size exceeded. with traditional String.fromCharCode.apply(null, file.source)
        var source = "";
        for (var i = 0, len = file.size; i < len; i++) {
          source += String.fromCharCode(file.source[i]);
        }

        var data = {
          _name: attachment.name,
          _content_type: attachment.type,
          _content_length: attachment.size,
          file: btoa(source)
        };

        // console.log(atob(data.file));

        HTTP.post(url, { data: data }, callback);
      });
    };

    /**
     * Queries an elastic search index.
     *
     * @param {Object} query Query as object literal.
     * @param {Function} callback Callback function(err, result) { }
     */
    this.query = function(query, callback) {
      var index = IndexSettings.getActiveIndex();

      var url = "{0}/{1}/_search?pretty=true".format(getBaseUrl(), index);

      HTTP.post(url, { data: query }, callback);
    };

    /**
     * Gets a document from index.
     *
     * @param {string} id Document id.
     * @param {Function} callback Callback function(err, result) { }
     */
    this.get = function(id, callback) {
      var index = IndexSettings.getActiveIndex();

      var url = "{0}/{1}/{2}/{3}".format(getBaseUrl(), index, this.options.attachmentsPath, id);

      HTTP.get(url, callback);
    };

    this.ping = function() {
      console.log('ping');
    };

    return this;
  }).call({}, Meteor.settings.public.elasticSearch);
}
