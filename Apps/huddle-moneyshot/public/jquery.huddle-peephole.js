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

                peepholeMetadata: {
                    scaleX: 1.0,
                    scaleY: 1.0
                }
            },
            options);

        var touchtarget = options.touchtarget ? $(options.touchtarget) : this;
        var that = this;

        var $element = $(this);

        var lastDragPosition = {
            x: 0,
            y: 0
          };

        var scaleX = options.peepholeMetadata.scaleX;
        var scaleY = options.peepholeMetadata.scaleY;
        var scaleXInv = 1 / scaleX;
        var scaleYInv = 1 / scaleY;

        var dragHandler = function(e) {

            if (e.type == '_gesture2_touch_move' || e.type == '_gesture2_touch_end') {
                var vp = $element.data('visualProperties');

                var dx = (e.pageX - lastDragPosition.x) * scaleX;
                var dy = (e.pageY - lastDragPosition.y) * scaleY;
                
                // update last drag position to calculate delta on next move
                lastDragPosition = {
                    x: e.pageX,
                    y: e.pageY
                };
                
                var x = vp.x + dx;
                var y = vp.y + dy;

                var transform = //'scale(' + scaleX + ',' + scaleY + ') ' + 
                                //'translate(' +  + ',' +  + ') ' +
                                'translate(' + x + 'px,' + y + 'px)';// +
                                //'scale(' + scaleXInv + ',' + scaleYInv + ')';
                                // attach existing transforms?!?

                $element.css('-webkit-transform', transform);

                $element.data('visualProperties', {
                    x: x,
                    y: y
                });
            }
            else if (e.type == '_gesture2_touch_start') {
                console.log('_gesture2_touch_start');

                var vp = $element.data('visualProperties');
                if (!vp || vp == 'undefined') {
                    vp = {
                        x: 0,
                        y: 0
                    }
                    $element.data('visualProperties', vp);
                }

                lastDragPosition = {
                    x: e.pageX,
                    y: e.pageY
                };
            }
            
            if (e.type == '_gesture2_touch_end') {

                var vp = $element.data('visualProperties');
                if (!vp || vp == 'undefined') {
                    vp = {
                        x: 0,
                        y: 0
                    }
                }

                console.log('_gesture2_touch_end: ' + vp.x + ',' + vp.y);
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
        //touchtarget.on("_gesture2_gesture_start", gesture_handler);
        //touchtarget.on("_gesture2_gesture_move", gesture_handler);
        //touchtarget.on("_gesture2_gesture_end", gesture_handler);

        return this;
    };
})(jQuery);