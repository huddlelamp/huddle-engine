/**
 * Helper function to use string format, e.g., as known from C#
 * var awesomeWorld = "Hello {0}! You are {1}.".format("World", "awesome");
 *
 * TODO Enclose the format prototype function in HuddleClient JavaScript API.
 */
String.prototype.format = function () {
    var args = arguments;
    return this.replace(/\{\{|\}\}|\{(\d+)\}/g, function (m, n) {
        if (m == "{{") { return "{"; }
        if (m == "}}") { return "}"; }
        return args[n];
    });
};

function namespace(namespaceString) {
    var parts = namespaceString.split("."),
        parent = window,
        currentPart = "";

    for(var i = 0, length = parts.length; i < length; i++) {
        currentPart = parts[i];
        parent[currentPart] = parent[currentPart] || {};
        parent = parent[currentPart];
    }

    return parent;
};
