#!/bin/bash

## check if version parameter is set
if [ -z "$1" ]
  then
    echo "Version parameter is missing"
    echo "Usage: $> ./assemble-huddle.sh <VERSION> (e.g., 0.9.3)"

  exit 1
fi

echo "Create Huddle JavaScript library v$1 in repository folder."

## JavaScript files that will be concatenated in the defined order
declare -a files=("huddle-common.js" "huddle-log.js" "huddle-eventmanager.js" "huddle-client.js")

## concat all defined files
for file in "${files[@]}"
do
  cat "huddle/$file"
  echo
done > repository/huddle-$1.js

echo "Copy version to test-project/public/huddle.js"
cp -R repository/huddle-$1.js test-project/public/huddle.js

echo "Copy version to huddle.js"
cp -R repository/huddle-$1.js huddle.js

# Open repository folder
#open repository

echo "Copy version to huddle.js to meteor-huddle"
cp -R repository/huddle-$1.js ../../meteor-packages/raedle:huddle/raedle:huddle.js

echo "Done! Find huddle-$1.js ;)"
