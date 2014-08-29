(function () {

/* Imports */
var Meteor = Package.meteor.Meteor;

/* Package-scope variables */
var Deps;

(function () {

//////////////////////////////////////////////////////////////////////////////////
//                                                                              //
// packages/deps/deps.js                                                        //
//                                                                              //
//////////////////////////////////////////////////////////////////////////////////
                                                                                //
//////////////////////////////////////////////////                              // 1
// Package docs at http://docs.meteor.com/#deps //                              // 2
//////////////////////////////////////////////////                              // 3
                                                                                // 4
Deps = {};                                                                      // 5
                                                                                // 6
// http://docs.meteor.com/#deps_active                                          // 7
Deps.active = false;                                                            // 8
                                                                                // 9
// http://docs.meteor.com/#deps_currentcomputation                              // 10
Deps.currentComputation = null;                                                 // 11
                                                                                // 12
var setCurrentComputation = function (c) {                                      // 13
  Deps.currentComputation = c;                                                  // 14
  Deps.active = !! c;                                                           // 15
};                                                                              // 16
                                                                                // 17
var _debugFunc = function () {                                                  // 18
  // We want this code to work without Meteor, and also without                 // 19
  // "console" (which is technically non-standard and may be missing            // 20
  // on some browser we come across, like it was on IE 7).                      // 21
  //                                                                            // 22
  // Lazy evaluation because `Meteor` does not exist right away.(??)            // 23
  return (typeof Meteor !== "undefined" ? Meteor._debug :                       // 24
          ((typeof console !== "undefined") && console.log ?                    // 25
           function () { console.log.apply(console, arguments); } :             // 26
           function () {}));                                                    // 27
};                                                                              // 28
                                                                                // 29
var _throwOrLog = function (from, e) {                                          // 30
  if (throwFirstError) {                                                        // 31
    throw e;                                                                    // 32
  } else {                                                                      // 33
    var messageAndStack;                                                        // 34
    if (e.stack && e.message) {                                                 // 35
      var idx = e.stack.indexOf(e.message);                                     // 36
      if (idx >= 0 && idx <= 10) // allow for "Error: " (at least 7)            // 37
        messageAndStack = e.stack; // message is part of e.stack, as in Chrome  // 38
      else                                                                      // 39
        messageAndStack = e.message +                                           // 40
        (e.stack.charAt(0) === '\n' ? '' : '\n') + e.stack; // e.g. Safari      // 41
    } else {                                                                    // 42
      messageAndStack = e.stack || e.message;                                   // 43
    }                                                                           // 44
    _debugFunc()("Exception from Deps " + from + " function:",                  // 45
                 messageAndStack);                                              // 46
  }                                                                             // 47
};                                                                              // 48
                                                                                // 49
// Takes a function `f`, and wraps it in a `Meteor._noYieldsAllowed`            // 50
// block if we are running on the server. On the client, returns the            // 51
// original function (since `Meteor._noYieldsAllowed` is a                      // 52
// no-op). This has the benefit of not adding an unnecessary stack              // 53
// frame on the client.                                                         // 54
var withNoYieldsAllowed = function (f) {                                        // 55
  if ((typeof Meteor === 'undefined') || Meteor.isClient) {                     // 56
    return f;                                                                   // 57
  } else {                                                                      // 58
    return function () {                                                        // 59
      var args = arguments;                                                     // 60
      Meteor._noYieldsAllowed(function () {                                     // 61
        f.apply(null, args);                                                    // 62
      });                                                                       // 63
    };                                                                          // 64
  }                                                                             // 65
};                                                                              // 66
                                                                                // 67
var nextId = 1;                                                                 // 68
// computations whose callbacks we should call at flush time                    // 69
var pendingComputations = [];                                                   // 70
// `true` if a Deps.flush is scheduled, or if we are in Deps.flush now          // 71
var willFlush = false;                                                          // 72
// `true` if we are in Deps.flush now                                           // 73
var inFlush = false;                                                            // 74
// `true` if we are computing a computation now, either first time              // 75
// or recompute.  This matches Deps.active unless we are inside                 // 76
// Deps.nonreactive, which nullfies currentComputation even though              // 77
// an enclosing computation may still be running.                               // 78
var inCompute = false;                                                          // 79
// `true` if the `_throwFirstError` option was passed in to the call            // 80
// to Deps.flush that we are in. When set, throw rather than log the            // 81
// first error encountered while flushing. Before throwing the error,           // 82
// finish flushing (from a finally block), logging any subsequent               // 83
// errors.                                                                      // 84
var throwFirstError = false;                                                    // 85
                                                                                // 86
var afterFlushCallbacks = [];                                                   // 87
                                                                                // 88
var requireFlush = function () {                                                // 89
  if (! willFlush) {                                                            // 90
    setTimeout(Deps.flush, 0);                                                  // 91
    willFlush = true;                                                           // 92
  }                                                                             // 93
};                                                                              // 94
                                                                                // 95
// Deps.Computation constructor is visible but private                          // 96
// (throws an error if you try to call it)                                      // 97
var constructingComputation = false;                                            // 98
                                                                                // 99
//                                                                              // 100
// http://docs.meteor.com/#deps_computation                                     // 101
//                                                                              // 102
Deps.Computation = function (f, parent) {                                       // 103
  if (! constructingComputation)                                                // 104
    throw new Error(                                                            // 105
      "Deps.Computation constructor is private; use Deps.autorun");             // 106
  constructingComputation = false;                                              // 107
                                                                                // 108
  var self = this;                                                              // 109
                                                                                // 110
  // http://docs.meteor.com/#computation_stopped                                // 111
  self.stopped = false;                                                         // 112
                                                                                // 113
  // http://docs.meteor.com/#computation_invalidated                            // 114
  self.invalidated = false;                                                     // 115
                                                                                // 116
  // http://docs.meteor.com/#computation_firstrun                               // 117
  self.firstRun = true;                                                         // 118
                                                                                // 119
  self._id = nextId++;                                                          // 120
  self._onInvalidateCallbacks = [];                                             // 121
  // the plan is at some point to use the parent relation                       // 122
  // to constrain the order that computations are processed                     // 123
  self._parent = parent;                                                        // 124
  self._func = f;                                                               // 125
  self._recomputing = false;                                                    // 126
                                                                                // 127
  var errored = true;                                                           // 128
  try {                                                                         // 129
    self._compute();                                                            // 130
    errored = false;                                                            // 131
  } finally {                                                                   // 132
    self.firstRun = false;                                                      // 133
    if (errored)                                                                // 134
      self.stop();                                                              // 135
  }                                                                             // 136
};                                                                              // 137
                                                                                // 138
// http://docs.meteor.com/#computation_oninvalidate                             // 139
Deps.Computation.prototype.onInvalidate = function (f) {                        // 140
  var self = this;                                                              // 141
                                                                                // 142
  if (typeof f !== 'function')                                                  // 143
    throw new Error("onInvalidate requires a function");                        // 144
                                                                                // 145
  if (self.invalidated) {                                                       // 146
    Deps.nonreactive(function () {                                              // 147
      withNoYieldsAllowed(f)(self);                                             // 148
    });                                                                         // 149
  } else {                                                                      // 150
    self._onInvalidateCallbacks.push(f);                                        // 151
  }                                                                             // 152
};                                                                              // 153
                                                                                // 154
// http://docs.meteor.com/#computation_invalidate                               // 155
Deps.Computation.prototype.invalidate = function () {                           // 156
  var self = this;                                                              // 157
  if (! self.invalidated) {                                                     // 158
    // if we're currently in _recompute(), don't enqueue                        // 159
    // ourselves, since we'll rerun immediately anyway.                         // 160
    if (! self._recomputing && ! self.stopped) {                                // 161
      requireFlush();                                                           // 162
      pendingComputations.push(this);                                           // 163
    }                                                                           // 164
                                                                                // 165
    self.invalidated = true;                                                    // 166
                                                                                // 167
    // callbacks can't add callbacks, because                                   // 168
    // self.invalidated === true.                                               // 169
    for(var i = 0, f; f = self._onInvalidateCallbacks[i]; i++) {                // 170
      Deps.nonreactive(function () {                                            // 171
        withNoYieldsAllowed(f)(self);                                           // 172
      });                                                                       // 173
    }                                                                           // 174
    self._onInvalidateCallbacks = [];                                           // 175
  }                                                                             // 176
};                                                                              // 177
                                                                                // 178
// http://docs.meteor.com/#computation_stop                                     // 179
Deps.Computation.prototype.stop = function () {                                 // 180
  if (! this.stopped) {                                                         // 181
    this.stopped = true;                                                        // 182
    this.invalidate();                                                          // 183
  }                                                                             // 184
};                                                                              // 185
                                                                                // 186
Deps.Computation.prototype._compute = function () {                             // 187
  var self = this;                                                              // 188
  self.invalidated = false;                                                     // 189
                                                                                // 190
  var previous = Deps.currentComputation;                                       // 191
  setCurrentComputation(self);                                                  // 192
  var previousInCompute = inCompute;                                            // 193
  inCompute = true;                                                             // 194
  try {                                                                         // 195
    withNoYieldsAllowed(self._func)(self);                                      // 196
  } finally {                                                                   // 197
    setCurrentComputation(previous);                                            // 198
    inCompute = false;                                                          // 199
  }                                                                             // 200
};                                                                              // 201
                                                                                // 202
Deps.Computation.prototype._recompute = function () {                           // 203
  var self = this;                                                              // 204
                                                                                // 205
  self._recomputing = true;                                                     // 206
  try {                                                                         // 207
    while (self.invalidated && ! self.stopped) {                                // 208
      try {                                                                     // 209
        self._compute();                                                        // 210
      } catch (e) {                                                             // 211
        _throwOrLog("recompute", e);                                            // 212
      }                                                                         // 213
      // If _compute() invalidated us, we run again immediately.                // 214
      // A computation that invalidates itself indefinitely is an               // 215
      // infinite loop, of course.                                              // 216
      //                                                                        // 217
      // We could put an iteration counter here and catch run-away              // 218
      // loops.                                                                 // 219
    }                                                                           // 220
  } finally {                                                                   // 221
    self._recomputing = false;                                                  // 222
  }                                                                             // 223
};                                                                              // 224
                                                                                // 225
//                                                                              // 226
// http://docs.meteor.com/#deps_dependency                                      // 227
//                                                                              // 228
Deps.Dependency = function () {                                                 // 229
  this._dependentsById = {};                                                    // 230
};                                                                              // 231
                                                                                // 232
// http://docs.meteor.com/#dependency_depend                                    // 233
//                                                                              // 234
// Adds `computation` to this set if it is not already                          // 235
// present.  Returns true if `computation` is a new member of the set.          // 236
// If no argument, defaults to currentComputation, or does nothing              // 237
// if there is no currentComputation.                                           // 238
Deps.Dependency.prototype.depend = function (computation) {                     // 239
  if (! computation) {                                                          // 240
    if (! Deps.active)                                                          // 241
      return false;                                                             // 242
                                                                                // 243
    computation = Deps.currentComputation;                                      // 244
  }                                                                             // 245
  var self = this;                                                              // 246
  var id = computation._id;                                                     // 247
  if (! (id in self._dependentsById)) {                                         // 248
    self._dependentsById[id] = computation;                                     // 249
    computation.onInvalidate(function () {                                      // 250
      delete self._dependentsById[id];                                          // 251
    });                                                                         // 252
    return true;                                                                // 253
  }                                                                             // 254
  return false;                                                                 // 255
};                                                                              // 256
                                                                                // 257
// http://docs.meteor.com/#dependency_changed                                   // 258
Deps.Dependency.prototype.changed = function () {                               // 259
  var self = this;                                                              // 260
  for (var id in self._dependentsById)                                          // 261
    self._dependentsById[id].invalidate();                                      // 262
};                                                                              // 263
                                                                                // 264
// http://docs.meteor.com/#dependency_hasdependents                             // 265
Deps.Dependency.prototype.hasDependents = function () {                         // 266
  var self = this;                                                              // 267
  for(var id in self._dependentsById)                                           // 268
    return true;                                                                // 269
  return false;                                                                 // 270
};                                                                              // 271
                                                                                // 272
// http://docs.meteor.com/#deps_flush                                           // 273
Deps.flush = function (_opts) {                                                 // 274
  // XXX What part of the comment below is still true? (We no longer            // 275
  // have Spark)                                                                // 276
  //                                                                            // 277
  // Nested flush could plausibly happen if, say, a flush causes                // 278
  // DOM mutation, which causes a "blur" event, which runs an                   // 279
  // app event handler that calls Deps.flush.  At the moment                    // 280
  // Spark blocks event handlers during DOM mutation anyway,                    // 281
  // because the LiveRange tree isn't valid.  And we don't have                 // 282
  // any useful notion of a nested flush.                                       // 283
  //                                                                            // 284
  // https://app.asana.com/0/159908330244/385138233856                          // 285
  if (inFlush)                                                                  // 286
    throw new Error("Can't call Deps.flush while flushing");                    // 287
                                                                                // 288
  if (inCompute)                                                                // 289
    throw new Error("Can't flush inside Deps.autorun");                         // 290
                                                                                // 291
  inFlush = true;                                                               // 292
  willFlush = true;                                                             // 293
  throwFirstError = !! (_opts && _opts._throwFirstError);                       // 294
                                                                                // 295
  var finishedTry = false;                                                      // 296
  try {                                                                         // 297
    while (pendingComputations.length ||                                        // 298
           afterFlushCallbacks.length) {                                        // 299
                                                                                // 300
      // recompute all pending computations                                     // 301
      while (pendingComputations.length) {                                      // 302
        var comp = pendingComputations.shift();                                 // 303
        comp._recompute();                                                      // 304
      }                                                                         // 305
                                                                                // 306
      if (afterFlushCallbacks.length) {                                         // 307
        // call one afterFlush callback, which may                              // 308
        // invalidate more computations                                         // 309
        var func = afterFlushCallbacks.shift();                                 // 310
        try {                                                                   // 311
          func();                                                               // 312
        } catch (e) {                                                           // 313
          _throwOrLog("afterFlush", e);                                         // 314
        }                                                                       // 315
      }                                                                         // 316
    }                                                                           // 317
    finishedTry = true;                                                         // 318
  } finally {                                                                   // 319
    if (! finishedTry) {                                                        // 320
      // we're erroring                                                         // 321
      inFlush = false; // needed before calling `Deps.flush()` again            // 322
      Deps.flush({_throwFirstError: false}); // finish flushing                 // 323
    }                                                                           // 324
    willFlush = false;                                                          // 325
    inFlush = false;                                                            // 326
  }                                                                             // 327
};                                                                              // 328
                                                                                // 329
// http://docs.meteor.com/#deps_autorun                                         // 330
//                                                                              // 331
// Run f(). Record its dependencies. Rerun it whenever the                      // 332
// dependencies change.                                                         // 333
//                                                                              // 334
// Returns a new Computation, which is also passed to f.                        // 335
//                                                                              // 336
// Links the computation to the current computation                             // 337
// so that it is stopped if the current computation is invalidated.             // 338
Deps.autorun = function (f) {                                                   // 339
  if (typeof f !== 'function')                                                  // 340
    throw new Error('Deps.autorun requires a function argument');               // 341
                                                                                // 342
  constructingComputation = true;                                               // 343
  var c = new Deps.Computation(f, Deps.currentComputation);                     // 344
                                                                                // 345
  if (Deps.active)                                                              // 346
    Deps.onInvalidate(function () {                                             // 347
      c.stop();                                                                 // 348
    });                                                                         // 349
                                                                                // 350
  return c;                                                                     // 351
};                                                                              // 352
                                                                                // 353
// http://docs.meteor.com/#deps_nonreactive                                     // 354
//                                                                              // 355
// Run `f` with no current computation, returning the return value              // 356
// of `f`.  Used to turn off reactivity for the duration of `f`,                // 357
// so that reactive data sources accessed by `f` will not result in any         // 358
// computations being invalidated.                                              // 359
Deps.nonreactive = function (f) {                                               // 360
  var previous = Deps.currentComputation;                                       // 361
  setCurrentComputation(null);                                                  // 362
  try {                                                                         // 363
    return f();                                                                 // 364
  } finally {                                                                   // 365
    setCurrentComputation(previous);                                            // 366
  }                                                                             // 367
};                                                                              // 368
                                                                                // 369
// http://docs.meteor.com/#deps_oninvalidate                                    // 370
Deps.onInvalidate = function (f) {                                              // 371
  if (! Deps.active)                                                            // 372
    throw new Error("Deps.onInvalidate requires a currentComputation");         // 373
                                                                                // 374
  Deps.currentComputation.onInvalidate(f);                                      // 375
};                                                                              // 376
                                                                                // 377
// http://docs.meteor.com/#deps_afterflush                                      // 378
Deps.afterFlush = function (f) {                                                // 379
  afterFlushCallbacks.push(f);                                                  // 380
  requireFlush();                                                               // 381
};                                                                              // 382
                                                                                // 383
//////////////////////////////////////////////////////////////////////////////////

}).call(this);






(function () {

//////////////////////////////////////////////////////////////////////////////////
//                                                                              //
// packages/deps/deprecated.js                                                  //
//                                                                              //
//////////////////////////////////////////////////////////////////////////////////
                                                                                //
// Deprecated (Deps-recated?) functions.                                        // 1
                                                                                // 2
// These functions used to be on the Meteor object (and worked slightly         // 3
// differently).                                                                // 4
// XXX COMPAT WITH 0.5.7                                                        // 5
Meteor.flush = Deps.flush;                                                      // 6
Meteor.autorun = Deps.autorun;                                                  // 7
                                                                                // 8
// We used to require a special "autosubscribe" call to reactively subscribe to // 9
// things. Now, it works with autorun.                                          // 10
// XXX COMPAT WITH 0.5.4                                                        // 11
Meteor.autosubscribe = Deps.autorun;                                            // 12
                                                                                // 13
// This Deps API briefly existed in 0.5.8 and 0.5.9                             // 14
// XXX COMPAT WITH 0.5.9                                                        // 15
Deps.depend = function (d) {                                                    // 16
  return d.depend();                                                            // 17
};                                                                              // 18
                                                                                // 19
//////////////////////////////////////////////////////////////////////////////////

}).call(this);


/* Exports */
if (typeof Package === 'undefined') Package = {};
Package.deps = {
  Deps: Deps
};

})();

//# sourceMappingURL=deps.js.map
