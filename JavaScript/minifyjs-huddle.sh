#for file in "./*.js"; do echo $file && cat $file >> "huddle-0.9.1.js"; done
cat huddle-common.js huddle-debug.js huddle-eventmanager.js huddle-client.js > huddle-0.9.2.js
