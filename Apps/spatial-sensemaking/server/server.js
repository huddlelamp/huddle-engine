if (Meteor.isServer) {
  var Future = Npm.require('fibers/future');
  Meteor.startup(function () {
    Meteor.methods({
      'getOfficeContent': function(type, doc, cb) {
        
        var ext;
        if (type == "word") ext = "doc";
        console.log(type);
        console.log(ext);

        var fut = new Future();
        var fs = Npm.require('fs');
        // doc = unescape(encodeURIComponent(doc));
        doc = new Buffer(doc, "binary");
        fs.writeFileSync('/officedoc.'+ext, doc);

        var textract = Npm.require('textract');
        textract('/officedoc.'+ext, {preserveLineBreaks: true}, function( error, text ) {
          if (error !== null) console.log(error);
        // textract('/Users/BlackWolf/Desktop/test.doc', {preserveLineBreaks: true}, function( error, text ) {
          return fut.return(text);
        });

        return fut.wait();

        // require = Npm.require;
        //   node_xj = Npm.require("xls-to-json");
        //   node_xj({
        //     input: 'officedoc.'+ext,
        //     output: null
        //   }, function(err, result) {
        //     if(err) {
        //       console.error(err);
        //     } else {
        //       console.log(result);
        //     }
        //   });

        // var require = Npm.require;
        // XLS = require('xlsjs');
        // var workbook = XLS.readFile('/Users/BlackWolf/Desktop/test.xls');
        // console.log(workbook);
      }
    });
  });
}
