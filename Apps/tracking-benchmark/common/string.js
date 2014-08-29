/**
 * Helper function to use string format, e.g., as known from C#
 * var awesomeWorld = "Hello {0}! You are {1}.".format("World", "awesome");
 *
 * TODO Enclose the format prototype function in HuddleClient JavaScript API.
 * Source: http://stackoverflow.com/questions/1038746/equivalent-of-string-format-in-jquery
 */
String.prototype.format = function () {
    var args = arguments;
    return this.replace(/\{\{|\}\}|\{(\d+)\}/g, function (m, n) {
        if (m == "{{") { return "{"; }
        if (m == "}}") { return "}"; }
        return args[n];
    });
};
