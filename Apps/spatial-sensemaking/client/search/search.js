if (Meteor.isClient) {

  var search = function(query) {

    Session.set('querySuggestions', []);

    var SEARCH_FIELDS = ["file^10", "_name"];

    var must = [];
    var must_not = [];

    //Create an elasticsearch-entry for each term in the query
    processQuery(query, function(term, isNegated, isPhrase) {
      var entry = {
        multi_match: {
          query: term,
          fields: SEARCH_FIELDS
        }
      };

      if (isPhrase) entry.multi_match.type = "phrase";

      if (isNegated)  must_not.push(entry);
      else            must.push(entry);
    });

    //elasticsearch can't handle empty must/must_not blocks
    var bool = {};
    if (must.length > 0) bool.must = must;
    if (must_not.length > 0) bool.must_not = must_not;

    console.log(bool);

    var q = {
      fields: ["file"],
      from: 0,
      size: 10,
      query: { bool: bool },
      highlight: {
        fields: {
          file: {}
        }
      }
    };
    console.log(q);

    ElasticSearch.query(q, function(err, result) {
      if (err) {
        console.error(err);
      }
      else {
        var results = result.data;

        var observer = function(result) { 
          return function(newDocument) {
            result.documentMeta = newDocument;
            Session.set("results", results);
          }; 
        };

        for (var i = 0; i < results.hits.hits.length ; i++) {
          var result = results.hits.hits[i];

          var cursor = DocumentMeta.find({_id: result._id});
          result.documentMeta = cursor.fetch()[0];

          cursor.observe({
            added   : observer(result),
            changed : observer(result),
            removed : observer(result),
          });
        }

        Session.set("lastQuery", query);
        Session.set("results", results);

        var pastQuery = PastQueries.findOne({ query: query });
        if (pastQuery === undefined) {
          var newDoc = {
            query : query,
            count : 1
          };
          PastQueries.insert(newDoc);
        } else {
          PastQueries.update({_id: pastQuery._id}, { $inc: {count: 1}});
        }
      }
    });
  };

  Template.searchIndex.results = function() {
    return Session.get("results") || [];
  };

  Template.searchIndex.querySuggestions = function() {
    return Session.get("querySuggestions") || [];
  };

  Template.searchIndex.otherDevices = function() {
    return Session.get("otherDevices") || [];
  };

  Template.searchIndex.hasComment = function() {
    var meta = DocumentMeta.findOne({_id : this._id});
    return (meta && ((meta.comment && meta.comment.length > 0) || (meta.textHighlights && meta.textHighlights.length > 0)));
  };

  Template.searchIndex.wasWatched = function() {
    var meta = DocumentMeta.findOne({_id : this._id});
    return (meta && meta.watched);
  };

  Template.searchIndex.helpers({
    'thisDeviceBorderColorCSS': function() {
      var thisDevice = Session.get('thisDevice');
      if (thisDevice === undefined || !thisDevice.id) return;
      
      var info = DeviceInfo.findOne({ _id: thisDevice.id });
      if (info === undefined || !info.color) return;

      return 'border-image: radial-gradient(rgb('+info.color.r+', '+info.color.g+', '+info.color.b+') 25%, rgba('+info.color.r+', '+info.color.g+', '+info.color.b+', 0.35) 100%, rgba('+info.color.r+', '+info.color.g+', '+info.color.b+', 0.35)) 1%;';
      // return 'border-color: rgb('+info.color.r+', '+info.color.g+', '+info.color.b+');';
    },

    'deviceColorCSS': function() {
      var info = DeviceInfo.findOne({ _id: this.id });
      if (info === undefined || !info.color) return "";

      return 'background-color: rgb('+info.color.r+', '+info.color.g+', '+info.color.b+');';
    },

    'deviceSizePositionCSS': function() {
      var indicatorSize = 60;

      /** Returns the point of intersection of two lines.
          The lines are defined by their beginning and end points **/
      var intersect = function(p1, p2, p3, p4) {
        //Slopes
        var m1 = (p1.y-p2.y)/(p1.x-p2.x);
        var m2 = (p3.y-p4.y)/(p3.x-p4.x);

        //If a line is axis-parallel its slope is 0
        if (isNaN(m1) || !isFinite(m1)) m1 = 0;
        if (isNaN(m2) || !isFinite(m2)) m2 = 0;

        //If the two lines are parallel they don't intersect
        //If both lines have a slope of 0 they are orthogonal
        if (m1 === m2 && m1 !== 0) return undefined;

        // y = mx + c   =>   c = y - mx
        var c1 = p1.y - m1 * p1.x;
        var c2 = p3.y - m2 * p3.x;

        // y = m1 * x + c1 and y = m2 * x + c2   =>   x = (c2-c1)/(m1-m2)
        // Special case: If one of the two lines is y-parallel 
        var ix = (c2-c1)/(m1-m2);
        if ((p1.x-p2.x) === 0) ix = p1.x;
        if ((p3.x-p4.x) === 0) ix = p3.x;

        // y can now be figured out by inserting x into y = mx + c
        // Again special case: If a line is x-parallel
        var iy = m1 * ix + c1;
        if ((p1.y-p2.y) === 0) iy = p1.y;
        if ((p3.y-p4.y) === 0) iy = p3.y;

        return { x: ix, y: iy };
      };

      /** Checks if point p is inside the boundaries of the given device **/
      function insideDevice(p, device) {
        return (
          p.y >= device.topLeft.y && 
          p.y <= device.bottomLeft.y &&
          p.x >= device.topLeft.x &&
          p.x <= device.topRight.x
        );
      }

      /** Distance between two points **/
      function pointDist(p1, p2) {
        return Math.sqrt(Math.pow(p1.x - p2.x, 2) + Math.pow(p1.y - p2.y, 2));
      }

      var thisDevice = Session.get('thisDevice');
      var otherDevice = this;

      //Get the intersection with each of the four device boundaries
      var intersectLeft   = intersect(thisDevice.center, otherDevice.center, thisDevice.topLeft,    thisDevice.bottomLeft);
      var intersectTop    = intersect(thisDevice.center, otherDevice.center, thisDevice.topLeft,    thisDevice.topRight);
      var intersectRight  = intersect(thisDevice.center, otherDevice.center, thisDevice.topRight,   thisDevice.bottomRight);
      var intersectBottom = intersect(thisDevice.center, otherDevice.center, thisDevice.bottomLeft, thisDevice.bottomRight);

      // console.log("-----");
      // console.log(thisDevice);
      // console.log(intersectLeft);
      // console.log(intersectTop);
      // console.log(intersectRight);
      // console.log(intersectBottom);


      //Get the distance of each intersection and the other device
      //We need this to figure out which intersection to use
      //If an intersection is outside of the device it must be invalid      
      var leftDist   = insideDevice(intersectLeft,   thisDevice) ? pointDist(intersectLeft,   otherDevice.center) : 4000;
      var topDist    = insideDevice(intersectTop,    thisDevice) ? pointDist(intersectTop,    otherDevice.center) : 4000;
      var rightDist  = insideDevice(intersectRight,  thisDevice) ? pointDist(intersectRight,  otherDevice.center) : 4000;
      var bottomDist = insideDevice(intersectBottom, thisDevice) ? pointDist(intersectBottom, otherDevice.center) : 4000;

      // console.log(leftDist);
      // console.log(topDist);
      // console.log(rightDist);
      // console.log(bottomDist);

      //Figure out the final x/y coordinates of the device indicator
      //This is done by using the boundary intersection that is closest and valid
      //Then, one coordinate will be converted from world coords into device pixels
      //The other coordinate is simply set so that half the indicator is visible,
      //which gives us a nice half-circle
      //Furthermore, we calculate the indicator size based on the distance
      var top;
      var right;
      var bottom;
      var left;
      if (leftDist <= topDist && leftDist <= rightDist && leftDist <= bottomDist) {
        indicatorSize = 60 * (1.0-leftDist) + 40;
        var percent = ((intersectLeft.y - thisDevice.topLeft.y) / thisDevice.height);
        top = $(window).height() * percent - (indicatorSize/2.0);
        left = -(indicatorSize/2.0);
      } else if (topDist <= leftDist && topDist <= rightDist && topDist <= bottomDist) {
        indicatorSize = 60 * (1.0-topDist) + 40;
        var percent = ((intersectTop.x - thisDevice.topLeft.x) / thisDevice.width);
        top = -(indicatorSize/2.0);
        left = $(window).width() * percent - (indicatorSize/2.0);
      } else if (rightDist <= leftDist && rightDist <= topDist && rightDist <= bottomDist) {
        indicatorSize = 60 * (1.0-rightDist) + 40;
        var percent = ((intersectRight.y - thisDevice.topLeft.y) / thisDevice.height);
        top = $(window).height() * percent - (indicatorSize/2.0);
        right = -(indicatorSize/2.0);
      } else if (bottomDist <= leftDist && bottomDist <= topDist && bottomDist <= rightDist) {
        indicatorSize = 60 * (1.0-bottomDist) + 40;
        var percent = ((intersectBottom.x - thisDevice.topLeft.x) / thisDevice.width);
        bottom = -(indicatorSize/2.0);
        left = $(window).width() * percent - (indicatorSize/2.0);
      }

      var css = 'width: '+indicatorSize+'px; height: '+indicatorSize+'px; ';
      if (top    !== undefined) css += 'top: '+top+'px; ';
      if (right  !== undefined) css += 'right: '+right+'px; ';
      if (bottom !== undefined) css += 'bottom: '+bottom+'px; ';
      if (left   !== undefined) css += 'left: '+left+'px; ';

      /*console.log("SETTING DROP FOR "+$(".deviceIndicator").length);
      $(".deviceIndicator").bind("dragover dragenter", function(e) {
        event.dataTransfer.dropEffect = "copy";
        event.preventDefault();
      });

      $(".deviceIndicator").bind("drop", function() {
        console.log("DROOOp");
        event.preventDefault();
      });*/

      return css;
    },

    'toSeconds': function(ms) {
      var time = new Date(ms);
      return time.getSeconds() + "." + time.getMilliseconds();
    },

    urlToDocument: function(id) {
      var s = Meteor.settings.public.elasticSearch;

      var url = s.protocol + "://" + s.host + ":" + s.port + "/" + IndexSettings.getActiveIndex() + "/" + s.attachmentsPath + "/" + id;
      return url;
    },

    stringify: function(obj) {
      return JSON.stringify(obj);
    }
  });

  Template.searchIndex.events({
    'click #search-btn': function(e, tmpl) {
      var query = tmpl.$('#search-query').val();
      search(query);
    },

    'keyup #search-query': function(e, tmpl) {
      var query = tmpl.$('#search-query').val();
      if (query.trim().length === 0) {
        // $("#search-tag-wrapper").empty();
        return;
      }

      //On enter, start the search
      if (e.keyCode == 13) {
        search(query);
      //On every other key, try to fetch some query suggestions
      } else {
        // $("#search-tag-wrapper").empty();
        // tmpl.$('#search-query').val("");
        var unfinishedTerms = "";
        processQuery(query, function(term, isNegated, isPhrase, isFinished) {
          if (isFinished) {
            // $("#search-tag-wrapper").append("<span class='search-tag'>"+term+"</span>");
          } else {
            if (isPhrase) unfinishedTerms+= '"';
            unfinishedTerms += term+" ";
            // tmpl.$('#search-query').val(tmpl.$('#search-query').val()+" "+term);
          }
        });

        unfinishedTerms = unfinishedTerms.trim();
        // tmpl.$('#search-query').val(unfinishedTerms);

        var regexp = new RegExp('.*'+query+'.*', 'i');
        var suggestions = PastQueries.find(
          { query : { $regex: regexp } }, 
          { sort  : [["count", "desc"]] }
        );
        Session.set('querySuggestions', suggestions.fetch());
      }
    },

    'click .querySuggestion': function(e, tmpl) {
      tmpl.$('#search-query').val(this.query);
      search(this.query);
    },

    'click .hit': function(e, tmpl) {

      $.fancybox({
        type: "iframe",
        href: "/documentPopup/"+encodeURIComponent(this._id)+"/"+encodeURIComponent(Session.get("lastQuery")),
        autoSize: false,
        autoResize: false,
        height: "952px",
        width: "722px"
      });
      
    },

    'click .document-highlight': function(e, tmpl) {
      
      $.fancybox({
        type: "iframe",
        href: "/documentPopup/"+encodeURIComponent($(e.currentTarget).attr("documentid"))+"/"+encodeURIComponent(Session.get("lastQuery"))+"/"+encodeURIComponent(this),
        autoSize: false,
        autoResize: false,
        height: "952px",
        width: "722px"
      });

    },

    'click .favoritedStar': function(e, tmpl) {
      if (this.documentMeta && this.documentMeta.favorited) {
        DocumentMeta._upsert(this._id, {$set: {favorited: false}});
      } else {
        DocumentMeta._upsert(this._id, {$set: {favorited: true}});
      }
    },

    'touchdown .deviceIndicator, click .deviceIndicator': function(e, tmpl) {
      e.preventDefault();

      var iframe = $("iframe").first().get(0);
      var win = iframe.contentWindow || iframe;

      var targetID = $(e.currentTarget).attr("deviceid");
      var text = win.getSelectedContent();
      if (text !== undefined) {
        huddle.broadcast("addtextsnippet", { target: targetID, snippet: text } );
      } else {
        var documentID = win.documentID;
        if (documentID === undefined) return;
        huddle.broadcast("showdocument", { target: targetID, documentID: documentID } );
      }

      

      // console.log(text);
      // console.log(win.range);
    }
  });
}
