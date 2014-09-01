(function () {

/* Imports */
var Meteor = Package.meteor.Meteor;
var _ = Package.underscore._;
var EJSON = Package.ejson.EJSON;

/* Package-scope variables */
var check, Match;

(function () {

///////////////////////////////////////////////////////////////////////////////////
//                                                                               //
// packages/check/match.js                                                       //
//                                                                               //
///////////////////////////////////////////////////////////////////////////////////
                                                                                 //
// XXX docs                                                                      // 1
                                                                                 // 2
// Things we explicitly do NOT support:                                          // 3
//    - heterogenous arrays                                                      // 4
                                                                                 // 5
var currentArgumentChecker = new Meteor.EnvironmentVariable;                     // 6
                                                                                 // 7
check = function (value, pattern) {                                              // 8
  // Record that check got called, if somebody cared.                            // 9
  //                                                                             // 10
  // We use getOrNullIfOutsideFiber so that it's OK to call check()              // 11
  // from non-Fiber server contexts; the downside is that if you forget to       // 12
  // bindEnvironment on some random callback in your method/publisher,           // 13
  // it might not find the argumentChecker and you'll get an error about         // 14
  // not checking an argument that it looks like you're checking (instead        // 15
  // of just getting a "Node code must run in a Fiber" error).                   // 16
  var argChecker = currentArgumentChecker.getOrNullIfOutsideFiber();             // 17
  if (argChecker)                                                                // 18
    argChecker.checking(value);                                                  // 19
  try {                                                                          // 20
    checkSubtree(value, pattern);                                                // 21
  } catch (err) {                                                                // 22
    if ((err instanceof Match.Error) && err.path)                                // 23
      err.message += " in field " + err.path;                                    // 24
    throw err;                                                                   // 25
  }                                                                              // 26
};                                                                               // 27
                                                                                 // 28
Match = {                                                                        // 29
  Optional: function (pattern) {                                                 // 30
    return new Optional(pattern);                                                // 31
  },                                                                             // 32
  OneOf: function (/*arguments*/) {                                              // 33
    return new OneOf(_.toArray(arguments));                                      // 34
  },                                                                             // 35
  Any: ['__any__'],                                                              // 36
  Where: function (condition) {                                                  // 37
    return new Where(condition);                                                 // 38
  },                                                                             // 39
  ObjectIncluding: function (pattern) {                                          // 40
    return new ObjectIncluding(pattern);                                         // 41
  },                                                                             // 42
  ObjectWithValues: function (pattern) {                                         // 43
    return new ObjectWithValues(pattern);                                        // 44
  },                                                                             // 45
  // Matches only signed 32-bit integers                                         // 46
  Integer: ['__integer__'],                                                      // 47
                                                                                 // 48
  // XXX matchers should know how to describe themselves for errors              // 49
  Error: Meteor.makeErrorType("Match.Error", function (msg) {                    // 50
    this.message = "Match error: " + msg;                                        // 51
    // The path of the value that failed to match. Initially empty, this gets    // 52
    // populated by catching and rethrowing the exception as it goes back up the // 53
    // stack.                                                                    // 54
    // E.g.: "vals[3].entity.created"                                            // 55
    this.path = "";                                                              // 56
    // If this gets sent over DDP, don't give full internal details but at least // 57
    // provide something better than 500 Internal server error.                  // 58
    this.sanitizedError = new Meteor.Error(400, "Match failed");                 // 59
  }),                                                                            // 60
                                                                                 // 61
  // Tests to see if value matches pattern. Unlike check, it merely returns true // 62
  // or false (unless an error other than Match.Error was thrown). It does not   // 63
  // interact with _failIfArgumentsAreNotAllChecked.                             // 64
  // XXX maybe also implement a Match.match which returns more information about // 65
  //     failures but without using exception handling or doing what check()     // 66
  //     does with _failIfArgumentsAreNotAllChecked and Meteor.Error conversion  // 67
  test: function (value, pattern) {                                              // 68
    try {                                                                        // 69
      checkSubtree(value, pattern);                                              // 70
      return true;                                                               // 71
    } catch (e) {                                                                // 72
      if (e instanceof Match.Error)                                              // 73
        return false;                                                            // 74
      // Rethrow other errors.                                                   // 75
      throw e;                                                                   // 76
    }                                                                            // 77
  },                                                                             // 78
                                                                                 // 79
  // Runs `f.apply(context, args)`. If check() is not called on every element of // 80
  // `args` (either directly or in the first level of an array), throws an error // 81
  // (using `description` in the message).                                       // 82
  //                                                                             // 83
  _failIfArgumentsAreNotAllChecked: function (f, context, args, description) {   // 84
    var argChecker = new ArgumentChecker(args, description);                     // 85
    var result = currentArgumentChecker.withValue(argChecker, function () {      // 86
      return f.apply(context, args);                                             // 87
    });                                                                          // 88
    // If f didn't itself throw, make sure it checked all of its arguments.      // 89
    argChecker.throwUnlessAllArgumentsHaveBeenChecked();                         // 90
    return result;                                                               // 91
  }                                                                              // 92
};                                                                               // 93
                                                                                 // 94
var Optional = function (pattern) {                                              // 95
  this.pattern = pattern;                                                        // 96
};                                                                               // 97
                                                                                 // 98
var OneOf = function (choices) {                                                 // 99
  if (_.isEmpty(choices))                                                        // 100
    throw new Error("Must provide at least one choice to Match.OneOf");          // 101
  this.choices = choices;                                                        // 102
};                                                                               // 103
                                                                                 // 104
var Where = function (condition) {                                               // 105
  this.condition = condition;                                                    // 106
};                                                                               // 107
                                                                                 // 108
var ObjectIncluding = function (pattern) {                                       // 109
  this.pattern = pattern;                                                        // 110
};                                                                               // 111
                                                                                 // 112
var ObjectWithValues = function (pattern) {                                      // 113
  this.pattern = pattern;                                                        // 114
};                                                                               // 115
                                                                                 // 116
var typeofChecks = [                                                             // 117
  [String, "string"],                                                            // 118
  [Number, "number"],                                                            // 119
  [Boolean, "boolean"],                                                          // 120
  // While we don't allow undefined in EJSON, this is good for optional          // 121
  // arguments with OneOf.                                                       // 122
  [undefined, "undefined"]                                                       // 123
];                                                                               // 124
                                                                                 // 125
var checkSubtree = function (value, pattern) {                                   // 126
  // Match anything!                                                             // 127
  if (pattern === Match.Any)                                                     // 128
    return;                                                                      // 129
                                                                                 // 130
  // Basic atomic types.                                                         // 131
  // Do not match boxed objects (e.g. String, Boolean)                           // 132
  for (var i = 0; i < typeofChecks.length; ++i) {                                // 133
    if (pattern === typeofChecks[i][0]) {                                        // 134
      if (typeof value === typeofChecks[i][1])                                   // 135
        return;                                                                  // 136
      throw new Match.Error("Expected " + typeofChecks[i][1] + ", got " +        // 137
                            typeof value);                                       // 138
    }                                                                            // 139
  }                                                                              // 140
  if (pattern === null) {                                                        // 141
    if (value === null)                                                          // 142
      return;                                                                    // 143
    throw new Match.Error("Expected null, got " + EJSON.stringify(value));       // 144
  }                                                                              // 145
                                                                                 // 146
  // Strings and numbers match literally.  Goes well with Match.OneOf.           // 147
  if (typeof pattern === "string" || typeof pattern === "number") {              // 148
    if (value === pattern)                                                       // 149
      return;                                                                    // 150
    throw new Match.Error("Expected " + pattern + ", got " +                     // 151
                          EJSON.stringify(value));                               // 152
  }                                                                              // 153
                                                                                 // 154
  // Match.Integer is special type encoded with array                            // 155
  if (pattern === Match.Integer) {                                               // 156
    // There is no consistent and reliable way to check if variable is a 64-bit  // 157
    // integer. One of the popular solutions is to get reminder of division by 1 // 158
    // but this method fails on really large floats with big precision.          // 159
    // E.g.: 1.348192308491824e+23 % 1 === 0 in V8                               // 160
    // Bitwise operators work consistantly but always cast variable to 32-bit    // 161
    // signed integer according to JavaScript specs.                             // 162
    if (typeof value === "number" && (value | 0) === value)                      // 163
      return                                                                     // 164
    throw new Match.Error("Expected Integer, got "                               // 165
                + (value instanceof Object ? EJSON.stringify(value) : value));   // 166
  }                                                                              // 167
                                                                                 // 168
  // "Object" is shorthand for Match.ObjectIncluding({});                        // 169
  if (pattern === Object)                                                        // 170
    pattern = Match.ObjectIncluding({});                                         // 171
                                                                                 // 172
  // Array (checked AFTER Any, which is implemented as an Array).                // 173
  if (pattern instanceof Array) {                                                // 174
    if (pattern.length !== 1)                                                    // 175
      throw Error("Bad pattern: arrays must have one type element" +             // 176
                  EJSON.stringify(pattern));                                     // 177
    if (!_.isArray(value) && !_.isArguments(value)) {                            // 178
      throw new Match.Error("Expected array, got " + EJSON.stringify(value));    // 179
    }                                                                            // 180
                                                                                 // 181
    _.each(value, function (valueElement, index) {                               // 182
      try {                                                                      // 183
        checkSubtree(valueElement, pattern[0]);                                  // 184
      } catch (err) {                                                            // 185
        if (err instanceof Match.Error) {                                        // 186
          err.path = _prependPath(index, err.path);                              // 187
        }                                                                        // 188
        throw err;                                                               // 189
      }                                                                          // 190
    });                                                                          // 191
    return;                                                                      // 192
  }                                                                              // 193
                                                                                 // 194
  // Arbitrary validation checks. The condition can return false or throw a      // 195
  // Match.Error (ie, it can internally use check()) to fail.                    // 196
  if (pattern instanceof Where) {                                                // 197
    if (pattern.condition(value))                                                // 198
      return;                                                                    // 199
    // XXX this error is terrible                                                // 200
    throw new Match.Error("Failed Match.Where validation");                      // 201
  }                                                                              // 202
                                                                                 // 203
                                                                                 // 204
  if (pattern instanceof Optional)                                               // 205
    pattern = Match.OneOf(undefined, pattern.pattern);                           // 206
                                                                                 // 207
  if (pattern instanceof OneOf) {                                                // 208
    for (var i = 0; i < pattern.choices.length; ++i) {                           // 209
      try {                                                                      // 210
        checkSubtree(value, pattern.choices[i]);                                 // 211
        // No error? Yay, return.                                                // 212
        return;                                                                  // 213
      } catch (err) {                                                            // 214
        // Other errors should be thrown. Match errors just mean try another     // 215
        // choice.                                                               // 216
        if (!(err instanceof Match.Error))                                       // 217
          throw err;                                                             // 218
      }                                                                          // 219
    }                                                                            // 220
    // XXX this error is terrible                                                // 221
    throw new Match.Error("Failed Match.OneOf or Match.Optional validation");    // 222
  }                                                                              // 223
                                                                                 // 224
  // A function that isn't something we special-case is assumed to be a          // 225
  // constructor.                                                                // 226
  if (pattern instanceof Function) {                                             // 227
    if (value instanceof pattern)                                                // 228
      return;                                                                    // 229
    // XXX what if .name isn't defined                                           // 230
    throw new Match.Error("Expected " + pattern.name);                           // 231
  }                                                                              // 232
                                                                                 // 233
  var unknownKeysAllowed = false;                                                // 234
  var unknownKeyPattern;                                                         // 235
  if (pattern instanceof ObjectIncluding) {                                      // 236
    unknownKeysAllowed = true;                                                   // 237
    pattern = pattern.pattern;                                                   // 238
  }                                                                              // 239
  if (pattern instanceof ObjectWithValues) {                                     // 240
    unknownKeysAllowed = true;                                                   // 241
    unknownKeyPattern = [pattern.pattern];                                       // 242
    pattern = {};  // no required keys                                           // 243
  }                                                                              // 244
                                                                                 // 245
  if (typeof pattern !== "object")                                               // 246
    throw Error("Bad pattern: unknown pattern type");                            // 247
                                                                                 // 248
  // An object, with required and optional keys. Note that this does NOT do      // 249
  // structural matches against objects of special types that happen to match    // 250
  // the pattern: this really needs to be a plain old {Object}!                  // 251
  if (typeof value !== 'object')                                                 // 252
    throw new Match.Error("Expected object, got " + typeof value);               // 253
  if (value === null)                                                            // 254
    throw new Match.Error("Expected object, got null");                          // 255
  if (value.constructor !== Object)                                              // 256
    throw new Match.Error("Expected plain object");                              // 257
                                                                                 // 258
  var requiredPatterns = {};                                                     // 259
  var optionalPatterns = {};                                                     // 260
  _.each(pattern, function (subPattern, key) {                                   // 261
    if (subPattern instanceof Optional)                                          // 262
      optionalPatterns[key] = subPattern.pattern;                                // 263
    else                                                                         // 264
      requiredPatterns[key] = subPattern;                                        // 265
  });                                                                            // 266
                                                                                 // 267
  _.each(value, function (subValue, key) {                                       // 268
    try {                                                                        // 269
      if (_.has(requiredPatterns, key)) {                                        // 270
        checkSubtree(subValue, requiredPatterns[key]);                           // 271
        delete requiredPatterns[key];                                            // 272
      } else if (_.has(optionalPatterns, key)) {                                 // 273
        checkSubtree(subValue, optionalPatterns[key]);                           // 274
      } else {                                                                   // 275
        if (!unknownKeysAllowed)                                                 // 276
          throw new Match.Error("Unknown key");                                  // 277
        if (unknownKeyPattern) {                                                 // 278
          checkSubtree(subValue, unknownKeyPattern[0]);                          // 279
        }                                                                        // 280
      }                                                                          // 281
    } catch (err) {                                                              // 282
      if (err instanceof Match.Error)                                            // 283
        err.path = _prependPath(key, err.path);                                  // 284
      throw err;                                                                 // 285
    }                                                                            // 286
  });                                                                            // 287
                                                                                 // 288
  _.each(requiredPatterns, function (subPattern, key) {                          // 289
    throw new Match.Error("Missing key '" + key + "'");                          // 290
  });                                                                            // 291
};                                                                               // 292
                                                                                 // 293
var ArgumentChecker = function (args, description) {                             // 294
  var self = this;                                                               // 295
  // Make a SHALLOW copy of the arguments. (We'll be doing identity checks       // 296
  // against its contents.)                                                      // 297
  self.args = _.clone(args);                                                     // 298
  // Since the common case will be to check arguments in order, and we splice    // 299
  // out arguments when we check them, make it so we splice out from the end     // 300
  // rather than the beginning.                                                  // 301
  self.args.reverse();                                                           // 302
  self.description = description;                                                // 303
};                                                                               // 304
                                                                                 // 305
_.extend(ArgumentChecker.prototype, {                                            // 306
  checking: function (value) {                                                   // 307
    var self = this;                                                             // 308
    if (self._checkingOneValue(value))                                           // 309
      return;                                                                    // 310
    // Allow check(arguments, [String]) or check(arguments.slice(1), [String])   // 311
    // or check([foo, bar], [String]) to count... but only if value wasn't       // 312
    // itself an argument.                                                       // 313
    if (_.isArray(value) || _.isArguments(value)) {                              // 314
      _.each(value, _.bind(self._checkingOneValue, self));                       // 315
    }                                                                            // 316
  },                                                                             // 317
  _checkingOneValue: function (value) {                                          // 318
    var self = this;                                                             // 319
    for (var i = 0; i < self.args.length; ++i) {                                 // 320
      // Is this value one of the arguments? (This can have a false positive if  // 321
      // the argument is an interned primitive, but it's still a good enough     // 322
      // check.)                                                                 // 323
      if (value === self.args[i]) {                                              // 324
        self.args.splice(i, 1);                                                  // 325
        return true;                                                             // 326
      }                                                                          // 327
    }                                                                            // 328
    return false;                                                                // 329
  },                                                                             // 330
  throwUnlessAllArgumentsHaveBeenChecked: function () {                          // 331
    var self = this;                                                             // 332
    if (!_.isEmpty(self.args))                                                   // 333
      throw new Error("Did not check() all arguments during " +                  // 334
                      self.description);                                         // 335
  }                                                                              // 336
});                                                                              // 337
                                                                                 // 338
var _jsKeywords = ["do", "if", "in", "for", "let", "new", "try", "var", "case",  // 339
  "else", "enum", "eval", "false", "null", "this", "true", "void", "with",       // 340
  "break", "catch", "class", "const", "super", "throw", "while", "yield",        // 341
  "delete", "export", "import", "public", "return", "static", "switch",          // 342
  "typeof", "default", "extends", "finally", "package", "private", "continue",   // 343
  "debugger", "function", "arguments", "interface", "protected", "implements",   // 344
  "instanceof"];                                                                 // 345
                                                                                 // 346
// Assumes the base of path is already escaped properly                          // 347
// returns key + base                                                            // 348
var _prependPath = function (key, base) {                                        // 349
  if ((typeof key) === "number" || key.match(/^[0-9]+$/))                        // 350
    key = "[" + key + "]";                                                       // 351
  else if (!key.match(/^[a-z_$][0-9a-z_$]*$/i) || _.contains(_jsKeywords, key))  // 352
    key = JSON.stringify([key]);                                                 // 353
                                                                                 // 354
  if (base && base[0] !== "[")                                                   // 355
    return key + '.' + base;                                                     // 356
  return key + base;                                                             // 357
};                                                                               // 358
                                                                                 // 359
                                                                                 // 360
///////////////////////////////////////////////////////////////////////////////////

}).call(this);


/* Exports */
if (typeof Package === 'undefined') Package = {};
Package.check = {
  check: check,
  Match: Match
};

})();

//# sourceMappingURL=check.js.map
