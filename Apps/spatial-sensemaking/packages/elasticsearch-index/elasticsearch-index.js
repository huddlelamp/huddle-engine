// how long to let phantomjs run before we kill it
var REQUEST_TIMEOUT = 15*1000;
// maximum size of result HTML. node's default is 200k which is too
// small for our docs.
var MAX_BUFFER = 5*1024*1024; // 5MB

if (Meteor.isServer) {

  var childProcess = Npm.require('child_process');

  // childProcess.execFile(
  //   '/bin/bash',
  //   ['-c', ("exec curl -X PUT localhost:9200/test/attachment/phantomjs /dev/stdin <<'END'\n" + phantomScript + "\n")],
  //   {
  //     timeout: REQUEST_TIMEOUT,
  //     maxBuffer: MAX_BUFFER
  //   }, function (error, stdout, stderr) {
  //
  //     var startFlag = "###START###";
  //     var endFlag = "###END###";
  //
  //     var startIdx = stdout.indexOf(startFlag) + startFlag.length;
  //     var endIdx = stdout.indexOf(endFlag);
  //
  //     var html = stdout.substring(startIdx, endIdx);
  //
  //     if (!error) {
  //       var compressedHtml = html.replace(/(\r\n|\n|\r)/gm,"");
  //       callback(compressedHtml);
  //     }
  //   });

  _.extend(ES, {

    index: {
      addFile: function(fileInfo) {

        var buffer = new Buffer(fileInfo.source);
        var base64 = buffer.toString('base64');

        // console.log(base64);

        var bash = "curl -X POST \"localhost:9200/test/attachment/\" -d '{\"file\": \"" + base64 + "\"}'";

        if (true) {
          childProcess.execFile(
            '/bin/bash',
            ['-c', (bash)],
            {
              timeout: REQUEST_TIMEOUT,
              maxBuffer: MAX_BUFFER
            }, function (error, stdout, stderr) {

              if (error) {
                console.log(error);
              }
              else {
                console.log(stdout);
              }
            });
        }

        // console.log(bash);
      },
      removeFile: function(fileInfo) {
        console.log('echo');
      },
    },
  });

  // console.log(ES.index.addFile({
  //   source: "asdf4 4t34tq4aa 34 43 q4#44^6754^5"
  // }));
}
