if (Meteor.isServer) {
  var Future = Npm.require('fibers/future');
  Meteor.startup(function () {
    Meteor.methods({
      'getOfficeContent': function(type, doc, cb) {
    
        var ext;
        if (type == "word") ext = "doc";
        if (type == "excel") ext = "xls";
        
        var fs = Npm.require('fs');
        // doc = unescape(encodeURIComponent(doc));
        // doc = new Buffer(doc, "binary");
        fs.writeFileSync('/officedoc.'+ext, doc);

        var fut = new Future();

        if (type == "word") {
            var textract = Npm.require('textract');
            textract('/officedoc.'+ext, {preserveLineBreaks: true}, function( error, text ) {
            // textract('/Users/BlackWolf/Desktop/test.doc', {preserveLineBreaks: true}, function( error, text ) {
              if (error !== null) console.log(error);
              return fut.return(text);
            });
        }

        if (type == "excel") {
            var require = Npm.require;
            XLS = require('xlsjs');
            var workbook = XLS.readFile('/Users/BlackWolf/Desktop/test.xls');
            return workbook;
        }
        
        return fut.wait();
      }
    });
  });
}
