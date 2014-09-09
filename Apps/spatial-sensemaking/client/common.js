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
  var needCheckColor = true;

  var transformDeviceData = function(data) {
    var newData = {
      id: data.Identity.toString(),
      topLeft: {
        x: data.Location[0] || 0,
        y: data.Location[1] || 0,
        // x: Math.round(data.Location[0] * 10) / 10,
        // y: Math.round(data.Location[1] * 10) / 10
      },
      ratio: {
        // x: (Math.round(data.RgbImageToDisplayRatio.X * 10) / 10) || 1,
        // y: (Math.round(data.RgbImageToDisplayRatio.Y * 10) / 10) || 1
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

  var existingID = amplify.store("huddleid");
  if (existingID === undefined) existingID = null;
  console.log("EXISTING ID: "+existingID);

  // var start = Date.now();
  huddle = Huddle.client({ glyphId: existingID+"" })
  .on("devicelost", function(a) {
    console.log("DEVICE WAS LOST");
    console.log(a);
  })
  .on("proximity", function(data) {
    // if (capFPS(1)) return;

    if (firstProximityData && data.Identity) {
      console.log("MY ID: "+data.Identity);
      amplify.store("huddleid", data.Identity);
      firstProximityData = false;

      determineDeviceColor(data.Identity.toString());

      //Because two devices loading at the same time can get the same color, we
      //check the color after a random amount of time. If it's not unique, it will
      //be changed
      if (needCheckColor) {
        needCheckColor = false;

        var timeout = getRandomInt(3000, 8000);
        Meteor.setTimeout(function() {
          determineDeviceColor(data.Identity.toString());
        }, timeout);
      }
    }

    Session.set('thisDevice', transformDeviceData(data));

    var otherDevices = [];
    data.Presences.forEach(function(presence) {
      otherDevices.push(transformDeviceData(presence));
    });
    Session.set('otherDevices', otherDevices);

  }); // end .on("proximity")

  huddle.connect(huddleHost, huddlePort);

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

  Huddle.on("dosearch", function(data) {
    var thisDevice = Session.get('thisDevice');
    if (data.target !== thisDevice.id) return;

    // if (!data.page) data.page = 1;
    search(data.query, data.page);
  });

  Huddle.on("go", function(data) {
    var thisDevice = Session.get('thisDevice');
    if (data.target !== thisDevice.id) return;

    Router.go(data.template, data.params);
    Template.searchIndex.reflectURL();
  });
}

function determineDeviceColor(deviceID) {
  //If the device has a unique color already, we don't need to look for a new one
  var needColor = true;
  var thisInfos = DeviceInfo.findOne({ _id: deviceID });
  if (thisInfos !== undefined) {
    var thisColorInfos = DeviceInfo.find({ colorDeg: thisInfos.colorDeg}).fetch();
    if (thisColorInfos.length === 1) {
      needColor = false;
    }
  }

  if (needColor) {
    //First time we got some proximity data, it's time to figure out this 
    //devices color. Sounds easy, right? It's not.
    //In general, we assume HSV colorspace with saturation and value both being 1,
    //which gives us a 2D color wheel. In order to calculate as different colors as 
    //possible, we now try to place colors evenly distributed on that wheel.
    //To do so, we divide the wheel into four even sections (90º each) and place 
    //our color in the section that has been used least. Inside each section, we
    //furthermore evenly distribute the colors. This also makes sure that after
    //a color has been given to a device, the next device receives the 
    //complementary color
    
    //Grab all colors that have been distributed already and divide them by the
    //four sectors of the color wheel
    var infos = DeviceInfo.find({}, {sort: ["colorDeg", "asc"]}).fetch();
    var existingColors = { 0: [], 1: [], 2: [], 3: [] };
    for (var i=0; i<infos.length; i++) {
      var currentColor = infos[i].colorDeg;
      var sector;
      if (currentColor >= 0   && currentColor < 90)  sector = 0;
      if (currentColor >= 90  && currentColor < 180) sector = 1;
      if (currentColor >= 180 && currentColor < 270) sector = 2;
      if (currentColor >= 270 && currentColor < 369) sector = 3;

      //Don't count duplicates in the sectors
      if (existingColors[sector].indexOf(currentColor) === -1) {
        existingColors[sector].push(currentColor);
      }//
    }

    //Figure out the largest sector (that has the most colors). If multiple sectors
    //are equally large, all of them are considered the largest sector
    var largestSectorSize = Math.max(
      existingColors[0].length, 
      existingColors[1].length,
      existingColors[2].length,
      existingColors[3].length
    );

    var largestSectors = [];
    if (existingColors[0].length === largestSectorSize) largestSectors.push(0);
    if (existingColors[1].length === largestSectorSize) largestSectors.push(1);
    if (existingColors[2].length === largestSectorSize) largestSectors.push(2);
    if (existingColors[3].length === largestSectorSize) largestSectors.push(3);

    //Now that we have the largest sector(s) we can figure out the sectors that
    //the new color should be in, so that our colors get evenly distributed among
    //the sectors.
    var targetSector;

    //If all sectors are equal in size, we just target the first sector
    if (largestSectors.length === 0 || largestSectors.length === 4) {
      targetSector = 0;
    }

    //If one sector is larger than all others, we need to pick the opposite 
    //sector to balance the color wheel
    if (largestSectors.length === 1) {
      targetSector = (largestSectors[0] + 2) % 4;
    }

    //If two sectors are larger than the others, we can pick any of the remaining
    //two (and just pick the first of them)
    if (largestSectors.length === 2) {
      targetSector = (largestSectors[0] + 1) % 4;
    }

    //If three sectors are larger than a fourth, we must pick the remaining one
    if (largestSectors.length === 3) {
      var sectorsLeft = [0, 1, 2, 3];
      for (var i=0; i<largestSectors.length; i++) {
        var index = sectorsLeft.indexOf(largestSectors[i]);
        if (index > -1) {
          sectorsLeft.splice(index, 1);
        }
      }
      targetSector = sectorsLeft[0];
    }

    //Now that we have the sector our color must be in, we need to balance colors
    //inside of that sector. For that, we look for the largest difference between
    //two colors inside that sector and place the new color right in the middle
    //of those
    //Exception: The first and second color are "hardcoded" for the algorithm
    //to work. The first color is the first color in that sector, the second color
    //is the middle color of that sector (because it's in the middle between this and
    //the next sector)
    var smallestSectorColorDeg = targetSector * 90;
    var largestSectorColorDeg = smallestSectorColorDeg + 90;

    var newColorDeg;
    if (existingColors[targetSector].length === 0) {
      newColorDeg = smallestSectorColorDeg;
    } else if (existingColors[targetSector].length === 1) {
      newColorDeg = smallestSectorColorDeg + 45;
    } else {
      var maxDiff = 0;
      for (var i=0; i<existingColors[targetSector].length; i++) {
        var currentColorDeg = existingColors[targetSector][i];

        var nextColor = largestSectorColorDeg;
        if ((i+1) < existingColors[targetSector].length) {
          nextColor = existingColors[targetSector][i+1];
        }

        var diff = nextColor-currentColorDeg;
        if (diff > maxDiff) {
          newColorDeg = currentColorDeg + diff/2.0;
          maxDiff = diff;
        }
      }
    }
    
    DeviceInfo._upsert(deviceID, { $set: { colorDeg: newColorDeg } });
  }
}

var cachedColorDegs = {};
window.getDeviceColorDeg = function(deviceID) {
  if (cachedColorDegs[deviceID]) {
    return cachedColorDegs[deviceID];
  }

  // var info = DeviceInfo.findOne({ _id: deviceID });
  // if (info === undefined || info.colorDeg === undefined) return 0;
  
  var cursor = DeviceInfo.find({_id: deviceID });
  var info = cursor.fetch()[0];
  if (info === undefined || info.colorDeg === undefined) return 0;

  var observer = function(deviceID) {
    return function(newDocument) {
      if (newDocument === undefined || newDocument.colorDeg === undefined) return;
      cachedColorDegs[deviceID] = newDocument.colorDeg;
    };
  };

  cursor.observe({
    added   : observer(deviceID),
    changed : observer(deviceID),
    removed : observer(deviceID),
  });

  cachedColorDegs[deviceID] = info.colorDeg;
  return info.colorDeg;
};

window.degreesToColor = function(deg, ensureVisibility) {
  if (ensureVisibility === undefined) ensureVisibility = true;

  var color = new tinycolor({
    s: 1,
    v: 1,
    h: deg
  });

  //If requested, make sure the color is visible on white background
  if (ensureVisibility) {
    while (color.getBrightness() > 127) {
      color = color.darken(1);
    }
  }

  color = color.toRgb();
  return {
    r: Math.round(color.r),
    g: Math.round(color.g),
    b: Math.round(color.b)
  };
};

function getRandomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

var lastFPSCap = new Date();
function capFPS(fps) {
  var now = new Date();
  var timeDiff = now.getTime() - lastFPSCap.getTime();

  if (timeDiff < (1000.0/fps)) {
    return true;
  } else {
    lastFPSCap = now;
    return false;
  }
}
