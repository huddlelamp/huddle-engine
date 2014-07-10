// Create closure.
(function($) {
 
    // Plugin definition.
    $.fn.visualizeTouches = function(options) {

        // This is the easiest way to have default options.
        this.settings = $.extend({
            // These are the defaults.
            canvasStyle: 'position: absolute; left: 0; top: 0; background-color: transparent;',
            canvasClass: '',
            radius: 30,
            fillStyle: 'rgba(255, 255, 255, 0.1)',
            strokeStyle: 'black',
            lineWidth: 5
        }, options);

        var $canvas = $('#touch-visualizer');
        if ($canvas.length == 0) {
            $canvas = $('<canvas/>', {
                id: 'touch-visualizer',
                style: this.settings.canvasStyle,
                'class': this.settings.canvasClass
            });

            var canvas = $canvas.get(0);
            canvas.setAttribute('width', this.width());
            canvas.setAttribute('height', this.height());
        }
        
        this.prepend($canvas);

        var that = this;

        //this.on('mousemove', function(e) {
        //    renderMouse.call(that, $canvas, e);
        //});

        this.on('touchstart', function(e) {
            renderTouches.call(that, $canvas, e.originalEvent.touches);
        });

        this.on('touchmove', function(e) {
            renderTouches.call(that, $canvas, e.originalEvent.touches);
        });

        this.on('touchend', function(e) {
            renderTouches.call(that, $canvas, e.originalEvent.touches);
        });
    };

    function renderTouches($canvas, touches) {
        var canvas = $canvas.get(0);
        var context = canvas.getContext('2d');
        
        //canvas.width = canvas.width;
        context.clearRect(0, 0, this.width(), this.height());

        context.beginPath();

        for (var i = touches.length - 1; i >= 0; i--) {
            var touch = touches[i];

            var centerX = touch.clientX;
            var centerY = touch.clientY;

            var settings = this.settings;

            context.moveTo(centerX + settings.radius, centerY);
            context.arc(centerX, centerY, settings.radius, 0, 2 * Math.PI, false);
            context.fillStyle = settings.fillStyle;
            context.fill();
            context.lineWidth = settings.lineWidth;
            context.strokeStyle = settings.strokeStyle;
            context.stroke();
        }
    };

    function renderMouse($canvas, e) {
        var canvas = $canvas.get(0);
        var context = canvas.getContext('2d');
        
        //canvas.width = canvas.width;
        context.clearRect(0, 0, this.width(), this.height());

        context.beginPath();

        var centerX = e.clientX;
        var centerY = e.clientY;

        var settings = this.settings;

        context.moveTo(centerX + settings.radius, centerY);
        context.arc(centerX, centerY, settings.radius, 0, 2 * Math.PI, false);
        context.fillStyle = settings.fillStyle;
        context.fill();
        context.lineWidth = settings.lineWidth;
        context.strokeStyle = settings.strokeStyle;
        context.stroke();
    };
 
// End of closure.
 
})(jQuery);