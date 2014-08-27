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
    if (firstProximityData && data.Identity) {
      firstProximityData = false;

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
        existingColors[sector].push(currentColor);
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
      DeviceInfo._upsert(data.Identity.toString(), { $set: { colorDeg: newColorDeg } });
    }

    Session.set('thisDevice', transformDeviceData(data));

    var otherDevices = [];
    data.Presences.forEach(function(presence) {
      otherDevices.push(transformDeviceData(presence));
    });
    Session.set('otherDevices', otherDevices);

  }); // end .on("proximity")
  huddle.connect(huddleHost, huddlePort);

  // function getRandomInt(min, max) {
  //   return Math.floor(Math.random() * (max - min + 1)) + min;
  // }

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

window.degreesToColor = function(deg) {
  var hsv = new HSVColorspace();
  var color = hsv.toRGB({
    s: 1,
    v: 1,
    h: deg
  });

  // //The four directions of the color wheel are hardcoded.
  // //From those, we can calculate all the colors inbetween 
  // //Crazy Color Wheel from Wikipedia   
  // // var magenta = { r: 255, g: 0,   b: 255 }; //0º
  // // var blue    = { r: 0,   g: 0,   b: 255 }; //90º
  // // var cyan    = { r: 0,   g: 255, b: 255 }; //180º
  // // var yellow  = { r: 255, g: 255, b: 0   }; //270º

  // //CMYK Color Wheel
  // // var magenta = { r: 255, g: 0,   b: 255 }; //0º
  // // var blue    = { r: 0,   g: 0,   b: 0   }; //90º
  // // var cyan    = { r: 0,   g: 255, b: 255 }; //180º
  // // var yellow  = { r: 255, g: 255, b: 0   }; //270º

  // //Johannes Itten Color Wheel
  // // var magenta = { r: 255, g: 255,   b: 0 }; //0º
  // // var blue    = { r: 255, g: 64,    b: 0 }; //90º
  // // var cyan    = { r: 127, g: 0,     b: 127 }; //180º
  // // var yellow  = { r: 64,   g: 64,   b: 191   }; //270º
  
  // //RGB Color Wheel
  // var magenta = { r: 255, g: 0,   b: 0 }; //0º
  // var blue    = { r: 64,  g: 191,    b: 0 }; //90º
  // var cyan    = { r: 0, g: 127,     b: 127 }; //180º
  // var yellow  = { r: 64,   g: 0,   b: 191   }; //270ºah, wo

  // //Grab the percentage we walked on the wheel between two of the defined colors
  // //For example, 30º is 1/3 between magenta and blue and will therefore give us
  // //degPer = 0.33
  // var degMod = deg%90;
  // var degPer = (degMod > 0) ? (degMod/90) : 0;
  // var adegPer = 1.0 - degPer;
  // var color;
  // if (deg >= 0 && deg < 90) {
  //   color = {
  //     r: adegPer*magenta.r + degPer*blue.r,
  //     g: adegPer*magenta.g + degPer*blue.g,
  //     b: adegPer*magenta.b + degPer*blue.b,
  //   };
  // } 
  // if (deg >= 90 && deg < 180) {
  //   color = {
  //     r: adegPer*blue.r + degPer*cyan.r,
  //     g: adegPer*blue.g + degPer*cyan.g,
  //     b: adegPer*blue.b + degPer*cyan.b,
  //   };
  // } 
  // if (deg >= 180 && deg < 270) {
  //   color = {
  //     r: adegPer*cyan.r + degPer*yellow.r,
  //     g: adegPer*cyan.g + degPer*yellow.g,
  //     b: adegPer*cyan.b + degPer*yellow.b,
  //   };
  // } 
  // if (deg >= 270 && deg < 360) {
  //   color = {
  //     r: adegPer*yellow.r + degPer*magenta.r,
  //     g: adegPer*yellow.g + degPer*magenta.g,
  //     b: adegPer*yellow.b + degPer*magenta.b,
  //   };
  // } 

  // console.log(JSON.stringify(color));

  // if (color === undefined) return undefined;

  return {
    r: Math.round(color.r),
    g: Math.round(color.g),
    b: Math.round(color.b)
  };
};
