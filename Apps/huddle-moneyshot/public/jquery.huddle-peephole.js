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
                offsetX = transformPoint.x;
                offsetY = transformPoint.y;   

                transform = 'translate(' + offsetX + ',' + offsetY +') ';
            }

            transform += 'translate(' + x + 'px,' + y + 'px) ' +
                         'scale(' + scale + ') ' +
                         'rotate(' + rotation + 'rad) '

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
            }
            else if (e.type == '_gesture2_touch_move' || e.type == '_gesture2_touch_end') {
                //console.log('_gesture2_touch_move');

                var vp = $visual.data('visualProperties');

                var dx = (e.pageX - lastDragPosition.x) * peepholeMetadata.scaleX;
                var dy = (e.pageY - lastDragPosition.y) * peepholeMetadata.scaleY;
                
                // update last drag position to calculate delta on next move
                lastDragPosition = {
                    x: e.pageX,
                    y: e.pageY
                };
                
                var x = vp.x + dx;
                var y = vp.y + dy;

                vp.x = x;
                vp.y = y;

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

            if (e.type == '_gesture2_gesture_start') {
                //console.log('_gesture2_gesture_start');

                var vp = $visual.data('visualProperties');

                saveTouches = e.touches;

                // deactivates drag handler
                isScaleRotate = true;

                gestureData = {
                    x: e.pageX,
                    y: e.pageY,
                    scale: vp.scale,
                    rotation: vp.rotation
                };
            }
            else if (e.type == '_gesture2_gesture_move') {
                //console.log('_gesture2_gesture_move');

                var vp = $visual.data('visualProperties');

                var dx = (e.pageX - gestureData.x) * peepholeMetadata.scaleX;
                var dy = (e.pageY - gestureData.y) * peepholeMetadata.scaleY;
                
                // update last drag position to calculate delta on next move
                gestureData.x = e.pageX;
                gestureData.y = e.pageY;
                
                var x = vp.x + dx;
                var y = vp.y + dy;
                vp.scale = (gestureData.scale * e.scale);
                vp.rotation = (gestureData.rotation + e.rotation);

                doVisualTransform(null);
            }
            else if (e.type == '_gesture2_gesture_end') {
                //console.log('_gesture2_gesture_end')

                for (var i = 0; i < saveTouches.length; i++) {
                    saveTouches[i].handled = true;
                }

                // activates drag handler
                isScaleRotate = false;
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