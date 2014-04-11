(function ($) {
    
    // an easy gesture function
    // will make a jQuery object move/scale/rotate on touch(es)
    $.fn.interactive = function (options) {
        if (!options || typeof (options) != "object") {
            options = {};
        }

        // default options
        options = $.extend(
            {
                // whether dragging, scaling and/or rotating are enable on the object
                // all these can be boolean or a function that returns boolean
                drag: true,
                scale: true,
                rotate: true,

                // we can make this jQuery object responds to touch on another jQuery
                touchtarget: null,

                visualProperties: {
                    lockedBy: -1,
                    x: 0,
                    y: 0,
                    width: 100,
                    height: 100,
                    rotation: 0.0,
                    scale: 1.0
                },

                peepholeMetadata: {
                    scaleX: 1.0,
                    scaleY: 1.0,
                    minVisualScale: 0.2,
                    maxVisualScale: 2.5
                },

                modelUpdated: null,
            },
            options);

        var touchtarget = options.touchtarget ? $(options.touchtarget) : this;
        var modelUpdated = options.modelUpdated;

        var that = this;

        var $visual = $(this);

        var vp = options.visualProperties;
        $visual.data('visualProperties', vp);

        var getParameterByName = function(name) {
            name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
            var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
                results = regex.exec(location.search);
            return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
        }
        // get device id -> used to lock objects
        var deviceId = getParameterByName("id");

        var doVisualTransform = function(transformPoint) {
            var vp = $visual.data('visualProperties');

            var x = vp.x;
            var y = vp.y;
            var rotation = vp.rotation;
            var scale = vp.scale;

            var offsetX = 0;
            var offsetY = 0;

            var transform = '';
            if (transformPoint != null && transformPoint != 'undefined') {
                offsetX = -transformPoint.x;
                offsetY = -transformPoint.y;   

                transform = 'translate(' + offsetX + ',' + offsetY +') ';
            }

            transform += 'translate(' + x + 'px,' + y + 'px) ' +
                         'scale(' + scale + ') ' +
                         'rotate(' + rotation + 'rad)';
            
            if (transformPoint && transformPoint != 'undefined') {
                transform += ' translate(' + (-offsetX) + ',' + (-offsetY) +')';
            }

            //console.log(transform);

            $visual.css('-webkit-transform', transform);

            if (modelUpdated != null && typeof modelUpdated == 'function') {
                // call model updated function with that as context
                var vp = $visual.data('visualProperties');
                modelUpdated.call(that, vp);
            }
        };

        var lastDragPosition = {
            x: 0,
            y: 0,
            scale: 1.0
          };

        var isScaleRotate = false;

        var dragHandler = function(e) {
            if (isScaleRotate) return;

            var vp = $visual.data('visualProperties');

            if (vp.lockedBy &&
                (vp.lockedBy != -1 && vp.lockedBy != deviceId)) return;

            for (var i = 0; i < e.touches.length; i++) {
                if (e.touches[i].handled) {
                    console.log('I am a dirty touch :)');
                    return;
                }
            }

            if (e.type == '_gesture2_touch_start') {
                //console.log('_gesture2_touch_start');

                lastDragPosition = {
                    x: e.pageX,
                    y: e.pageY
                };

                vp.lockedBy = deviceId;

                doVisualTransform(null);
            }
            else if (e.type == '_gesture2_touch_move' || e.type == '_gesture2_touch_end') {
                //console.log('_gesture2_touch_move');

                var dx = (e.pageX - lastDragPosition.x) * peepholeMetadata.scaleX;
                var dy = (e.pageY - lastDragPosition.y) * peepholeMetadata.scaleY;

                // adjust drag vector to device orientation
                var angle = window.orientationDevice * Math.PI / 180.0;
                var rotx = Math.cos(angle) * dx - Math.sin(angle) * dy;
                var roty = Math.sin(angle) * dx + Math.cos(angle) * dy;
                
                // update last drag position to calculate delta on next move
                lastDragPosition = {
                    x: e.pageX,
                    y: e.pageY
                };
                
                vp.x += rotx;
                vp.y += roty;

                doVisualTransform(null);
            }

            if (e.type == '_gesture2_touch_end') {
                vp.lockedBy = -1;

                doVisualTransform(null);
            }
        };

        var startRotateScalePosition = {
            x: 0,
            y: 0,
            scale: 1.0
          };

        var original_css;

        var gestureData;

        var saveTouches;

        var rotateScaleHandler = function(e) {

            var vp = $visual.data('visualProperties');

            if (vp.lockedBy &&
                (vp.lockedBy != -1 && vp.lockedBy != deviceId)) return;

            if (e.type == '_gesture2_gesture_start') {
                //console.log('_gesture2_gesture_start');

                saveTouches = e.touches;

                // deactivates drag handler
                isScaleRotate = true;

                vp.lockedBy = deviceId;

                doVisualTransform(null);

                gestureData = {
                    x: e.pageX,
                    y: e.pageY,
                    scale: vp.scale,
                    rotation: vp.rotation,
                    transformPoint: {
                        x: 0,
                        y: 0,
                    }
                };
            }
            else if (e.type == '_gesture2_gesture_move') {
                //console.log('_gesture2_gesture_move');

                var dx = (e.pageX - gestureData.x) * peepholeMetadata.scaleX;
                var dy = (e.pageY - gestureData.y) * peepholeMetadata.scaleY;
                
                // update last drag position to calculate delta on next move
                gestureData.x = e.pageX;
                gestureData.y = e.pageY;
                
                vp.x += dx;
                vp.y += dy;
                vp.scale = (gestureData.scale * e.scale);
                vp.rotation = (gestureData.rotation + e.rotation);

                /*
                var x = 0;
                var y = 0;
                var s = peepholeMetadata.scaleX;
                var r = vp.rotation;

                var v = Vector.create([e.pageX,e.pageY,0]);

                console.log('device point: ' + v.elements[0] + ',' + v.elements[1]);

                var M = $M([
                  [s * Math.cos(r),-1 * s * Math.sin(r),x],
                  [s * Math.sin(r),s * Math.cos(r),y],
                  [0,0,1]
                ]);
                var invM = M.inverse();
                var newVector = invM.multiply(v);

                console.log('page coordinate to world coordinate: ' + newVector.elements[0] + ',' + newVector.elements[1]);
                */

                doVisualTransform(gestureData.transformPoint);
            }
            else if (e.type == '_gesture2_gesture_end') {
                //console.log('_gesture2_gesture_end')

                for (var i = 0; i < saveTouches.length; i++) {
                    saveTouches[i].handled = true;
                }

                // activates drag handler
                isScaleRotate = false;

                vp.lockedBy = -1;

                doVisualTransform(null);
            }
        };

        // init gesture and touches, add handlers
        touchtarget.gestureInit({
            prefix: "_gesture2_",
            gesture_prefix: "_gesture2_"
        });

        touchtarget.on("_gesture2_touch_start", dragHandler);
        touchtarget.on("_gesture2_touch_move", dragHandler);
        touchtarget.on("_gesture2_touch_end", dragHandler);
        touchtarget.on("_gesture2_gesture_start", rotateScaleHandler);
        touchtarget.on("_gesture2_gesture_move", rotateScaleHandler);
        touchtarget.on("_gesture2_gesture_end", rotateScaleHandler);

        return this;
    };
})(jQuery);