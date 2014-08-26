if (Meteor.isClient) {
  Template.navigation.helpers({
    "active": function(path) {
      var router = Router.current();
      if (router && router.route.name === path) {
        return "active";
      }
      return "";
    }
  });

  window.huddle = undefined;
  var huddleHost = Meteor.settings.public.huddle.host;
  var huddlePort = Meteor.settings.public.huddle.port;

  var firstProximityData = true;

  var transformDeviceData = function(data) {
    var newData = {
      id: data.Identity.toString(),
      topLeft: {
        x: data.Location[0] || 0,
        y: data.Location[1] || 0,
      },
      ratio: {
        x: data.RgbImageToDisplayRatio.X || 1,
        y: data.RgbImageToDisplayRatio.Y || 1
      },
      angle: data.Orientation || 0,
    };

    newData.width = 1.0/newData.ratio.x;
    newData.height = 1.0/newData.ratio.y;

    newData.topRight = {
      x: newData.topLeft.x + newData.width,
      y: newData.topLeft.y
    };

    newData.bottomRight = {
      x: newData.topLeft.x + newData.width,
      y: newData.topLeft.y + newData.height
    };

    newData.bottomLeft = {
      x: newData.topLeft.x,
      y: newData.topLeft.y + newData.height
    };

    newData.center = {
      x: newData.topLeft.x + newData.width/2.0,
      y: newData.topLeft.y + newData.height/2.0
    };

    return newData;
  }; //end transformDeviceData

  huddle = Huddle.client("MyHuddleName")
  .on("proximity", function(data) {
    Session.set('thisDevice', transformDeviceData(data));

    var otherDevices = [];
    data.Presences.forEach(function(presence) {
      otherDevices.push(transformDeviceData(presence));
    });
    Session.set('otherDevices', otherDevices);

    if (data.Identity && firstProximityData) {
      //Write a random color into the db, this will be this devices color
      var color = {
        r: getRandomInt(0, 255),
        g: getRandomInt(0, 255),
        b: getRandomInt(0, 255),
      };
      DeviceInfo._upsert(data.Identity.toString(), { $set: { color: color } });

      firstProximityData = false;
    }
  }); // end .on("proximity")
  huddle.connect(huddleHost, huddlePort);

  function getRandomInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
  }

  Huddle.on("showdocument", function(data) {
    //If this page has no detail document template, we are screwed :-)
    if (!Template.detailDocumentTemplate) return;

    var thisDevice = Session.get('thisDevice');
    if (data.target !== thisDevice.id) return;

    ElasticSearch.get(data.documentID, function(err, result) {
      if (err) {
        console.error(err);
      }
      else {
        Template.detailDocumentTemplate.open(result.data);
      }
    });
  });

  Huddle.on("addtextsnippet", function(data) {
    var thisDevice = Session.get('thisDevice');
    if (data.target !== thisDevice.id) return;

    //TODO also insert source document and the device that sent the snippet
    Snippets.insert({ device: thisDevice.id, text: data.snippet });
  });
}
