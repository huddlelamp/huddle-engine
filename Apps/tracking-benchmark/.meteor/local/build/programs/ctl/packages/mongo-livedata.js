(function () {

/* Imports */
var Meteor = Package.meteor.Meteor;
var Random = Package.random.Random;
var EJSON = Package.ejson.EJSON;
var _ = Package.underscore._;
var LocalCollection = Package.minimongo.LocalCollection;
var Minimongo = Package.minimongo.Minimongo;
var Log = Package.logging.Log;
var DDP = Package.livedata.DDP;
var DDPServer = Package.livedata.DDPServer;
var Deps = Package.deps.Deps;
var AppConfig = Package['application-configuration'].AppConfig;
var check = Package.check.check;
var Match = Package.check.Match;
var MaxHeap = Package['binary-heap'].MaxHeap;
var MinHeap = Package['binary-heap'].MinHeap;
var MinMaxHeap = Package['binary-heap'].MinMaxHeap;
var Hook = Package['callback-hook'].Hook;

/* Package-scope variables */
var MongoInternals, MongoTest, MongoConnection, CursorDescription, Cursor, listenAll, forEachTrigger, OPLOG_COLLECTION, idForOp, OplogHandle, ObserveMultiplexer, ObserveHandle, DocFetcher, PollingObserveDriver, OplogObserveDriver, LocalCollectionDriver;

(function () {

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                                      //
// packages/mongo-livedata/mongo_driver.js                                                              //
//                                                                                                      //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                                                        //
/**                                                                                                     // 1
 * Provide a synchronous Collection API using fibers, backed by                                         // 2
 * MongoDB.  This is only for use on the server, and mostly identical                                   // 3
 * to the client API.                                                                                   // 4
 *                                                                                                      // 5
 * NOTE: the public API methods must be run within a fiber. If you call                                 // 6
 * these outside of a fiber they will explode!                                                          // 7
 */                                                                                                     // 8
                                                                                                        // 9
var path = Npm.require('path');                                                                         // 10
var MongoDB = Npm.require('mongodb');                                                                   // 11
var Fiber = Npm.require('fibers');                                                                      // 12
var Future = Npm.require(path.join('fibers', 'future'));                                                // 13
                                                                                                        // 14
MongoInternals = {};                                                                                    // 15
MongoTest = {};                                                                                         // 16
                                                                                                        // 17
// This is used to add or remove EJSON from the beginning of everything nested                          // 18
// inside an EJSON custom type. It should only be called on pure JSON!                                  // 19
var replaceNames = function (filter, thing) {                                                           // 20
  if (typeof thing === "object") {                                                                      // 21
    if (_.isArray(thing)) {                                                                             // 22
      return _.map(thing, _.bind(replaceNames, null, filter));                                          // 23
    }                                                                                                   // 24
    var ret = {};                                                                                       // 25
    _.each(thing, function (value, key) {                                                               // 26
      ret[filter(key)] = replaceNames(filter, value);                                                   // 27
    });                                                                                                 // 28
    return ret;                                                                                         // 29
  }                                                                                                     // 30
  return thing;                                                                                         // 31
};                                                                                                      // 32
                                                                                                        // 33
// Ensure that EJSON.clone keeps a Timestamp as a Timestamp (instead of just                            // 34
// doing a structural clone).                                                                           // 35
// XXX how ok is this? what if there are multiple copies of MongoDB loaded?                             // 36
MongoDB.Timestamp.prototype.clone = function () {                                                       // 37
  // Timestamps should be immutable.                                                                    // 38
  return this;                                                                                          // 39
};                                                                                                      // 40
                                                                                                        // 41
var makeMongoLegal = function (name) { return "EJSON" + name; };                                        // 42
var unmakeMongoLegal = function (name) { return name.substr(5); };                                      // 43
                                                                                                        // 44
var replaceMongoAtomWithMeteor = function (document) {                                                  // 45
  if (document instanceof MongoDB.Binary) {                                                             // 46
    var buffer = document.value(true);                                                                  // 47
    return new Uint8Array(buffer);                                                                      // 48
  }                                                                                                     // 49
  if (document instanceof MongoDB.ObjectID) {                                                           // 50
    return new Meteor.Collection.ObjectID(document.toHexString());                                      // 51
  }                                                                                                     // 52
  if (document["EJSON$type"] && document["EJSON$value"]                                                 // 53
      && _.size(document) === 2) {                                                                      // 54
    return EJSON.fromJSONValue(replaceNames(unmakeMongoLegal, document));                               // 55
  }                                                                                                     // 56
  if (document instanceof MongoDB.Timestamp) {                                                          // 57
    // For now, the Meteor representation of a Mongo timestamp type (not a date!                        // 58
    // this is a weird internal thing used in the oplog!) is the same as the                            // 59
    // Mongo representation. We need to do this explicitly or else we would do a                        // 60
    // structural clone and lose the prototype.                                                         // 61
    return document;                                                                                    // 62
  }                                                                                                     // 63
  return undefined;                                                                                     // 64
};                                                                                                      // 65
                                                                                                        // 66
var replaceMeteorAtomWithMongo = function (document) {                                                  // 67
  if (EJSON.isBinary(document)) {                                                                       // 68
    // This does more copies than we'd like, but is necessary because                                   // 69
    // MongoDB.BSON only looks like it takes a Uint8Array (and doesn't actually                         // 70
    // serialize it correctly).                                                                         // 71
    return new MongoDB.Binary(new Buffer(document));                                                    // 72
  }                                                                                                     // 73
  if (document instanceof Meteor.Collection.ObjectID) {                                                 // 74
    return new MongoDB.ObjectID(document.toHexString());                                                // 75
  }                                                                                                     // 76
  if (document instanceof MongoDB.Timestamp) {                                                          // 77
    // For now, the Meteor representation of a Mongo timestamp type (not a date!                        // 78
    // this is a weird internal thing used in the oplog!) is the same as the                            // 79
    // Mongo representation. We need to do this explicitly or else we would do a                        // 80
    // structural clone and lose the prototype.                                                         // 81
    return document;                                                                                    // 82
  }                                                                                                     // 83
  if (EJSON._isCustomType(document)) {                                                                  // 84
    return replaceNames(makeMongoLegal, EJSON.toJSONValue(document));                                   // 85
  }                                                                                                     // 86
  // It is not ordinarily possible to stick dollar-sign keys into mongo                                 // 87
  // so we don't bother checking for things that need escaping at this time.                            // 88
  return undefined;                                                                                     // 89
};                                                                                                      // 90
                                                                                                        // 91
var replaceTypes = function (document, atomTransformer) {                                               // 92
  if (typeof document !== 'object' || document === null)                                                // 93
    return document;                                                                                    // 94
                                                                                                        // 95
  var replacedTopLevelAtom = atomTransformer(document);                                                 // 96
  if (replacedTopLevelAtom !== undefined)                                                               // 97
    return replacedTopLevelAtom;                                                                        // 98
                                                                                                        // 99
  var ret = document;                                                                                   // 100
  _.each(document, function (val, key) {                                                                // 101
    var valReplaced = replaceTypes(val, atomTransformer);                                               // 102
    if (val !== valReplaced) {                                                                          // 103
      // Lazy clone. Shallow copy.                                                                      // 104
      if (ret === document)                                                                             // 105
        ret = _.clone(document);                                                                        // 106
      ret[key] = valReplaced;                                                                           // 107
    }                                                                                                   // 108
  });                                                                                                   // 109
  return ret;                                                                                           // 110
};                                                                                                      // 111
                                                                                                        // 112
                                                                                                        // 113
MongoConnection = function (url, options) {                                                             // 114
  var self = this;                                                                                      // 115
  options = options || {};                                                                              // 116
  self._connectCallbacks = [];                                                                          // 117
  self._observeMultiplexers = {};                                                                       // 118
  self._onFailoverHook = new Hook;                                                                      // 119
                                                                                                        // 120
  var mongoOptions = {db: {safe: true}, server: {}, replSet: {}};                                       // 121
                                                                                                        // 122
  // Set autoReconnect to true, unless passed on the URL. Why someone                                   // 123
  // would want to set autoReconnect to false, I'm not really sure, but                                 // 124
  // keeping this for backwards compatibility for now.                                                  // 125
  if (!(/[\?&]auto_?[rR]econnect=/.test(url))) {                                                        // 126
    mongoOptions.server.auto_reconnect = true;                                                          // 127
  }                                                                                                     // 128
                                                                                                        // 129
  // Disable the native parser by default, unless specifically enabled                                  // 130
  // in the mongo URL.                                                                                  // 131
  // - The native driver can cause errors which normally would be                                       // 132
  //   thrown, caught, and handled into segfaults that take down the                                    // 133
  //   whole app.                                                                                       // 134
  // - Binary modules don't yet work when you bundle and move the bundle                                // 135
  //   to a different platform (aka deploy)                                                             // 136
  // We should revisit this after binary npm module support lands.                                      // 137
  if (!(/[\?&]native_?[pP]arser=/.test(url))) {                                                         // 138
    mongoOptions.db.native_parser = false;                                                              // 139
  }                                                                                                     // 140
                                                                                                        // 141
  // XXX maybe we should have a better way of allowing users to configure the                           // 142
  // underlying Mongo driver                                                                            // 143
  if (_.has(options, 'poolSize')) {                                                                     // 144
    // If we just set this for "server", replSet will override it. If we just                           // 145
    // set it for replSet, it will be ignored if we're not using a replSet.                             // 146
    mongoOptions.server.poolSize = options.poolSize;                                                    // 147
    mongoOptions.replSet.poolSize = options.poolSize;                                                   // 148
  }                                                                                                     // 149
                                                                                                        // 150
  MongoDB.connect(url, mongoOptions, Meteor.bindEnvironment(function(err, db) {                         // 151
    if (err)                                                                                            // 152
      throw err;                                                                                        // 153
    self.db = db;                                                                                       // 154
    // We keep track of the ReplSet's primary, so that we can trigger hooks when                        // 155
    // it changes.  The Node driver's joined callback seems to fire way too                             // 156
    // often, which is why we need to track it ourselves.                                               // 157
    self._primary = null;                                                                               // 158
    // First, figure out what the current primary is, if any.                                           // 159
    if (self.db.serverConfig._state.master)                                                             // 160
      self._primary = self.db.serverConfig._state.master.name;                                          // 161
    self.db.serverConfig.on(                                                                            // 162
      'joined', Meteor.bindEnvironment(function (kind, doc) {                                           // 163
        if (kind === 'primary') {                                                                       // 164
          if (doc.primary !== self._primary) {                                                          // 165
            self._primary = doc.primary;                                                                // 166
            self._onFailoverHook.each(function (callback) {                                             // 167
              callback();                                                                               // 168
              return true;                                                                              // 169
            });                                                                                         // 170
          }                                                                                             // 171
        } else if (doc.me === self._primary) {                                                          // 172
          // The thing we thought was primary is now something other than                               // 173
          // primary.  Forget that we thought it was primary.  (This means that                         // 174
          // if a server stops being primary and then starts being primary again                        // 175
          // without another server becoming primary in the middle, we'll                               // 176
          // correctly count it as a failover.)                                                         // 177
          self._primary = null;                                                                         // 178
        }                                                                                               // 179
    }));                                                                                                // 180
                                                                                                        // 181
    // drain queue of pending callbacks                                                                 // 182
    _.each(self._connectCallbacks, function (c) {                                                       // 183
      c(db);                                                                                            // 184
    });                                                                                                 // 185
  }));                                                                                                  // 186
                                                                                                        // 187
  self._docFetcher = new DocFetcher(self);                                                              // 188
  self._oplogHandle = null;                                                                             // 189
                                                                                                        // 190
  if (options.oplogUrl && !Package['disable-oplog']) {                                                  // 191
    var dbNameFuture = new Future;                                                                      // 192
    self._withDb(function (db) {                                                                        // 193
      dbNameFuture.return(db.databaseName);                                                             // 194
    });                                                                                                 // 195
    self._oplogHandle = new OplogHandle(options.oplogUrl, dbNameFuture.wait());                         // 196
  }                                                                                                     // 197
};                                                                                                      // 198
                                                                                                        // 199
MongoConnection.prototype.close = function() {                                                          // 200
  var self = this;                                                                                      // 201
                                                                                                        // 202
  // XXX probably untested                                                                              // 203
  var oplogHandle = self._oplogHandle;                                                                  // 204
  self._oplogHandle = null;                                                                             // 205
  if (oplogHandle)                                                                                      // 206
    oplogHandle.stop();                                                                                 // 207
                                                                                                        // 208
  // Use Future.wrap so that errors get thrown. This happens to                                         // 209
  // work even outside a fiber since the 'close' method is not                                          // 210
  // actually asynchronous.                                                                             // 211
  Future.wrap(_.bind(self.db.close, self.db))(true).wait();                                             // 212
};                                                                                                      // 213
                                                                                                        // 214
MongoConnection.prototype._withDb = function (callback) {                                               // 215
  var self = this;                                                                                      // 216
  if (self.db) {                                                                                        // 217
    callback(self.db);                                                                                  // 218
  } else {                                                                                              // 219
    self._connectCallbacks.push(callback);                                                              // 220
  }                                                                                                     // 221
};                                                                                                      // 222
                                                                                                        // 223
// Returns the Mongo Collection object; may yield.                                                      // 224
MongoConnection.prototype._getCollection = function (collectionName) {                                  // 225
  var self = this;                                                                                      // 226
                                                                                                        // 227
  var future = new Future;                                                                              // 228
  self._withDb(function (db) {                                                                          // 229
    db.collection(collectionName, future.resolver());                                                   // 230
  });                                                                                                   // 231
  return future.wait();                                                                                 // 232
};                                                                                                      // 233
                                                                                                        // 234
MongoConnection.prototype._createCappedCollection = function (collectionName,                           // 235
                                                              byteSize) {                               // 236
  var self = this;                                                                                      // 237
  var future = new Future();                                                                            // 238
  self._withDb(function (db) {                                                                          // 239
    db.createCollection(collectionName, {capped: true, size: byteSize},                                 // 240
                        future.resolver());                                                             // 241
  });                                                                                                   // 242
  future.wait();                                                                                        // 243
};                                                                                                      // 244
                                                                                                        // 245
// This should be called synchronously with a write, to create a                                        // 246
// transaction on the current write fence, if any. After we can read                                    // 247
// the write, and after observers have been notified (or at least,                                      // 248
// after the observer notifiers have added themselves to the write                                      // 249
// fence), you should call 'committed()' on the object returned.                                        // 250
MongoConnection.prototype._maybeBeginWrite = function () {                                              // 251
  var self = this;                                                                                      // 252
  var fence = DDPServer._CurrentWriteFence.get();                                                       // 253
  if (fence)                                                                                            // 254
    return fence.beginWrite();                                                                          // 255
  else                                                                                                  // 256
    return {committed: function () {}};                                                                 // 257
};                                                                                                      // 258
                                                                                                        // 259
// Internal interface: adds a callback which is called when the Mongo primary                           // 260
// changes. Returns a stop handle.                                                                      // 261
MongoConnection.prototype._onFailover = function (callback) {                                           // 262
  return this._onFailoverHook.register(callback);                                                       // 263
};                                                                                                      // 264
                                                                                                        // 265
                                                                                                        // 266
//////////// Public API //////////                                                                      // 267
                                                                                                        // 268
// The write methods block until the database has confirmed the write (it may                           // 269
// not be replicated or stable on disk, but one server has confirmed it) if no                          // 270
// callback is provided. If a callback is provided, then they call the callback                         // 271
// when the write is confirmed. They return nothing on success, and raise an                            // 272
// exception on failure.                                                                                // 273
//                                                                                                      // 274
// After making a write (with insert, update, remove), observers are                                    // 275
// notified asynchronously. If you want to receive a callback once all                                  // 276
// of the observer notifications have landed for your write, do the                                     // 277
// writes inside a write fence (set DDPServer._CurrentWriteFence to a new                               // 278
// _WriteFence, and then set a callback on the write fence.)                                            // 279
//                                                                                                      // 280
// Since our execution environment is single-threaded, this is                                          // 281
// well-defined -- a write "has been made" if it's returned, and an                                     // 282
// observer "has been notified" if its callback has returned.                                           // 283
                                                                                                        // 284
var writeCallback = function (write, refresh, callback) {                                               // 285
  return function (err, result) {                                                                       // 286
    if (! err) {                                                                                        // 287
      // XXX We don't have to run this on error, right?                                                 // 288
      refresh();                                                                                        // 289
    }                                                                                                   // 290
    write.committed();                                                                                  // 291
    if (callback)                                                                                       // 292
      callback(err, result);                                                                            // 293
    else if (err)                                                                                       // 294
      throw err;                                                                                        // 295
  };                                                                                                    // 296
};                                                                                                      // 297
                                                                                                        // 298
var bindEnvironmentForWrite = function (callback) {                                                     // 299
  return Meteor.bindEnvironment(callback, "Mongo write");                                               // 300
};                                                                                                      // 301
                                                                                                        // 302
MongoConnection.prototype._insert = function (collection_name, document,                                // 303
                                              callback) {                                               // 304
  var self = this;                                                                                      // 305
                                                                                                        // 306
  var sendError = function (e) {                                                                        // 307
    if (callback)                                                                                       // 308
      return callback(e);                                                                               // 309
    throw e;                                                                                            // 310
  };                                                                                                    // 311
                                                                                                        // 312
  if (collection_name === "___meteor_failure_test_collection") {                                        // 313
    var e = new Error("Failure test");                                                                  // 314
    e.expected = true;                                                                                  // 315
    sendError(e);                                                                                       // 316
    return;                                                                                             // 317
  }                                                                                                     // 318
                                                                                                        // 319
  if (!(LocalCollection._isPlainObject(document) &&                                                     // 320
        !EJSON._isCustomType(document))) {                                                              // 321
    sendError(new Error(                                                                                // 322
      "Only documents (plain objects) may be inserted into MongoDB"));                                  // 323
    return;                                                                                             // 324
  }                                                                                                     // 325
                                                                                                        // 326
  var write = self._maybeBeginWrite();                                                                  // 327
  var refresh = function () {                                                                           // 328
    Meteor.refresh({collection: collection_name, id: document._id });                                   // 329
  };                                                                                                    // 330
  callback = bindEnvironmentForWrite(writeCallback(write, refresh, callback));                          // 331
  try {                                                                                                 // 332
    var collection = self._getCollection(collection_name);                                              // 333
    collection.insert(replaceTypes(document, replaceMeteorAtomWithMongo),                               // 334
                      {safe: true}, callback);                                                          // 335
  } catch (e) {                                                                                         // 336
    write.committed();                                                                                  // 337
    throw e;                                                                                            // 338
  }                                                                                                     // 339
};                                                                                                      // 340
                                                                                                        // 341
// Cause queries that may be affected by the selector to poll in this write                             // 342
// fence.                                                                                               // 343
MongoConnection.prototype._refresh = function (collectionName, selector) {                              // 344
  var self = this;                                                                                      // 345
  var refreshKey = {collection: collectionName};                                                        // 346
  // If we know which documents we're removing, don't poll queries that are                             // 347
  // specific to other documents. (Note that multiple notifications here should                         // 348
  // not cause multiple polls, since all our listener is doing is enqueueing a                          // 349
  // poll.)                                                                                             // 350
  var specificIds = LocalCollection._idsMatchedBySelector(selector);                                    // 351
  if (specificIds) {                                                                                    // 352
    _.each(specificIds, function (id) {                                                                 // 353
      Meteor.refresh(_.extend({id: id}, refreshKey));                                                   // 354
    });                                                                                                 // 355
  } else {                                                                                              // 356
    Meteor.refresh(refreshKey);                                                                         // 357
  }                                                                                                     // 358
};                                                                                                      // 359
                                                                                                        // 360
MongoConnection.prototype._remove = function (collection_name, selector,                                // 361
                                              callback) {                                               // 362
  var self = this;                                                                                      // 363
                                                                                                        // 364
  if (collection_name === "___meteor_failure_test_collection") {                                        // 365
    var e = new Error("Failure test");                                                                  // 366
    e.expected = true;                                                                                  // 367
    if (callback)                                                                                       // 368
      return callback(e);                                                                               // 369
    else                                                                                                // 370
      throw e;                                                                                          // 371
  }                                                                                                     // 372
                                                                                                        // 373
  var write = self._maybeBeginWrite();                                                                  // 374
  var refresh = function () {                                                                           // 375
    self._refresh(collection_name, selector);                                                           // 376
  };                                                                                                    // 377
  callback = bindEnvironmentForWrite(writeCallback(write, refresh, callback));                          // 378
                                                                                                        // 379
  try {                                                                                                 // 380
    var collection = self._getCollection(collection_name);                                              // 381
    collection.remove(replaceTypes(selector, replaceMeteorAtomWithMongo),                               // 382
                      {safe: true}, callback);                                                          // 383
  } catch (e) {                                                                                         // 384
    write.committed();                                                                                  // 385
    throw e;                                                                                            // 386
  }                                                                                                     // 387
};                                                                                                      // 388
                                                                                                        // 389
MongoConnection.prototype._dropCollection = function (collectionName, cb) {                             // 390
  var self = this;                                                                                      // 391
                                                                                                        // 392
  var write = self._maybeBeginWrite();                                                                  // 393
  var refresh = function () {                                                                           // 394
    Meteor.refresh({collection: collectionName, id: null,                                               // 395
                    dropCollection: true});                                                             // 396
  };                                                                                                    // 397
  cb = bindEnvironmentForWrite(writeCallback(write, refresh, cb));                                      // 398
                                                                                                        // 399
  try {                                                                                                 // 400
    var collection = self._getCollection(collectionName);                                               // 401
    collection.drop(cb);                                                                                // 402
  } catch (e) {                                                                                         // 403
    write.committed();                                                                                  // 404
    throw e;                                                                                            // 405
  }                                                                                                     // 406
};                                                                                                      // 407
                                                                                                        // 408
MongoConnection.prototype._update = function (collection_name, selector, mod,                           // 409
                                              options, callback) {                                      // 410
  var self = this;                                                                                      // 411
                                                                                                        // 412
  if (! callback && options instanceof Function) {                                                      // 413
    callback = options;                                                                                 // 414
    options = null;                                                                                     // 415
  }                                                                                                     // 416
                                                                                                        // 417
  if (collection_name === "___meteor_failure_test_collection") {                                        // 418
    var e = new Error("Failure test");                                                                  // 419
    e.expected = true;                                                                                  // 420
    if (callback)                                                                                       // 421
      return callback(e);                                                                               // 422
    else                                                                                                // 423
      throw e;                                                                                          // 424
  }                                                                                                     // 425
                                                                                                        // 426
  // explicit safety check. null and undefined can crash the mongo                                      // 427
  // driver. Although the node driver and minimongo do 'support'                                        // 428
  // non-object modifier in that they don't crash, they are not                                         // 429
  // meaningful operations and do not do anything. Defensively throw an                                 // 430
  // error here.                                                                                        // 431
  if (!mod || typeof mod !== 'object')                                                                  // 432
    throw new Error("Invalid modifier. Modifier must be an object.");                                   // 433
                                                                                                        // 434
  if (!options) options = {};                                                                           // 435
                                                                                                        // 436
  var write = self._maybeBeginWrite();                                                                  // 437
  var refresh = function () {                                                                           // 438
    self._refresh(collection_name, selector);                                                           // 439
  };                                                                                                    // 440
  callback = writeCallback(write, refresh, callback);                                                   // 441
  try {                                                                                                 // 442
    var collection = self._getCollection(collection_name);                                              // 443
    var mongoOpts = {safe: true};                                                                       // 444
    // explictly enumerate options that minimongo supports                                              // 445
    if (options.upsert) mongoOpts.upsert = true;                                                        // 446
    if (options.multi) mongoOpts.multi = true;                                                          // 447
                                                                                                        // 448
    var mongoSelector = replaceTypes(selector, replaceMeteorAtomWithMongo);                             // 449
    var mongoMod = replaceTypes(mod, replaceMeteorAtomWithMongo);                                       // 450
                                                                                                        // 451
    var isModify = isModificationMod(mongoMod);                                                         // 452
    var knownId = (isModify ? selector._id : mod._id);                                                  // 453
                                                                                                        // 454
    if (options.upsert && (! knownId) && options.insertedId) {                                          // 455
      // XXX In future we could do a real upsert for the mongo id generation                            // 456
      // case, if the the node mongo driver gives us back the id of the upserted                        // 457
      // doc (which our current version does not).                                                      // 458
      simulateUpsertWithInsertedId(                                                                     // 459
        collection, mongoSelector, mongoMod,                                                            // 460
        isModify, options,                                                                              // 461
        // This callback does not need to be bindEnvironment'ed because                                 // 462
        // simulateUpsertWithInsertedId() wraps it and then passes it through                           // 463
        // bindEnvironmentForWrite.                                                                     // 464
        function (err, result) {                                                                        // 465
          // If we got here via a upsert() call, then options._returnObject will                        // 466
          // be set and we should return the whole object. Otherwise, we should                         // 467
          // just return the number of affected docs to match the mongo API.                            // 468
          if (result && ! options._returnObject)                                                        // 469
            callback(err, result.numberAffected);                                                       // 470
          else                                                                                          // 471
            callback(err, result);                                                                      // 472
        }                                                                                               // 473
      );                                                                                                // 474
    } else {                                                                                            // 475
      collection.update(                                                                                // 476
        mongoSelector, mongoMod, mongoOpts,                                                             // 477
        bindEnvironmentForWrite(function (err, result, extra) {                                         // 478
          if (! err) {                                                                                  // 479
            if (result && options._returnObject) {                                                      // 480
              result = { numberAffected: result };                                                      // 481
              // If this was an upsert() call, and we ended up                                          // 482
              // inserting a new doc and we know its id, then                                           // 483
              // return that id as well.                                                                // 484
              if (options.upsert && knownId &&                                                          // 485
                  ! extra.updatedExisting)                                                              // 486
                result.insertedId = knownId;                                                            // 487
            }                                                                                           // 488
          }                                                                                             // 489
          callback(err, result);                                                                        // 490
        }));                                                                                            // 491
    }                                                                                                   // 492
  } catch (e) {                                                                                         // 493
    write.committed();                                                                                  // 494
    throw e;                                                                                            // 495
  }                                                                                                     // 496
};                                                                                                      // 497
                                                                                                        // 498
var isModificationMod = function (mod) {                                                                // 499
  for (var k in mod)                                                                                    // 500
    if (k.substr(0, 1) === '$')                                                                         // 501
      return true;                                                                                      // 502
  return false;                                                                                         // 503
};                                                                                                      // 504
                                                                                                        // 505
var NUM_OPTIMISTIC_TRIES = 3;                                                                           // 506
                                                                                                        // 507
// exposed for testing                                                                                  // 508
MongoConnection._isCannotChangeIdError = function (err) {                                               // 509
  // either of these checks should work, but just to be safe...                                         // 510
  return (err.code === 13596 ||                                                                         // 511
          err.err.indexOf("cannot change _id of a document") === 0);                                    // 512
};                                                                                                      // 513
                                                                                                        // 514
var simulateUpsertWithInsertedId = function (collection, selector, mod,                                 // 515
                                             isModify, options, callback) {                             // 516
  // STRATEGY:  First try doing a plain update.  If it affected 0 documents,                            // 517
  // then without affecting the database, we know we should probably do an                              // 518
  // insert.  We then do a *conditional* insert that will fail in the case                              // 519
  // of a race condition.  This conditional insert is actually an                                       // 520
  // upsert-replace with an _id, which will never successfully update an                                // 521
  // existing document.  If this upsert fails with an error saying it                                   // 522
  // couldn't change an existing _id, then we know an intervening write has                             // 523
  // caused the query to match something.  We go back to step one and repeat.                           // 524
  // Like all "optimistic write" schemes, we rely on the fact that it's                                 // 525
  // unlikely our writes will continue to be interfered with under normal                               // 526
  // circumstances (though sufficiently heavy contention with writers                                   // 527
  // disagreeing on the existence of an object will cause writes to fail                                // 528
  // in theory).                                                                                        // 529
                                                                                                        // 530
  var newDoc;                                                                                           // 531
  // Run this code up front so that it fails fast if someone uses                                       // 532
  // a Mongo update operator we don't support.                                                          // 533
  if (isModify) {                                                                                       // 534
    // We've already run replaceTypes/replaceMeteorAtomWithMongo on                                     // 535
    // selector and mod.  We assume it doesn't matter, as far as                                        // 536
    // the behavior of modifiers is concerned, whether `_modify`                                        // 537
    // is run on EJSON or on mongo-converted EJSON.                                                     // 538
    var selectorDoc = LocalCollection._removeDollarOperators(selector);                                 // 539
    LocalCollection._modify(selectorDoc, mod, {isInsert: true});                                        // 540
    newDoc = selectorDoc;                                                                               // 541
  } else {                                                                                              // 542
    newDoc = mod;                                                                                       // 543
  }                                                                                                     // 544
                                                                                                        // 545
  var insertedId = options.insertedId; // must exist                                                    // 546
  var mongoOptsForUpdate = {                                                                            // 547
    safe: true,                                                                                         // 548
    multi: options.multi                                                                                // 549
  };                                                                                                    // 550
  var mongoOptsForInsert = {                                                                            // 551
    safe: true,                                                                                         // 552
    upsert: true                                                                                        // 553
  };                                                                                                    // 554
                                                                                                        // 555
  var tries = NUM_OPTIMISTIC_TRIES;                                                                     // 556
                                                                                                        // 557
  var doUpdate = function () {                                                                          // 558
    tries--;                                                                                            // 559
    if (! tries) {                                                                                      // 560
      callback(new Error("Upsert failed after " + NUM_OPTIMISTIC_TRIES + " tries."));                   // 561
    } else {                                                                                            // 562
      collection.update(selector, mod, mongoOptsForUpdate,                                              // 563
                        bindEnvironmentForWrite(function (err, result) {                                // 564
                          if (err)                                                                      // 565
                            callback(err);                                                              // 566
                          else if (result)                                                              // 567
                            callback(null, {                                                            // 568
                              numberAffected: result                                                    // 569
                            });                                                                         // 570
                          else                                                                          // 571
                            doConditionalInsert();                                                      // 572
                        }));                                                                            // 573
    }                                                                                                   // 574
  };                                                                                                    // 575
                                                                                                        // 576
  var doConditionalInsert = function () {                                                               // 577
    var replacementWithId = _.extend(                                                                   // 578
      replaceTypes({_id: insertedId}, replaceMeteorAtomWithMongo),                                      // 579
      newDoc);                                                                                          // 580
    collection.update(selector, replacementWithId, mongoOptsForInsert,                                  // 581
                      bindEnvironmentForWrite(function (err, result) {                                  // 582
                        if (err) {                                                                      // 583
                          // figure out if this is a                                                    // 584
                          // "cannot change _id of document" error, and                                 // 585
                          // if so, try doUpdate() again, up to 3 times.                                // 586
                          if (MongoConnection._isCannotChangeIdError(err)) {                            // 587
                            doUpdate();                                                                 // 588
                          } else {                                                                      // 589
                            callback(err);                                                              // 590
                          }                                                                             // 591
                        } else {                                                                        // 592
                          callback(null, {                                                              // 593
                            numberAffected: result,                                                     // 594
                            insertedId: insertedId                                                      // 595
                          });                                                                           // 596
                        }                                                                               // 597
                      }));                                                                              // 598
  };                                                                                                    // 599
                                                                                                        // 600
  doUpdate();                                                                                           // 601
};                                                                                                      // 602
                                                                                                        // 603
_.each(["insert", "update", "remove", "dropCollection"], function (method) {                            // 604
  MongoConnection.prototype[method] = function (/* arguments */) {                                      // 605
    var self = this;                                                                                    // 606
    return Meteor._wrapAsync(self["_" + method]).apply(self, arguments);                                // 607
  };                                                                                                    // 608
});                                                                                                     // 609
                                                                                                        // 610
// XXX MongoConnection.upsert() does not return the id of the inserted document                         // 611
// unless you set it explicitly in the selector or modifier (as a replacement                           // 612
// doc).                                                                                                // 613
MongoConnection.prototype.upsert = function (collectionName, selector, mod,                             // 614
                                             options, callback) {                                       // 615
  var self = this;                                                                                      // 616
  if (typeof options === "function" && ! callback) {                                                    // 617
    callback = options;                                                                                 // 618
    options = {};                                                                                       // 619
  }                                                                                                     // 620
                                                                                                        // 621
  return self.update(collectionName, selector, mod,                                                     // 622
                     _.extend({}, options, {                                                            // 623
                       upsert: true,                                                                    // 624
                       _returnObject: true                                                              // 625
                     }), callback);                                                                     // 626
};                                                                                                      // 627
                                                                                                        // 628
MongoConnection.prototype.find = function (collectionName, selector, options) {                         // 629
  var self = this;                                                                                      // 630
                                                                                                        // 631
  if (arguments.length === 1)                                                                           // 632
    selector = {};                                                                                      // 633
                                                                                                        // 634
  return new Cursor(                                                                                    // 635
    self, new CursorDescription(collectionName, selector, options));                                    // 636
};                                                                                                      // 637
                                                                                                        // 638
MongoConnection.prototype.findOne = function (collection_name, selector,                                // 639
                                              options) {                                                // 640
  var self = this;                                                                                      // 641
  if (arguments.length === 1)                                                                           // 642
    selector = {};                                                                                      // 643
                                                                                                        // 644
  options = options || {};                                                                              // 645
  options.limit = 1;                                                                                    // 646
  return self.find(collection_name, selector, options).fetch()[0];                                      // 647
};                                                                                                      // 648
                                                                                                        // 649
// We'll actually design an index API later. For now, we just pass through to                           // 650
// Mongo's, but make it synchronous.                                                                    // 651
MongoConnection.prototype._ensureIndex = function (collectionName, index,                               // 652
                                                   options) {                                           // 653
  var self = this;                                                                                      // 654
  options = _.extend({safe: true}, options);                                                            // 655
                                                                                                        // 656
  // We expect this function to be called at startup, not from within a method,                         // 657
  // so we don't interact with the write fence.                                                         // 658
  var collection = self._getCollection(collectionName);                                                 // 659
  var future = new Future;                                                                              // 660
  var indexName = collection.ensureIndex(index, options, future.resolver());                            // 661
  future.wait();                                                                                        // 662
};                                                                                                      // 663
MongoConnection.prototype._dropIndex = function (collectionName, index) {                               // 664
  var self = this;                                                                                      // 665
                                                                                                        // 666
  // This function is only used by test code, not within a method, so we don't                          // 667
  // interact with the write fence.                                                                     // 668
  var collection = self._getCollection(collectionName);                                                 // 669
  var future = new Future;                                                                              // 670
  var indexName = collection.dropIndex(index, future.resolver());                                       // 671
  future.wait();                                                                                        // 672
};                                                                                                      // 673
                                                                                                        // 674
// CURSORS                                                                                              // 675
                                                                                                        // 676
// There are several classes which relate to cursors:                                                   // 677
//                                                                                                      // 678
// CursorDescription represents the arguments used to construct a cursor:                               // 679
// collectionName, selector, and (find) options.  Because it is used as a key                           // 680
// for cursor de-dup, everything in it should either be JSON-stringifiable or                           // 681
// not affect observeChanges output (eg, options.transform functions are not                            // 682
// stringifiable but do not affect observeChanges).                                                     // 683
//                                                                                                      // 684
// SynchronousCursor is a wrapper around a MongoDB cursor                                               // 685
// which includes fully-synchronous versions of forEach, etc.                                           // 686
//                                                                                                      // 687
// Cursor is the cursor object returned from find(), which implements the                               // 688
// documented Meteor.Collection cursor API.  It wraps a CursorDescription and a                         // 689
// SynchronousCursor (lazily: it doesn't contact Mongo until you call a method                          // 690
// like fetch or forEach on it).                                                                        // 691
//                                                                                                      // 692
// ObserveHandle is the "observe handle" returned from observeChanges. It has a                         // 693
// reference to an ObserveMultiplexer.                                                                  // 694
//                                                                                                      // 695
// ObserveMultiplexer allows multiple identical ObserveHandles to be driven by a                        // 696
// single observe driver.                                                                               // 697
//                                                                                                      // 698
// There are two "observe drivers" which drive ObserveMultiplexers:                                     // 699
//   - PollingObserveDriver caches the results of a query and reruns it when                            // 700
//     necessary.                                                                                       // 701
//   - OplogObserveDriver follows the Mongo operation log to directly observe                           // 702
//     database changes.                                                                                // 703
// Both implementations follow the same simple interface: when you create them,                         // 704
// they start sending observeChanges callbacks (and a ready() invocation) to                            // 705
// their ObserveMultiplexer, and you stop them by calling their stop() method.                          // 706
                                                                                                        // 707
CursorDescription = function (collectionName, selector, options) {                                      // 708
  var self = this;                                                                                      // 709
  self.collectionName = collectionName;                                                                 // 710
  self.selector = Meteor.Collection._rewriteSelector(selector);                                         // 711
  self.options = options || {};                                                                         // 712
};                                                                                                      // 713
                                                                                                        // 714
Cursor = function (mongo, cursorDescription) {                                                          // 715
  var self = this;                                                                                      // 716
                                                                                                        // 717
  self._mongo = mongo;                                                                                  // 718
  self._cursorDescription = cursorDescription;                                                          // 719
  self._synchronousCursor = null;                                                                       // 720
};                                                                                                      // 721
                                                                                                        // 722
_.each(['forEach', 'map', 'fetch', 'count'], function (method) {                                        // 723
  Cursor.prototype[method] = function () {                                                              // 724
    var self = this;                                                                                    // 725
                                                                                                        // 726
    // You can only observe a tailable cursor.                                                          // 727
    if (self._cursorDescription.options.tailable)                                                       // 728
      throw new Error("Cannot call " + method + " on a tailable cursor");                               // 729
                                                                                                        // 730
    if (!self._synchronousCursor) {                                                                     // 731
      self._synchronousCursor = self._mongo._createSynchronousCursor(                                   // 732
        self._cursorDescription, {                                                                      // 733
          // Make sure that the "self" argument to forEach/map callbacks is the                         // 734
          // Cursor, not the SynchronousCursor.                                                         // 735
          selfForIteration: self,                                                                       // 736
          useTransform: true                                                                            // 737
        });                                                                                             // 738
    }                                                                                                   // 739
                                                                                                        // 740
    return self._synchronousCursor[method].apply(                                                       // 741
      self._synchronousCursor, arguments);                                                              // 742
  };                                                                                                    // 743
});                                                                                                     // 744
                                                                                                        // 745
// Since we don't actually have a "nextObject" interface, there's really no                             // 746
// reason to have a "rewind" interface.  All it did was make multiple calls                             // 747
// to fetch/map/forEach return nothing the second time.                                                 // 748
// XXX COMPAT WITH 0.8.1                                                                                // 749
Cursor.prototype.rewind = function () {                                                                 // 750
};                                                                                                      // 751
                                                                                                        // 752
Cursor.prototype.getTransform = function () {                                                           // 753
  return this._cursorDescription.options.transform;                                                     // 754
};                                                                                                      // 755
                                                                                                        // 756
// When you call Meteor.publish() with a function that returns a Cursor, we need                        // 757
// to transmute it into the equivalent subscription.  This is the function that                         // 758
// does that.                                                                                           // 759
                                                                                                        // 760
Cursor.prototype._publishCursor = function (sub) {                                                      // 761
  var self = this;                                                                                      // 762
  var collection = self._cursorDescription.collectionName;                                              // 763
  return Meteor.Collection._publishCursor(self, sub, collection);                                       // 764
};                                                                                                      // 765
                                                                                                        // 766
// Used to guarantee that publish functions return at most one cursor per                               // 767
// collection. Private, because we might later have cursors that include                                // 768
// documents from multiple collections somehow.                                                         // 769
Cursor.prototype._getCollectionName = function () {                                                     // 770
  var self = this;                                                                                      // 771
  return self._cursorDescription.collectionName;                                                        // 772
}                                                                                                       // 773
                                                                                                        // 774
Cursor.prototype.observe = function (callbacks) {                                                       // 775
  var self = this;                                                                                      // 776
  return LocalCollection._observeFromObserveChanges(self, callbacks);                                   // 777
};                                                                                                      // 778
                                                                                                        // 779
Cursor.prototype.observeChanges = function (callbacks) {                                                // 780
  var self = this;                                                                                      // 781
  var ordered = LocalCollection._observeChangesCallbacksAreOrdered(callbacks);                          // 782
  return self._mongo._observeChanges(                                                                   // 783
    self._cursorDescription, ordered, callbacks);                                                       // 784
};                                                                                                      // 785
                                                                                                        // 786
MongoConnection.prototype._createSynchronousCursor = function(                                          // 787
    cursorDescription, options) {                                                                       // 788
  var self = this;                                                                                      // 789
  options = _.pick(options || {}, 'selfForIteration', 'useTransform');                                  // 790
                                                                                                        // 791
  var collection = self._getCollection(cursorDescription.collectionName);                               // 792
  var cursorOptions = cursorDescription.options;                                                        // 793
  var mongoOptions = {                                                                                  // 794
    sort: cursorOptions.sort,                                                                           // 795
    limit: cursorOptions.limit,                                                                         // 796
    skip: cursorOptions.skip                                                                            // 797
  };                                                                                                    // 798
                                                                                                        // 799
  // Do we want a tailable cursor (which only works on capped collections)?                             // 800
  if (cursorOptions.tailable) {                                                                         // 801
    // We want a tailable cursor...                                                                     // 802
    mongoOptions.tailable = true;                                                                       // 803
    // ... and for the server to wait a bit if any getMore has no data (rather                          // 804
    // than making us put the relevant sleeps in the client)...                                         // 805
    mongoOptions.awaitdata = true;                                                                      // 806
    // ... and to keep querying the server indefinitely rather than just 5 times                        // 807
    // if there's no more data.                                                                         // 808
    mongoOptions.numberOfRetries = -1;                                                                  // 809
    // And if this is on the oplog collection and the cursor specifies a 'ts',                          // 810
    // then set the undocumented oplog replay flag, which does a special scan to                        // 811
    // find the first document (instead of creating an index on ts). This is a                          // 812
    // very hard-coded Mongo flag which only works on the oplog collection and                          // 813
    // only works with the ts field.                                                                    // 814
    if (cursorDescription.collectionName === OPLOG_COLLECTION &&                                        // 815
        cursorDescription.selector.ts) {                                                                // 816
      mongoOptions.oplogReplay = true;                                                                  // 817
    }                                                                                                   // 818
  }                                                                                                     // 819
                                                                                                        // 820
  var dbCursor = collection.find(                                                                       // 821
    replaceTypes(cursorDescription.selector, replaceMeteorAtomWithMongo),                               // 822
    cursorOptions.fields, mongoOptions);                                                                // 823
                                                                                                        // 824
  return new SynchronousCursor(dbCursor, cursorDescription, options);                                   // 825
};                                                                                                      // 826
                                                                                                        // 827
var SynchronousCursor = function (dbCursor, cursorDescription, options) {                               // 828
  var self = this;                                                                                      // 829
  options = _.pick(options || {}, 'selfForIteration', 'useTransform');                                  // 830
                                                                                                        // 831
  self._dbCursor = dbCursor;                                                                            // 832
  self._cursorDescription = cursorDescription;                                                          // 833
  // The "self" argument passed to forEach/map callbacks. If we're wrapped                              // 834
  // inside a user-visible Cursor, we want to provide the outer cursor!                                 // 835
  self._selfForIteration = options.selfForIteration || self;                                            // 836
  if (options.useTransform && cursorDescription.options.transform) {                                    // 837
    self._transform = LocalCollection.wrapTransform(                                                    // 838
      cursorDescription.options.transform);                                                             // 839
  } else {                                                                                              // 840
    self._transform = null;                                                                             // 841
  }                                                                                                     // 842
                                                                                                        // 843
  // Need to specify that the callback is the first argument to nextObject,                             // 844
  // since otherwise when we try to call it with no args the driver will                                // 845
  // interpret "undefined" first arg as an options hash and crash.                                      // 846
  self._synchronousNextObject = Future.wrap(                                                            // 847
    dbCursor.nextObject.bind(dbCursor), 0);                                                             // 848
  self._synchronousCount = Future.wrap(dbCursor.count.bind(dbCursor));                                  // 849
  self._visitedIds = new LocalCollection._IdMap;                                                        // 850
};                                                                                                      // 851
                                                                                                        // 852
_.extend(SynchronousCursor.prototype, {                                                                 // 853
  _nextObject: function () {                                                                            // 854
    var self = this;                                                                                    // 855
                                                                                                        // 856
    while (true) {                                                                                      // 857
      var doc = self._synchronousNextObject().wait();                                                   // 858
                                                                                                        // 859
      if (!doc) return null;                                                                            // 860
      doc = replaceTypes(doc, replaceMongoAtomWithMeteor);                                              // 861
                                                                                                        // 862
      if (!self._cursorDescription.options.tailable && _.has(doc, '_id')) {                             // 863
        // Did Mongo give us duplicate documents in the same cursor? If so,                             // 864
        // ignore this one. (Do this before the transform, since transform might                        // 865
        // return some unrelated value.) We don't do this for tailable cursors,                         // 866
        // because we want to maintain O(1) memory usage. And if there isn't _id                        // 867
        // for some reason (maybe it's the oplog), then we don't do this either.                        // 868
        // (Be careful to do this for falsey but existing _id, though.)                                 // 869
        if (self._visitedIds.has(doc._id)) continue;                                                    // 870
        self._visitedIds.set(doc._id, true);                                                            // 871
      }                                                                                                 // 872
                                                                                                        // 873
      if (self._transform)                                                                              // 874
        doc = self._transform(doc);                                                                     // 875
                                                                                                        // 876
      return doc;                                                                                       // 877
    }                                                                                                   // 878
  },                                                                                                    // 879
                                                                                                        // 880
  forEach: function (callback, thisArg) {                                                               // 881
    var self = this;                                                                                    // 882
                                                                                                        // 883
    // Get back to the beginning.                                                                       // 884
    self._rewind();                                                                                     // 885
                                                                                                        // 886
    // We implement the loop ourself instead of using self._dbCursor.each,                              // 887
    // because "each" will call its callback outside of a fiber which makes it                          // 888
    // much more complex to make this function synchronous.                                             // 889
    var index = 0;                                                                                      // 890
    while (true) {                                                                                      // 891
      var doc = self._nextObject();                                                                     // 892
      if (!doc) return;                                                                                 // 893
      callback.call(thisArg, doc, index++, self._selfForIteration);                                     // 894
    }                                                                                                   // 895
  },                                                                                                    // 896
                                                                                                        // 897
  // XXX Allow overlapping callback executions if callback yields.                                      // 898
  map: function (callback, thisArg) {                                                                   // 899
    var self = this;                                                                                    // 900
    var res = [];                                                                                       // 901
    self.forEach(function (doc, index) {                                                                // 902
      res.push(callback.call(thisArg, doc, index, self._selfForIteration));                             // 903
    });                                                                                                 // 904
    return res;                                                                                         // 905
  },                                                                                                    // 906
                                                                                                        // 907
  _rewind: function () {                                                                                // 908
    var self = this;                                                                                    // 909
                                                                                                        // 910
    // known to be synchronous                                                                          // 911
    self._dbCursor.rewind();                                                                            // 912
                                                                                                        // 913
    self._visitedIds = new LocalCollection._IdMap;                                                      // 914
  },                                                                                                    // 915
                                                                                                        // 916
  // Mostly usable for tailable cursors.                                                                // 917
  close: function () {                                                                                  // 918
    var self = this;                                                                                    // 919
                                                                                                        // 920
    self._dbCursor.close();                                                                             // 921
  },                                                                                                    // 922
                                                                                                        // 923
  fetch: function () {                                                                                  // 924
    var self = this;                                                                                    // 925
    return self.map(_.identity);                                                                        // 926
  },                                                                                                    // 927
                                                                                                        // 928
  count: function () {                                                                                  // 929
    var self = this;                                                                                    // 930
    return self._synchronousCount().wait();                                                             // 931
  },                                                                                                    // 932
                                                                                                        // 933
  // This method is NOT wrapped in Cursor.                                                              // 934
  getRawObjects: function (ordered) {                                                                   // 935
    var self = this;                                                                                    // 936
    if (ordered) {                                                                                      // 937
      return self.fetch();                                                                              // 938
    } else {                                                                                            // 939
      var results = new LocalCollection._IdMap;                                                         // 940
      self.forEach(function (doc) {                                                                     // 941
        results.set(doc._id, doc);                                                                      // 942
      });                                                                                               // 943
      return results;                                                                                   // 944
    }                                                                                                   // 945
  }                                                                                                     // 946
});                                                                                                     // 947
                                                                                                        // 948
MongoConnection.prototype.tail = function (cursorDescription, docCallback) {                            // 949
  var self = this;                                                                                      // 950
  if (!cursorDescription.options.tailable)                                                              // 951
    throw new Error("Can only tail a tailable cursor");                                                 // 952
                                                                                                        // 953
  var cursor = self._createSynchronousCursor(cursorDescription);                                        // 954
                                                                                                        // 955
  var stopped = false;                                                                                  // 956
  var lastTS = undefined;                                                                               // 957
  var loop = function () {                                                                              // 958
    while (true) {                                                                                      // 959
      if (stopped)                                                                                      // 960
        return;                                                                                         // 961
      try {                                                                                             // 962
        var doc = cursor._nextObject();                                                                 // 963
      } catch (err) {                                                                                   // 964
        // There's no good way to figure out if this was actually an error                              // 965
        // from Mongo. Ah well. But either way, we need to retry the cursor                             // 966
        // (unless the failure was because the observe got stopped).                                    // 967
        doc = null;                                                                                     // 968
      }                                                                                                 // 969
      // Since cursor._nextObject can yield, we need to check again to see if                           // 970
      // we've been stopped before calling the callback.                                                // 971
      if (stopped)                                                                                      // 972
        return;                                                                                         // 973
      if (doc) {                                                                                        // 974
        // If a tailable cursor contains a "ts" field, use it to recreate the                           // 975
        // cursor on error. ("ts" is a standard that Mongo uses internally for                          // 976
        // the oplog, and there's a special flag that lets you do binary search                         // 977
        // on it instead of needing to use an index.)                                                   // 978
        lastTS = doc.ts;                                                                                // 979
        docCallback(doc);                                                                               // 980
      } else {                                                                                          // 981
        var newSelector = _.clone(cursorDescription.selector);                                          // 982
        if (lastTS) {                                                                                   // 983
          newSelector.ts = {$gt: lastTS};                                                               // 984
        }                                                                                               // 985
        cursor = self._createSynchronousCursor(new CursorDescription(                                   // 986
          cursorDescription.collectionName,                                                             // 987
          newSelector,                                                                                  // 988
          cursorDescription.options));                                                                  // 989
        // Mongo failover takes many seconds.  Retry in a bit.  (Without this                           // 990
        // setTimeout, we peg the CPU at 100% and never notice the actual                               // 991
        // failover.                                                                                    // 992
        Meteor.setTimeout(loop, 100);                                                                   // 993
        break;                                                                                          // 994
      }                                                                                                 // 995
    }                                                                                                   // 996
  };                                                                                                    // 997
                                                                                                        // 998
  Meteor.defer(loop);                                                                                   // 999
                                                                                                        // 1000
  return {                                                                                              // 1001
    stop: function () {                                                                                 // 1002
      stopped = true;                                                                                   // 1003
      cursor.close();                                                                                   // 1004
    }                                                                                                   // 1005
  };                                                                                                    // 1006
};                                                                                                      // 1007
                                                                                                        // 1008
MongoConnection.prototype._observeChanges = function (                                                  // 1009
    cursorDescription, ordered, callbacks) {                                                            // 1010
  var self = this;                                                                                      // 1011
                                                                                                        // 1012
  if (cursorDescription.options.tailable) {                                                             // 1013
    return self._observeChangesTailable(cursorDescription, ordered, callbacks);                         // 1014
  }                                                                                                     // 1015
                                                                                                        // 1016
  // You may not filter out _id when observing changes, because the id is a core                        // 1017
  // part of the observeChanges API.                                                                    // 1018
  if (cursorDescription.options.fields &&                                                               // 1019
      (cursorDescription.options.fields._id === 0 ||                                                    // 1020
       cursorDescription.options.fields._id === false)) {                                               // 1021
    throw Error("You may not observe a cursor with {fields: {_id: 0}}");                                // 1022
  }                                                                                                     // 1023
                                                                                                        // 1024
  var observeKey = JSON.stringify(                                                                      // 1025
    _.extend({ordered: ordered}, cursorDescription));                                                   // 1026
                                                                                                        // 1027
  var multiplexer, observeDriver;                                                                       // 1028
  var firstHandle = false;                                                                              // 1029
                                                                                                        // 1030
  // Find a matching ObserveMultiplexer, or create a new one. This next block is                        // 1031
  // guaranteed to not yield (and it doesn't call anything that can observe a                           // 1032
  // new query), so no other calls to this function can interleave with it.                             // 1033
  Meteor._noYieldsAllowed(function () {                                                                 // 1034
    if (_.has(self._observeMultiplexers, observeKey)) {                                                 // 1035
      multiplexer = self._observeMultiplexers[observeKey];                                              // 1036
    } else {                                                                                            // 1037
      firstHandle = true;                                                                               // 1038
      // Create a new ObserveMultiplexer.                                                               // 1039
      multiplexer = new ObserveMultiplexer({                                                            // 1040
        ordered: ordered,                                                                               // 1041
        onStop: function () {                                                                           // 1042
          observeDriver.stop();                                                                         // 1043
          delete self._observeMultiplexers[observeKey];                                                 // 1044
        }                                                                                               // 1045
      });                                                                                               // 1046
      self._observeMultiplexers[observeKey] = multiplexer;                                              // 1047
    }                                                                                                   // 1048
  });                                                                                                   // 1049
                                                                                                        // 1050
  var observeHandle = new ObserveHandle(multiplexer, callbacks);                                        // 1051
                                                                                                        // 1052
  if (firstHandle) {                                                                                    // 1053
    var matcher, sorter;                                                                                // 1054
    var canUseOplog = _.all([                                                                           // 1055
      function () {                                                                                     // 1056
        // At a bare minimum, using the oplog requires us to have an oplog, to                          // 1057
        // want unordered callbacks, and to not want a callback on the polls                            // 1058
        // that won't happen.                                                                           // 1059
        return self._oplogHandle && !ordered &&                                                         // 1060
          !callbacks._testOnlyPollCallback;                                                             // 1061
      }, function () {                                                                                  // 1062
        // We need to be able to compile the selector. Fall back to polling for                         // 1063
        // some newfangled $selector that minimongo doesn't support yet.                                // 1064
        try {                                                                                           // 1065
          matcher = new Minimongo.Matcher(cursorDescription.selector);                                  // 1066
          return true;                                                                                  // 1067
        } catch (e) {                                                                                   // 1068
          // XXX make all compilation errors MinimongoError or something                                // 1069
          //     so that this doesn't ignore unrelated exceptions                                       // 1070
          return false;                                                                                 // 1071
        }                                                                                               // 1072
      }, function () {                                                                                  // 1073
        // ... and the selector itself needs to support oplog.                                          // 1074
        return OplogObserveDriver.cursorSupported(cursorDescription, matcher);                          // 1075
      }, function () {                                                                                  // 1076
        // And we need to be able to compile the sort, if any.  eg, can't be                            // 1077
        // {$natural: 1}.                                                                               // 1078
        if (!cursorDescription.options.sort)                                                            // 1079
          return true;                                                                                  // 1080
        try {                                                                                           // 1081
          sorter = new Minimongo.Sorter(cursorDescription.options.sort,                                 // 1082
                                        { matcher: matcher });                                          // 1083
          return true;                                                                                  // 1084
        } catch (e) {                                                                                   // 1085
          // XXX make all compilation errors MinimongoError or something                                // 1086
          //     so that this doesn't ignore unrelated exceptions                                       // 1087
          return false;                                                                                 // 1088
        }                                                                                               // 1089
      }], function (f) { return f(); });  // invoke each function                                       // 1090
                                                                                                        // 1091
    var driverClass = canUseOplog ? OplogObserveDriver : PollingObserveDriver;                          // 1092
    observeDriver = new driverClass({                                                                   // 1093
      cursorDescription: cursorDescription,                                                             // 1094
      mongoHandle: self,                                                                                // 1095
      multiplexer: multiplexer,                                                                         // 1096
      ordered: ordered,                                                                                 // 1097
      matcher: matcher,  // ignored by polling                                                          // 1098
      sorter: sorter,  // ignored by polling                                                            // 1099
      _testOnlyPollCallback: callbacks._testOnlyPollCallback                                            // 1100
    });                                                                                                 // 1101
                                                                                                        // 1102
    // This field is only set for use in tests.                                                         // 1103
    multiplexer._observeDriver = observeDriver;                                                         // 1104
  }                                                                                                     // 1105
                                                                                                        // 1106
  // Blocks until the initial adds have been sent.                                                      // 1107
  multiplexer.addHandleAndSendInitialAdds(observeHandle);                                               // 1108
                                                                                                        // 1109
  return observeHandle;                                                                                 // 1110
};                                                                                                      // 1111
                                                                                                        // 1112
// Listen for the invalidation messages that will trigger us to poll the                                // 1113
// database for changes. If this selector specifies specific IDs, specify them                          // 1114
// here, so that updates to different specific IDs don't cause us to poll.                              // 1115
// listenCallback is the same kind of (notification, complete) callback passed                          // 1116
// to InvalidationCrossbar.listen.                                                                      // 1117
                                                                                                        // 1118
listenAll = function (cursorDescription, listenCallback) {                                              // 1119
  var listeners = [];                                                                                   // 1120
  forEachTrigger(cursorDescription, function (trigger) {                                                // 1121
    listeners.push(DDPServer._InvalidationCrossbar.listen(                                              // 1122
      trigger, listenCallback));                                                                        // 1123
  });                                                                                                   // 1124
                                                                                                        // 1125
  return {                                                                                              // 1126
    stop: function () {                                                                                 // 1127
      _.each(listeners, function (listener) {                                                           // 1128
        listener.stop();                                                                                // 1129
      });                                                                                               // 1130
    }                                                                                                   // 1131
  };                                                                                                    // 1132
};                                                                                                      // 1133
                                                                                                        // 1134
forEachTrigger = function (cursorDescription, triggerCallback) {                                        // 1135
  var key = {collection: cursorDescription.collectionName};                                             // 1136
  var specificIds = LocalCollection._idsMatchedBySelector(                                              // 1137
    cursorDescription.selector);                                                                        // 1138
  if (specificIds) {                                                                                    // 1139
    _.each(specificIds, function (id) {                                                                 // 1140
      triggerCallback(_.extend({id: id}, key));                                                         // 1141
    });                                                                                                 // 1142
    triggerCallback(_.extend({dropCollection: true, id: null}, key));                                   // 1143
  } else {                                                                                              // 1144
    triggerCallback(key);                                                                               // 1145
  }                                                                                                     // 1146
};                                                                                                      // 1147
                                                                                                        // 1148
// observeChanges for tailable cursors on capped collections.                                           // 1149
//                                                                                                      // 1150
// Some differences from normal cursors:                                                                // 1151
//   - Will never produce anything other than 'added' or 'addedBefore'. If you                          // 1152
//     do update a document that has already been produced, this will not notice                        // 1153
//     it.                                                                                              // 1154
//   - If you disconnect and reconnect from Mongo, it will essentially restart                          // 1155
//     the query, which will lead to duplicate results. This is pretty bad,                             // 1156
//     but if you include a field called 'ts' which is inserted as                                      // 1157
//     new MongoInternals.MongoTimestamp(0, 0) (which is initialized to the                             // 1158
//     current Mongo-style timestamp), we'll be able to find the place to                               // 1159
//     restart properly. (This field is specifically understood by Mongo with an                        // 1160
//     optimization which allows it to find the right place to start without                            // 1161
//     an index on ts. It's how the oplog works.)                                                       // 1162
//   - No callbacks are triggered synchronously with the call (there's no                               // 1163
//     differentiation between "initial data" and "later changes"; everything                           // 1164
//     that matches the query gets sent asynchronously).                                                // 1165
//   - De-duplication is not implemented.                                                               // 1166
//   - Does not yet interact with the write fence. Probably, this should work by                        // 1167
//     ignoring removes (which don't work on capped collections) and updates                            // 1168
//     (which don't affect tailable cursors), and just keeping track of the ID                          // 1169
//     of the inserted object, and closing the write fence once you get to that                         // 1170
//     ID (or timestamp?).  This doesn't work well if the document doesn't match                        // 1171
//     the query, though.  On the other hand, the write fence can close                                 // 1172
//     immediately if it does not match the query. So if we trust minimongo                             // 1173
//     enough to accurately evaluate the query against the write fence, we                              // 1174
//     should be able to do this...  Of course, minimongo doesn't even support                          // 1175
//     Mongo Timestamps yet.                                                                            // 1176
MongoConnection.prototype._observeChangesTailable = function (                                          // 1177
    cursorDescription, ordered, callbacks) {                                                            // 1178
  var self = this;                                                                                      // 1179
                                                                                                        // 1180
  // Tailable cursors only ever call added/addedBefore callbacks, so it's an                            // 1181
  // error if you didn't provide them.                                                                  // 1182
  if ((ordered && !callbacks.addedBefore) ||                                                            // 1183
      (!ordered && !callbacks.added)) {                                                                 // 1184
    throw new Error("Can't observe an " + (ordered ? "ordered" : "unordered")                           // 1185
                    + " tailable cursor without a "                                                     // 1186
                    + (ordered ? "addedBefore" : "added") + " callback");                               // 1187
  }                                                                                                     // 1188
                                                                                                        // 1189
  return self.tail(cursorDescription, function (doc) {                                                  // 1190
    var id = doc._id;                                                                                   // 1191
    delete doc._id;                                                                                     // 1192
    // The ts is an implementation detail. Hide it.                                                     // 1193
    delete doc.ts;                                                                                      // 1194
    if (ordered) {                                                                                      // 1195
      callbacks.addedBefore(id, doc, null);                                                             // 1196
    } else {                                                                                            // 1197
      callbacks.added(id, doc);                                                                         // 1198
    }                                                                                                   // 1199
  });                                                                                                   // 1200
};                                                                                                      // 1201
                                                                                                        // 1202
// XXX We probably need to find a better way to expose this. Right now                                  // 1203
// it's only used by tests, but in fact you need it in normal                                           // 1204
// operation to interact with capped collections (eg, Galaxy uses it).                                  // 1205
MongoInternals.MongoTimestamp = MongoDB.Timestamp;                                                      // 1206
                                                                                                        // 1207
MongoInternals.Connection = MongoConnection;                                                            // 1208
MongoInternals.NpmModule = MongoDB;                                                                     // 1209
                                                                                                        // 1210
//////////////////////////////////////////////////////////////////////////////////////////////////////////

}).call(this);






(function () {

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                                      //
// packages/mongo-livedata/oplog_tailing.js                                                             //
//                                                                                                      //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                                                        //
var Future = Npm.require('fibers/future');                                                              // 1
                                                                                                        // 2
OPLOG_COLLECTION = 'oplog.rs';                                                                          // 3
var REPLSET_COLLECTION = 'system.replset';                                                              // 4
                                                                                                        // 5
// Like Perl's quotemeta: quotes all regexp metacharacters. See                                         // 6
//   https://github.com/substack/quotemeta/blob/master/index.js                                         // 7
// XXX this is duplicated with accounts_server.js                                                       // 8
var quotemeta = function (str) {                                                                        // 9
    return String(str).replace(/(\W)/g, '\\$1');                                                        // 10
};                                                                                                      // 11
                                                                                                        // 12
var showTS = function (ts) {                                                                            // 13
  return "Timestamp(" + ts.getHighBits() + ", " + ts.getLowBits() + ")";                                // 14
};                                                                                                      // 15
                                                                                                        // 16
idForOp = function (op) {                                                                               // 17
  if (op.op === 'd')                                                                                    // 18
    return op.o._id;                                                                                    // 19
  else if (op.op === 'i')                                                                               // 20
    return op.o._id;                                                                                    // 21
  else if (op.op === 'u')                                                                               // 22
    return op.o2._id;                                                                                   // 23
  else if (op.op === 'c')                                                                               // 24
    throw Error("Operator 'c' doesn't supply an object with id: " +                                     // 25
                EJSON.stringify(op));                                                                   // 26
  else                                                                                                  // 27
    throw Error("Unknown op: " + EJSON.stringify(op));                                                  // 28
};                                                                                                      // 29
                                                                                                        // 30
OplogHandle = function (oplogUrl, dbName) {                                                             // 31
  var self = this;                                                                                      // 32
  self._oplogUrl = oplogUrl;                                                                            // 33
  self._dbName = dbName;                                                                                // 34
                                                                                                        // 35
  self._oplogLastEntryConnection = null;                                                                // 36
  self._oplogTailConnection = null;                                                                     // 37
  self._stopped = false;                                                                                // 38
  self._tailHandle = null;                                                                              // 39
  self._readyFuture = new Future();                                                                     // 40
  self._crossbar = new DDPServer._Crossbar({                                                            // 41
    factPackage: "mongo-livedata", factName: "oplog-watchers"                                           // 42
  });                                                                                                   // 43
  self._lastProcessedTS = null;                                                                         // 44
  self._baseOplogSelector = {                                                                           // 45
    ns: new RegExp('^' + quotemeta(self._dbName) + '\\.'),                                              // 46
    $or: [                                                                                              // 47
      { op: {$in: ['i', 'u', 'd']} },                                                                   // 48
      // If it is not db.collection.drop(), ignore it                                                   // 49
      { op: 'c', 'o.drop': { $exists: true } }]                                                         // 50
  };                                                                                                    // 51
  // XXX doc                                                                                            // 52
  self._catchingUpFutures = [];                                                                         // 53
                                                                                                        // 54
  self._startTailing();                                                                                 // 55
};                                                                                                      // 56
                                                                                                        // 57
_.extend(OplogHandle.prototype, {                                                                       // 58
  stop: function () {                                                                                   // 59
    var self = this;                                                                                    // 60
    if (self._stopped)                                                                                  // 61
      return;                                                                                           // 62
    self._stopped = true;                                                                               // 63
    if (self._tailHandle)                                                                               // 64
      self._tailHandle.stop();                                                                          // 65
    // XXX should close connections too                                                                 // 66
  },                                                                                                    // 67
  onOplogEntry: function (trigger, callback) {                                                          // 68
    var self = this;                                                                                    // 69
    if (self._stopped)                                                                                  // 70
      throw new Error("Called onOplogEntry on stopped handle!");                                        // 71
                                                                                                        // 72
    // Calling onOplogEntry requires us to wait for the tailing to be ready.                            // 73
    self._readyFuture.wait();                                                                           // 74
                                                                                                        // 75
    var originalCallback = callback;                                                                    // 76
    callback = Meteor.bindEnvironment(function (notification) {                                         // 77
      // XXX can we avoid this clone by making oplog.js careful?                                        // 78
      originalCallback(EJSON.clone(notification));                                                      // 79
    }, function (err) {                                                                                 // 80
      Meteor._debug("Error in oplog callback", err.stack);                                              // 81
    });                                                                                                 // 82
    var listenHandle = self._crossbar.listen(trigger, callback);                                        // 83
    return {                                                                                            // 84
      stop: function () {                                                                               // 85
        listenHandle.stop();                                                                            // 86
      }                                                                                                 // 87
    };                                                                                                  // 88
  },                                                                                                    // 89
  // Calls `callback` once the oplog has been processed up to a point that is                           // 90
  // roughly "now": specifically, once we've processed all ops that are                                 // 91
  // currently visible.                                                                                 // 92
  // XXX become convinced that this is actually safe even if oplogConnection                            // 93
  // is some kind of pool                                                                               // 94
  waitUntilCaughtUp: function () {                                                                      // 95
    var self = this;                                                                                    // 96
    if (self._stopped)                                                                                  // 97
      throw new Error("Called waitUntilCaughtUp on stopped handle!");                                   // 98
                                                                                                        // 99
    // Calling waitUntilCaughtUp requries us to wait for the oplog connection to                        // 100
    // be ready.                                                                                        // 101
    self._readyFuture.wait();                                                                           // 102
                                                                                                        // 103
    while (!self._stopped) {                                                                            // 104
      // We need to make the selector at least as restrictive as the actual                             // 105
      // tailing selector (ie, we need to specify the DB name) or else we might                         // 106
      // find a TS that won't show up in the actual tail stream.                                        // 107
      try {                                                                                             // 108
        var lastEntry = self._oplogLastEntryConnection.findOne(                                         // 109
          OPLOG_COLLECTION, self._baseOplogSelector,                                                    // 110
          {fields: {ts: 1}, sort: {$natural: -1}});                                                     // 111
        break;                                                                                          // 112
      } catch (e) {                                                                                     // 113
        // During failover (eg) if we get an exception we should log and retry                          // 114
        // instead of crashing.                                                                         // 115
        Meteor._debug("Got exception while reading last entry: " + e);                                  // 116
        Meteor._sleepForMs(100);                                                                        // 117
      }                                                                                                 // 118
    }                                                                                                   // 119
                                                                                                        // 120
    if (self._stopped)                                                                                  // 121
      return;                                                                                           // 122
                                                                                                        // 123
    if (!lastEntry) {                                                                                   // 124
      // Really, nothing in the oplog? Well, we've processed everything.                                // 125
      return;                                                                                           // 126
    }                                                                                                   // 127
                                                                                                        // 128
    var ts = lastEntry.ts;                                                                              // 129
    if (!ts)                                                                                            // 130
      throw Error("oplog entry without ts: " + EJSON.stringify(lastEntry));                             // 131
                                                                                                        // 132
    if (self._lastProcessedTS && ts.lessThanOrEqual(self._lastProcessedTS)) {                           // 133
      // We've already caught up to here.                                                               // 134
      return;                                                                                           // 135
    }                                                                                                   // 136
                                                                                                        // 137
                                                                                                        // 138
    // Insert the future into our list. Almost always, this will be at the end,                         // 139
    // but it's conceivable that if we fail over from one primary to another,                           // 140
    // the oplog entries we see will go backwards.                                                      // 141
    var insertAfter = self._catchingUpFutures.length;                                                   // 142
    while (insertAfter - 1 > 0                                                                          // 143
           && self._catchingUpFutures[insertAfter - 1].ts.greaterThan(ts)) {                            // 144
      insertAfter--;                                                                                    // 145
    }                                                                                                   // 146
    var f = new Future;                                                                                 // 147
    self._catchingUpFutures.splice(insertAfter, 0, {ts: ts, future: f});                                // 148
    f.wait();                                                                                           // 149
  },                                                                                                    // 150
  _startTailing: function () {                                                                          // 151
    var self = this;                                                                                    // 152
    // We make two separate connections to Mongo. The Node Mongo driver                                 // 153
    // implements a naive round-robin connection pool: each "connection" is a                           // 154
    // pool of several (5 by default) TCP connections, and each request is                              // 155
    // rotated through the pools. Tailable cursor queries block on the server                           // 156
    // until there is some data to return (or until a few seconds have                                  // 157
    // passed). So if the connection pool used for tailing cursors is the same                          // 158
    // pool used for other queries, the other queries will be delayed by seconds                        // 159
    // 1/5 of the time.                                                                                 // 160
    //                                                                                                  // 161
    // The tail connection will only ever be running a single tail command, so                          // 162
    // it only needs to make one underlying TCP connection.                                             // 163
    self._oplogTailConnection = new MongoConnection(                                                    // 164
      self._oplogUrl, {poolSize: 1});                                                                   // 165
    // XXX better docs, but: it's to get monotonic results                                              // 166
    // XXX is it safe to say "if there's an in flight query, just use its                               // 167
    //     results"? I don't think so but should consider that                                          // 168
    self._oplogLastEntryConnection = new MongoConnection(                                               // 169
      self._oplogUrl, {poolSize: 1});                                                                   // 170
                                                                                                        // 171
    // First, make sure that there actually is a repl set here. If not, oplog                           // 172
    // tailing won't ever find anything! (Blocks until the connection is ready.)                        // 173
    var replSetInfo = self._oplogLastEntryConnection.findOne(                                           // 174
      REPLSET_COLLECTION, {});                                                                          // 175
    if (!replSetInfo)                                                                                   // 176
      throw Error("$MONGO_OPLOG_URL must be set to the 'local' database of " +                          // 177
                  "a Mongo replica set");                                                               // 178
                                                                                                        // 179
    // Find the last oplog entry.                                                                       // 180
    var lastOplogEntry = self._oplogLastEntryConnection.findOne(                                        // 181
      OPLOG_COLLECTION, {}, {sort: {$natural: -1}, fields: {ts: 1}});                                   // 182
                                                                                                        // 183
    var oplogSelector = _.clone(self._baseOplogSelector);                                               // 184
    if (lastOplogEntry) {                                                                               // 185
      // Start after the last entry that currently exists.                                              // 186
      oplogSelector.ts = {$gt: lastOplogEntry.ts};                                                      // 187
      // If there are any calls to callWhenProcessedLatest before any other                             // 188
      // oplog entries show up, allow callWhenProcessedLatest to call its                               // 189
      // callback immediately.                                                                          // 190
      self._lastProcessedTS = lastOplogEntry.ts;                                                        // 191
    }                                                                                                   // 192
                                                                                                        // 193
    var cursorDescription = new CursorDescription(                                                      // 194
      OPLOG_COLLECTION, oplogSelector, {tailable: true});                                               // 195
                                                                                                        // 196
    self._tailHandle = self._oplogTailConnection.tail(                                                  // 197
      cursorDescription, function (doc) {                                                               // 198
        if (!(doc.ns && doc.ns.length > self._dbName.length + 1 &&                                      // 199
              doc.ns.substr(0, self._dbName.length + 1) ===                                             // 200
              (self._dbName + '.'))) {                                                                  // 201
          throw new Error("Unexpected ns");                                                             // 202
        }                                                                                               // 203
                                                                                                        // 204
        var trigger = {collection: doc.ns.substr(self._dbName.length + 1),                              // 205
                       dropCollection: false,                                                           // 206
                       op: doc};                                                                        // 207
                                                                                                        // 208
        // Is it a special command and the collection name is hidden somewhere                          // 209
        // in operator?                                                                                 // 210
        if (trigger.collection === "$cmd") {                                                            // 211
          trigger.collection = doc.o.drop;                                                              // 212
          trigger.dropCollection = true;                                                                // 213
          trigger.id = null;                                                                            // 214
        } else {                                                                                        // 215
          // All other ops have an id.                                                                  // 216
          trigger.id = idForOp(doc);                                                                    // 217
        }                                                                                               // 218
                                                                                                        // 219
        self._crossbar.fire(trigger);                                                                   // 220
                                                                                                        // 221
        // Now that we've processed this operation, process pending sequencers.                         // 222
        if (!doc.ts)                                                                                    // 223
          throw Error("oplog entry without ts: " + EJSON.stringify(doc));                               // 224
        self._lastProcessedTS = doc.ts;                                                                 // 225
        while (!_.isEmpty(self._catchingUpFutures)                                                      // 226
               && self._catchingUpFutures[0].ts.lessThanOrEqual(                                        // 227
                 self._lastProcessedTS)) {                                                              // 228
          var sequencer = self._catchingUpFutures.shift();                                              // 229
          sequencer.future.return();                                                                    // 230
        }                                                                                               // 231
      });                                                                                               // 232
    self._readyFuture.return();                                                                         // 233
  }                                                                                                     // 234
});                                                                                                     // 235
                                                                                                        // 236
//////////////////////////////////////////////////////////////////////////////////////////////////////////

}).call(this);






(function () {

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                                      //
// packages/mongo-livedata/observe_multiplex.js                                                         //
//                                                                                                      //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                                                        //
var Future = Npm.require('fibers/future');                                                              // 1
                                                                                                        // 2
ObserveMultiplexer = function (options) {                                                               // 3
  var self = this;                                                                                      // 4
                                                                                                        // 5
  if (!options || !_.has(options, 'ordered'))                                                           // 6
    throw Error("must specified ordered");                                                              // 7
                                                                                                        // 8
  Package.facts && Package.facts.Facts.incrementServerFact(                                             // 9
    "mongo-livedata", "observe-multiplexers", 1);                                                       // 10
                                                                                                        // 11
  self._ordered = options.ordered;                                                                      // 12
  self._onStop = options.onStop || function () {};                                                      // 13
  self._queue = new Meteor._SynchronousQueue();                                                         // 14
  self._handles = {};                                                                                   // 15
  self._readyFuture = new Future;                                                                       // 16
  self._cache = new LocalCollection._CachingChangeObserver({                                            // 17
    ordered: options.ordered});                                                                         // 18
  // Number of addHandleAndSendInitialAdds tasks scheduled but not yet                                  // 19
  // running. removeHandle uses this to know if it's time to call the onStop                            // 20
  // callback.                                                                                          // 21
  self._addHandleTasksScheduledButNotPerformed = 0;                                                     // 22
                                                                                                        // 23
  _.each(self.callbackNames(), function (callbackName) {                                                // 24
    self[callbackName] = function (/* ... */) {                                                         // 25
      self._applyCallback(callbackName, _.toArray(arguments));                                          // 26
    };                                                                                                  // 27
  });                                                                                                   // 28
};                                                                                                      // 29
                                                                                                        // 30
_.extend(ObserveMultiplexer.prototype, {                                                                // 31
  addHandleAndSendInitialAdds: function (handle) {                                                      // 32
    var self = this;                                                                                    // 33
                                                                                                        // 34
    // Check this before calling runTask (even though runTask does the same                             // 35
    // check) so that we don't leak an ObserveMultiplexer on error by                                   // 36
    // incrementing _addHandleTasksScheduledButNotPerformed and never                                   // 37
    // decrementing it.                                                                                 // 38
    if (!self._queue.safeToRunTask())                                                                   // 39
      throw new Error(                                                                                  // 40
        "Can't call observeChanges from an observe callback on the same query");                        // 41
    ++self._addHandleTasksScheduledButNotPerformed;                                                     // 42
                                                                                                        // 43
    Package.facts && Package.facts.Facts.incrementServerFact(                                           // 44
      "mongo-livedata", "observe-handles", 1);                                                          // 45
                                                                                                        // 46
    self._queue.runTask(function () {                                                                   // 47
      self._handles[handle._id] = handle;                                                               // 48
      // Send out whatever adds we have so far (whether or not we the                                   // 49
      // multiplexer is ready).                                                                         // 50
      self._sendAdds(handle);                                                                           // 51
      --self._addHandleTasksScheduledButNotPerformed;                                                   // 52
    });                                                                                                 // 53
    // *outside* the task, since otherwise we'd deadlock                                                // 54
    self._readyFuture.wait();                                                                           // 55
  },                                                                                                    // 56
                                                                                                        // 57
  // Remove an observe handle. If it was the last observe handle, call the                              // 58
  // onStop callback; you cannot add any more observe handles after this.                               // 59
  //                                                                                                    // 60
  // This is not synchronized with polls and handle additions: this means that                          // 61
  // you can safely call it from within an observe callback, but it also means                          // 62
  // that we have to be careful when we iterate over _handles.                                          // 63
  removeHandle: function (id) {                                                                         // 64
    var self = this;                                                                                    // 65
                                                                                                        // 66
    // This should not be possible: you can only call removeHandle by having                            // 67
    // access to the ObserveHandle, which isn't returned to user code until the                         // 68
    // multiplex is ready.                                                                              // 69
    if (!self._ready())                                                                                 // 70
      throw new Error("Can't remove handles until the multiplex is ready");                             // 71
                                                                                                        // 72
    delete self._handles[id];                                                                           // 73
                                                                                                        // 74
    Package.facts && Package.facts.Facts.incrementServerFact(                                           // 75
      "mongo-livedata", "observe-handles", -1);                                                         // 76
                                                                                                        // 77
    if (_.isEmpty(self._handles) &&                                                                     // 78
        self._addHandleTasksScheduledButNotPerformed === 0) {                                           // 79
      self._stop();                                                                                     // 80
    }                                                                                                   // 81
  },                                                                                                    // 82
  _stop: function () {                                                                                  // 83
    var self = this;                                                                                    // 84
    // It shouldn't be possible for us to stop when all our handles still                               // 85
    // haven't been returned from observeChanges!                                                       // 86
    if (!self._ready())                                                                                 // 87
      throw Error("surprising _stop: not ready");                                                       // 88
                                                                                                        // 89
    // Call stop callback (which kills the underlying process which sends us                            // 90
    // callbacks and removes us from the connection's dictionary).                                      // 91
    self._onStop();                                                                                     // 92
    Package.facts && Package.facts.Facts.incrementServerFact(                                           // 93
      "mongo-livedata", "observe-multiplexers", -1);                                                    // 94
                                                                                                        // 95
    // Cause future addHandleAndSendInitialAdds calls to throw (but the onStop                          // 96
    // callback should make our connection forget about us).                                            // 97
    self._handles = null;                                                                               // 98
  },                                                                                                    // 99
  // Allows all addHandleAndSendInitialAdds calls to return, once all preceding                         // 100
  // adds have been processed. Does not block.                                                          // 101
  ready: function () {                                                                                  // 102
    var self = this;                                                                                    // 103
    self._queue.queueTask(function () {                                                                 // 104
      if (self._ready())                                                                                // 105
        throw Error("can't make ObserveMultiplex ready twice!");                                        // 106
      self._readyFuture.return();                                                                       // 107
    });                                                                                                 // 108
  },                                                                                                    // 109
  // Calls "cb" once the effects of all "ready", "addHandleAndSendInitialAdds"                          // 110
  // and observe callbacks which came before this call have been propagated to                          // 111
  // all handles. "ready" must have already been called on this multiplexer.                            // 112
  onFlush: function (cb) {                                                                              // 113
    var self = this;                                                                                    // 114
    self._queue.queueTask(function () {                                                                 // 115
      if (!self._ready())                                                                               // 116
        throw Error("only call onFlush on a multiplexer that will be ready");                           // 117
      cb();                                                                                             // 118
    });                                                                                                 // 119
  },                                                                                                    // 120
  callbackNames: function () {                                                                          // 121
    var self = this;                                                                                    // 122
    if (self._ordered)                                                                                  // 123
      return ["addedBefore", "changed", "movedBefore", "removed"];                                      // 124
    else                                                                                                // 125
      return ["added", "changed", "removed"];                                                           // 126
  },                                                                                                    // 127
  _ready: function () {                                                                                 // 128
    return this._readyFuture.isResolved();                                                              // 129
  },                                                                                                    // 130
  _applyCallback: function (callbackName, args) {                                                       // 131
    var self = this;                                                                                    // 132
    self._queue.queueTask(function () {                                                                 // 133
      // If we stopped in the meantime, do nothing.                                                     // 134
      if (!self._handles)                                                                               // 135
        return;                                                                                         // 136
                                                                                                        // 137
      // First, apply the change to the cache.                                                          // 138
      // XXX We could make applyChange callbacks promise not to hang on to any                          // 139
      // state from their arguments (assuming that their supplied callbacks                             // 140
      // don't) and skip this clone. Currently 'changed' hangs on to state                              // 141
      // though.                                                                                        // 142
      self._cache.applyChange[callbackName].apply(null, EJSON.clone(args));                             // 143
                                                                                                        // 144
      // If we haven't finished the initial adds, then we should only be getting                        // 145
      // adds.                                                                                          // 146
      if (!self._ready() &&                                                                             // 147
          (callbackName !== 'added' && callbackName !== 'addedBefore')) {                               // 148
        throw new Error("Got " + callbackName + " during initial adds");                                // 149
      }                                                                                                 // 150
                                                                                                        // 151
      // Now multiplex the callbacks out to all observe handles. It's OK if                             // 152
      // these calls yield; since we're inside a task, no other use of our queue                        // 153
      // can continue until these are done. (But we do have to be careful to not                        // 154
      // use a handle that got removed, because removeHandle does not use the                           // 155
      // queue; thus, we iterate over an array of keys that we control.)                                // 156
      _.each(_.keys(self._handles), function (handleId) {                                               // 157
        var handle = self._handles && self._handles[handleId];                                          // 158
        if (!handle)                                                                                    // 159
          return;                                                                                       // 160
        var callback = handle['_' + callbackName];                                                      // 161
        // clone arguments so that callbacks can mutate their arguments                                 // 162
        callback && callback.apply(null, EJSON.clone(args));                                            // 163
      });                                                                                               // 164
    });                                                                                                 // 165
  },                                                                                                    // 166
                                                                                                        // 167
  // Sends initial adds to a handle. It should only be called from within a task                        // 168
  // (the task that is processing the addHandleAndSendInitialAdds call). It                             // 169
  // synchronously invokes the handle's added or addedBefore; there's no need to                        // 170
  // flush the queue afterwards to ensure that the callbacks get out.                                   // 171
  _sendAdds: function (handle) {                                                                        // 172
    var self = this;                                                                                    // 173
    if (self._queue.safeToRunTask())                                                                    // 174
      throw Error("_sendAdds may only be called from within a task!");                                  // 175
    var add = self._ordered ? handle._addedBefore : handle._added;                                      // 176
    if (!add)                                                                                           // 177
      return;                                                                                           // 178
    // note: docs may be an _IdMap or an OrderedDict                                                    // 179
    self._cache.docs.forEach(function (doc, id) {                                                       // 180
      if (!_.has(self._handles, handle._id))                                                            // 181
        throw Error("handle got removed before sending initial adds!");                                 // 182
      var fields = EJSON.clone(doc);                                                                    // 183
      delete fields._id;                                                                                // 184
      if (self._ordered)                                                                                // 185
        add(id, fields, null); // we're going in order, so add at end                                   // 186
      else                                                                                              // 187
        add(id, fields);                                                                                // 188
    });                                                                                                 // 189
  }                                                                                                     // 190
});                                                                                                     // 191
                                                                                                        // 192
                                                                                                        // 193
var nextObserveHandleId = 1;                                                                            // 194
ObserveHandle = function (multiplexer, callbacks) {                                                     // 195
  var self = this;                                                                                      // 196
  // The end user is only supposed to call stop().  The other fields are                                // 197
  // accessible to the multiplexer, though.                                                             // 198
  self._multiplexer = multiplexer;                                                                      // 199
  _.each(multiplexer.callbackNames(), function (name) {                                                 // 200
    if (callbacks[name]) {                                                                              // 201
      self['_' + name] = callbacks[name];                                                               // 202
    } else if (name === "addedBefore" && callbacks.added) {                                             // 203
      // Special case: if you specify "added" and "movedBefore", you get an                             // 204
      // ordered observe where for some reason you don't get ordering data on                           // 205
      // the adds.  I dunno, we wrote tests for it, there must have been a                              // 206
      // reason.                                                                                        // 207
      self._addedBefore = function (id, fields, before) {                                               // 208
        callbacks.added(id, fields);                                                                    // 209
      };                                                                                                // 210
    }                                                                                                   // 211
  });                                                                                                   // 212
  self._stopped = false;                                                                                // 213
  self._id = nextObserveHandleId++;                                                                     // 214
};                                                                                                      // 215
ObserveHandle.prototype.stop = function () {                                                            // 216
  var self = this;                                                                                      // 217
  if (self._stopped)                                                                                    // 218
    return;                                                                                             // 219
  self._stopped = true;                                                                                 // 220
  self._multiplexer.removeHandle(self._id);                                                             // 221
};                                                                                                      // 222
                                                                                                        // 223
//////////////////////////////////////////////////////////////////////////////////////////////////////////

}).call(this);






(function () {

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                                      //
// packages/mongo-livedata/doc_fetcher.js                                                               //
//                                                                                                      //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                                                        //
var Fiber = Npm.require('fibers');                                                                      // 1
var Future = Npm.require('fibers/future');                                                              // 2
                                                                                                        // 3
DocFetcher = function (mongoConnection) {                                                               // 4
  var self = this;                                                                                      // 5
  self._mongoConnection = mongoConnection;                                                              // 6
  // Map from cache key -> [callback]                                                                   // 7
  self._callbacksForCacheKey = {};                                                                      // 8
};                                                                                                      // 9
                                                                                                        // 10
_.extend(DocFetcher.prototype, {                                                                        // 11
  // Fetches document "id" from collectionName, returning it or null if not                             // 12
  // found.                                                                                             // 13
  //                                                                                                    // 14
  // If you make multiple calls to fetch() with the same cacheKey (a string),                           // 15
  // DocFetcher may assume that they all return the same document. (It does                             // 16
  // not check to see if collectionName/id match.)                                                      // 17
  //                                                                                                    // 18
  // You may assume that callback is never called synchronously (and in fact                            // 19
  // OplogObserveDriver does so).                                                                       // 20
  fetch: function (collectionName, id, cacheKey, callback) {                                            // 21
    var self = this;                                                                                    // 22
                                                                                                        // 23
    check(collectionName, String);                                                                      // 24
    // id is some sort of scalar                                                                        // 25
    check(cacheKey, String);                                                                            // 26
                                                                                                        // 27
    // If there's already an in-progress fetch for this cache key, yield until                          // 28
    // it's done and return whatever it returns.                                                        // 29
    if (_.has(self._callbacksForCacheKey, cacheKey)) {                                                  // 30
      self._callbacksForCacheKey[cacheKey].push(callback);                                              // 31
      return;                                                                                           // 32
    }                                                                                                   // 33
                                                                                                        // 34
    var callbacks = self._callbacksForCacheKey[cacheKey] = [callback];                                  // 35
                                                                                                        // 36
    Fiber(function () {                                                                                 // 37
      try {                                                                                             // 38
        var doc = self._mongoConnection.findOne(                                                        // 39
          collectionName, {_id: id}) || null;                                                           // 40
        // Return doc to all relevant callbacks. Note that this array can                               // 41
        // continue to grow during callback excecution.                                                 // 42
        while (!_.isEmpty(callbacks)) {                                                                 // 43
          // Clone the document so that the various calls to fetch don't return                         // 44
          // objects that are intertwingled with each other. Clone before                               // 45
          // popping the future, so that if clone throws, the error gets passed                         // 46
          // to the next callback.                                                                      // 47
          var clonedDoc = EJSON.clone(doc);                                                             // 48
          callbacks.pop()(null, clonedDoc);                                                             // 49
        }                                                                                               // 50
      } catch (e) {                                                                                     // 51
        while (!_.isEmpty(callbacks)) {                                                                 // 52
          callbacks.pop()(e);                                                                           // 53
        }                                                                                               // 54
      } finally {                                                                                       // 55
        // XXX consider keeping the doc around for a period of time before                              // 56
        // removing from the cache                                                                      // 57
        delete self._callbacksForCacheKey[cacheKey];                                                    // 58
      }                                                                                                 // 59
    }).run();                                                                                           // 60
  }                                                                                                     // 61
});                                                                                                     // 62
                                                                                                        // 63
MongoTest.DocFetcher = DocFetcher;                                                                      // 64
                                                                                                        // 65
//////////////////////////////////////////////////////////////////////////////////////////////////////////

}).call(this);






(function () {

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                                      //
// packages/mongo-livedata/polling_observe_driver.js                                                    //
//                                                                                                      //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                                                        //
PollingObserveDriver = function (options) {                                                             // 1
  var self = this;                                                                                      // 2
                                                                                                        // 3
  self._cursorDescription = options.cursorDescription;                                                  // 4
  self._mongoHandle = options.mongoHandle;                                                              // 5
  self._ordered = options.ordered;                                                                      // 6
  self._multiplexer = options.multiplexer;                                                              // 7
  self._stopCallbacks = [];                                                                             // 8
  self._stopped = false;                                                                                // 9
                                                                                                        // 10
  self._synchronousCursor = self._mongoHandle._createSynchronousCursor(                                 // 11
    self._cursorDescription);                                                                           // 12
                                                                                                        // 13
  // previous results snapshot.  on each poll cycle, diffs against                                      // 14
  // results drives the callbacks.                                                                      // 15
  self._results = null;                                                                                 // 16
                                                                                                        // 17
  // The number of _pollMongo calls that have been added to self._taskQueue but                         // 18
  // have not started running. Used to make sure we never schedule more than one                        // 19
  // _pollMongo (other than possibly the one that is currently running). It's                           // 20
  // also used by _suspendPolling to pretend there's a poll scheduled. Usually,                         // 21
  // it's either 0 (for "no polls scheduled other than maybe one currently                              // 22
  // running") or 1 (for "a poll scheduled that isn't running yet"), but it can                         // 23
  // also be 2 if incremented by _suspendPolling.                                                       // 24
  self._pollsScheduledButNotStarted = 0;                                                                // 25
  self._pendingWrites = []; // people to notify when polling completes                                  // 26
                                                                                                        // 27
  // Make sure to create a separately throttled function for each                                       // 28
  // PollingObserveDriver object.                                                                       // 29
  self._ensurePollIsScheduled = _.throttle(                                                             // 30
    self._unthrottledEnsurePollIsScheduled, 50 /* ms */);                                               // 31
                                                                                                        // 32
  // XXX figure out if we still need a queue                                                            // 33
  self._taskQueue = new Meteor._SynchronousQueue();                                                     // 34
                                                                                                        // 35
  var listenersHandle = listenAll(                                                                      // 36
    self._cursorDescription, function (notification) {                                                  // 37
      // When someone does a transaction that might affect us, schedule a poll                          // 38
      // of the database. If that transaction happens inside of a write fence,                          // 39
      // block the fence until we've polled and notified observers.                                     // 40
      var fence = DDPServer._CurrentWriteFence.get();                                                   // 41
      if (fence)                                                                                        // 42
        self._pendingWrites.push(fence.beginWrite());                                                   // 43
      // Ensure a poll is scheduled... but if we already know that one is,                              // 44
      // don't hit the throttled _ensurePollIsScheduled function (which might                           // 45
      // lead to us calling it unnecessarily in 50ms).                                                  // 46
      if (self._pollsScheduledButNotStarted === 0)                                                      // 47
        self._ensurePollIsScheduled();                                                                  // 48
    }                                                                                                   // 49
  );                                                                                                    // 50
  self._stopCallbacks.push(function () { listenersHandle.stop(); });                                    // 51
                                                                                                        // 52
  // every once and a while, poll even if we don't think we're dirty, for                               // 53
  // eventual consistency with database writes from outside the Meteor                                  // 54
  // universe.                                                                                          // 55
  //                                                                                                    // 56
  // For testing, there's an undocumented callback argument to observeChanges                           // 57
  // which disables time-based polling and gets called at the beginning of each                         // 58
  // poll.                                                                                              // 59
  if (options._testOnlyPollCallback) {                                                                  // 60
    self._testOnlyPollCallback = options._testOnlyPollCallback;                                         // 61
  } else {                                                                                              // 62
    var intervalHandle = Meteor.setInterval(                                                            // 63
      _.bind(self._ensurePollIsScheduled, self), 10 * 1000);                                            // 64
    self._stopCallbacks.push(function () {                                                              // 65
      Meteor.clearInterval(intervalHandle);                                                             // 66
    });                                                                                                 // 67
  }                                                                                                     // 68
                                                                                                        // 69
  // Make sure we actually poll soon!                                                                   // 70
  self._unthrottledEnsurePollIsScheduled();                                                             // 71
                                                                                                        // 72
  Package.facts && Package.facts.Facts.incrementServerFact(                                             // 73
    "mongo-livedata", "observe-drivers-polling", 1);                                                    // 74
};                                                                                                      // 75
                                                                                                        // 76
_.extend(PollingObserveDriver.prototype, {                                                              // 77
  // This is always called through _.throttle (except once at startup).                                 // 78
  _unthrottledEnsurePollIsScheduled: function () {                                                      // 79
    var self = this;                                                                                    // 80
    if (self._pollsScheduledButNotStarted > 0)                                                          // 81
      return;                                                                                           // 82
    ++self._pollsScheduledButNotStarted;                                                                // 83
    self._taskQueue.queueTask(function () {                                                             // 84
      self._pollMongo();                                                                                // 85
    });                                                                                                 // 86
  },                                                                                                    // 87
                                                                                                        // 88
  // test-only interface for controlling polling.                                                       // 89
  //                                                                                                    // 90
  // _suspendPolling blocks until any currently running and scheduled polls are                         // 91
  // done, and prevents any further polls from being scheduled. (new                                    // 92
  // ObserveHandles can be added and receive their initial added callbacks,                             // 93
  // though.)                                                                                           // 94
  //                                                                                                    // 95
  // _resumePolling immediately polls, and allows further polls to occur.                               // 96
  _suspendPolling: function() {                                                                         // 97
    var self = this;                                                                                    // 98
    // Pretend that there's another poll scheduled (which will prevent                                  // 99
    // _ensurePollIsScheduled from queueing any more polls).                                            // 100
    ++self._pollsScheduledButNotStarted;                                                                // 101
    // Now block until all currently running or scheduled polls are done.                               // 102
    self._taskQueue.runTask(function() {});                                                             // 103
                                                                                                        // 104
    // Confirm that there is only one "poll" (the fake one we're pretending to                          // 105
    // have) scheduled.                                                                                 // 106
    if (self._pollsScheduledButNotStarted !== 1)                                                        // 107
      throw new Error("_pollsScheduledButNotStarted is " +                                              // 108
                      self._pollsScheduledButNotStarted);                                               // 109
  },                                                                                                    // 110
  _resumePolling: function() {                                                                          // 111
    var self = this;                                                                                    // 112
    // We should be in the same state as in the end of _suspendPolling.                                 // 113
    if (self._pollsScheduledButNotStarted !== 1)                                                        // 114
      throw new Error("_pollsScheduledButNotStarted is " +                                              // 115
                      self._pollsScheduledButNotStarted);                                               // 116
    // Run a poll synchronously (which will counteract the                                              // 117
    // ++_pollsScheduledButNotStarted from _suspendPolling).                                            // 118
    self._taskQueue.runTask(function () {                                                               // 119
      self._pollMongo();                                                                                // 120
    });                                                                                                 // 121
  },                                                                                                    // 122
                                                                                                        // 123
  _pollMongo: function () {                                                                             // 124
    var self = this;                                                                                    // 125
    --self._pollsScheduledButNotStarted;                                                                // 126
                                                                                                        // 127
    var first = false;                                                                                  // 128
    var oldResults = self._results;                                                                     // 129
    if (!oldResults) {                                                                                  // 130
      first = true;                                                                                     // 131
      // XXX maybe use OrderedDict instead?                                                             // 132
      oldResults = self._ordered ? [] : new LocalCollection._IdMap;                                     // 133
    }                                                                                                   // 134
                                                                                                        // 135
    self._testOnlyPollCallback && self._testOnlyPollCallback();                                         // 136
                                                                                                        // 137
    // Save the list of pending writes which this round will commit.                                    // 138
    var writesForCycle = self._pendingWrites;                                                           // 139
    self._pendingWrites = [];                                                                           // 140
                                                                                                        // 141
    // Get the new query results. (This yields.)                                                        // 142
    try {                                                                                               // 143
      var newResults = self._synchronousCursor.getRawObjects(self._ordered);                            // 144
    } catch (e) {                                                                                       // 145
      // getRawObjects can throw if we're having trouble talking to the                                 // 146
      // database.  That's fine --- we will repoll later anyway. But we should                          // 147
      // make sure not to lose track of this cycle's writes.                                            // 148
      Array.prototype.push.apply(self._pendingWrites, writesForCycle);                                  // 149
      throw e;                                                                                          // 150
    }                                                                                                   // 151
                                                                                                        // 152
    // Run diffs.                                                                                       // 153
    if (!self._stopped) {                                                                               // 154
      LocalCollection._diffQueryChanges(                                                                // 155
        self._ordered, oldResults, newResults, self._multiplexer);                                      // 156
    }                                                                                                   // 157
                                                                                                        // 158
    // Signals the multiplexer to allow all observeChanges calls that share this                        // 159
    // multiplexer to return. (This happens asynchronously, via the                                     // 160
    // multiplexer's queue.)                                                                            // 161
    if (first)                                                                                          // 162
      self._multiplexer.ready();                                                                        // 163
                                                                                                        // 164
    // Replace self._results atomically.  (This assignment is what makes `first`                        // 165
    // stay through on the next cycle, so we've waited until after we've                                // 166
    // committed to ready-ing the multiplexer.)                                                         // 167
    self._results = newResults;                                                                         // 168
                                                                                                        // 169
    // Once the ObserveMultiplexer has processed everything we've done in this                          // 170
    // round, mark all the writes which existed before this call as                                     // 171
    // commmitted. (If new writes have shown up in the meantime, there'll                               // 172
    // already be another _pollMongo task scheduled.)                                                   // 173
    self._multiplexer.onFlush(function () {                                                             // 174
      _.each(writesForCycle, function (w) {                                                             // 175
        w.committed();                                                                                  // 176
      });                                                                                               // 177
    });                                                                                                 // 178
  },                                                                                                    // 179
                                                                                                        // 180
  stop: function () {                                                                                   // 181
    var self = this;                                                                                    // 182
    self._stopped = true;                                                                               // 183
    _.each(self._stopCallbacks, function (c) { c(); });                                                 // 184
    Package.facts && Package.facts.Facts.incrementServerFact(                                           // 185
      "mongo-livedata", "observe-drivers-polling", -1);                                                 // 186
  }                                                                                                     // 187
});                                                                                                     // 188
                                                                                                        // 189
//////////////////////////////////////////////////////////////////////////////////////////////////////////

}).call(this);






(function () {

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                                      //
// packages/mongo-livedata/oplog_observe_driver.js                                                      //
//                                                                                                      //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                                                        //
var Fiber = Npm.require('fibers');                                                                      // 1
var Future = Npm.require('fibers/future');                                                              // 2
                                                                                                        // 3
var PHASE = {                                                                                           // 4
  QUERYING: "QUERYING",                                                                                 // 5
  FETCHING: "FETCHING",                                                                                 // 6
  STEADY: "STEADY"                                                                                      // 7
};                                                                                                      // 8
                                                                                                        // 9
// Exception thrown by _needToPollQuery which unrolls the stack up to the                               // 10
// enclosing call to finishIfNeedToPollQuery.                                                           // 11
var SwitchedToQuery = function () {};                                                                   // 12
var finishIfNeedToPollQuery = function (f) {                                                            // 13
  return function () {                                                                                  // 14
    try {                                                                                               // 15
      f.apply(this, arguments);                                                                         // 16
    } catch (e) {                                                                                       // 17
      if (!(e instanceof SwitchedToQuery))                                                              // 18
        throw e;                                                                                        // 19
    }                                                                                                   // 20
  };                                                                                                    // 21
};                                                                                                      // 22
                                                                                                        // 23
// OplogObserveDriver is an alternative to PollingObserveDriver which follows                           // 24
// the Mongo operation log instead of just re-polling the query. It obeys the                           // 25
// same simple interface: constructing it starts sending observeChanges                                 // 26
// callbacks (and a ready() invocation) to the ObserveMultiplexer, and you stop                         // 27
// it by calling the stop() method.                                                                     // 28
OplogObserveDriver = function (options) {                                                               // 29
  var self = this;                                                                                      // 30
  self._usesOplog = true;  // tests look at this                                                        // 31
                                                                                                        // 32
  self._cursorDescription = options.cursorDescription;                                                  // 33
  self._mongoHandle = options.mongoHandle;                                                              // 34
  self._multiplexer = options.multiplexer;                                                              // 35
                                                                                                        // 36
  if (options.ordered) {                                                                                // 37
    throw Error("OplogObserveDriver only supports unordered observeChanges");                           // 38
  }                                                                                                     // 39
                                                                                                        // 40
  var sorter = options.sorter;                                                                          // 41
  // We don't support $near and other geo-queries so it's OK to initialize the                          // 42
  // comparator only once in the constructor.                                                           // 43
  var comparator = sorter && sorter.getComparator();                                                    // 44
                                                                                                        // 45
  if (options.cursorDescription.options.limit) {                                                        // 46
    // There are several properties ordered driver implements:                                          // 47
    // - _limit is a positive number                                                                    // 48
    // - _comparator is a function-comparator by which the query is ordered                             // 49
    // - _unpublishedBuffer is non-null Min/Max Heap,                                                   // 50
    //                      the empty buffer in STEADY phase implies that the                           // 51
    //                      everything that matches the queries selector fits                           // 52
    //                      into published set.                                                         // 53
    // - _published - Min Heap (also implements IdMap methods)                                          // 54
                                                                                                        // 55
    var heapOptions = { IdMap: LocalCollection._IdMap };                                                // 56
    self._limit = self._cursorDescription.options.limit;                                                // 57
    self._comparator = comparator;                                                                      // 58
    self._sorter = sorter;                                                                              // 59
    self._unpublishedBuffer = new MinMaxHeap(comparator, heapOptions);                                  // 60
    // We need something that can find Max value in addition to IdMap interface                         // 61
    self._published = new MaxHeap(comparator, heapOptions);                                             // 62
  } else {                                                                                              // 63
    self._limit = 0;                                                                                    // 64
    self._comparator = null;                                                                            // 65
    self._sorter = null;                                                                                // 66
    self._unpublishedBuffer = null;                                                                     // 67
    self._published = new LocalCollection._IdMap;                                                       // 68
  }                                                                                                     // 69
                                                                                                        // 70
  // Indicates if it is safe to insert a new document at the end of the buffer                          // 71
  // for this query. i.e. it is known that there are no documents matching the                          // 72
  // selector those are not in published or buffer.                                                     // 73
  self._safeAppendToBuffer = false;                                                                     // 74
                                                                                                        // 75
  self._stopped = false;                                                                                // 76
  self._stopHandles = [];                                                                               // 77
                                                                                                        // 78
  Package.facts && Package.facts.Facts.incrementServerFact(                                             // 79
    "mongo-livedata", "observe-drivers-oplog", 1);                                                      // 80
                                                                                                        // 81
  self._registerPhaseChange(PHASE.QUERYING);                                                            // 82
                                                                                                        // 83
  var selector = self._cursorDescription.selector;                                                      // 84
  self._matcher = options.matcher;                                                                      // 85
  var projection = self._cursorDescription.options.fields || {};                                        // 86
  self._projectionFn = LocalCollection._compileProjection(projection);                                  // 87
  // Projection function, result of combining important fields for selector and                         // 88
  // existing fields projection                                                                         // 89
  self._sharedProjection = self._matcher.combineIntoProjection(projection);                             // 90
  if (sorter)                                                                                           // 91
    self._sharedProjection = sorter.combineIntoProjection(self._sharedProjection);                      // 92
  self._sharedProjectionFn = LocalCollection._compileProjection(                                        // 93
    self._sharedProjection);                                                                            // 94
                                                                                                        // 95
  self._needToFetch = new LocalCollection._IdMap;                                                       // 96
  self._currentlyFetching = null;                                                                       // 97
  self._fetchGeneration = 0;                                                                            // 98
                                                                                                        // 99
  self._requeryWhenDoneThisQuery = false;                                                               // 100
  self._writesToCommitWhenWeReachSteady = [];                                                           // 101
                                                                                                        // 102
  forEachTrigger(self._cursorDescription, function (trigger) {                                          // 103
    self._stopHandles.push(self._mongoHandle._oplogHandle.onOplogEntry(                                 // 104
      trigger, function (notification) {                                                                // 105
        Meteor._noYieldsAllowed(finishIfNeedToPollQuery(function () {                                   // 106
          var op = notification.op;                                                                     // 107
          if (notification.dropCollection) {                                                            // 108
            // Note: this call is not allowed to block on anything (especially                          // 109
            // on waiting for oplog entries to catch up) because that will block                        // 110
            // onOplogEntry!                                                                            // 111
            self._needToPollQuery();                                                                    // 112
          } else {                                                                                      // 113
            // All other operators should be handled depending on phase                                 // 114
            if (self._phase === PHASE.QUERYING)                                                         // 115
              self._handleOplogEntryQuerying(op);                                                       // 116
            else                                                                                        // 117
              self._handleOplogEntrySteadyOrFetching(op);                                               // 118
          }                                                                                             // 119
        }));                                                                                            // 120
      }                                                                                                 // 121
    ));                                                                                                 // 122
  });                                                                                                   // 123
                                                                                                        // 124
  // XXX ordering w.r.t. everything else?                                                               // 125
  self._stopHandles.push(listenAll(                                                                     // 126
    self._cursorDescription, function (notification) {                                                  // 127
      // If we're not in a write fence, we don't have to do anything.                                   // 128
      var fence = DDPServer._CurrentWriteFence.get();                                                   // 129
      if (!fence)                                                                                       // 130
        return;                                                                                         // 131
      var write = fence.beginWrite();                                                                   // 132
      // This write cannot complete until we've caught up to "this point" in the                        // 133
      // oplog, and then made it back to the steady state.                                              // 134
      Meteor.defer(function () {                                                                        // 135
        self._mongoHandle._oplogHandle.waitUntilCaughtUp();                                             // 136
        if (self._stopped) {                                                                            // 137
          // We're stopped, so just immediately commit.                                                 // 138
          write.committed();                                                                            // 139
        } else if (self._phase === PHASE.STEADY) {                                                      // 140
          // Make sure that all of the callbacks have made it through the                               // 141
          // multiplexer and been delivered to ObserveHandles before committing                         // 142
          // writes.                                                                                    // 143
          self._multiplexer.onFlush(function () {                                                       // 144
            write.committed();                                                                          // 145
          });                                                                                           // 146
        } else {                                                                                        // 147
          self._writesToCommitWhenWeReachSteady.push(write);                                            // 148
        }                                                                                               // 149
      });                                                                                               // 150
    }                                                                                                   // 151
  ));                                                                                                   // 152
                                                                                                        // 153
  // When Mongo fails over, we need to repoll the query, in case we processed an                        // 154
  // oplog entry that got rolled back.                                                                  // 155
  self._stopHandles.push(self._mongoHandle._onFailover(finishIfNeedToPollQuery(                         // 156
    function () {                                                                                       // 157
      self._needToPollQuery();                                                                          // 158
    })));                                                                                               // 159
                                                                                                        // 160
  // Give _observeChanges a chance to add the new ObserveHandle to our                                  // 161
  // multiplexer, so that the added calls get streamed.                                                 // 162
  Meteor.defer(finishIfNeedToPollQuery(function () {                                                    // 163
    self._runInitialQuery();                                                                            // 164
  }));                                                                                                  // 165
};                                                                                                      // 166
                                                                                                        // 167
_.extend(OplogObserveDriver.prototype, {                                                                // 168
  _addPublished: function (id, doc) {                                                                   // 169
    var self = this;                                                                                    // 170
    Meteor._noYieldsAllowed(function () {                                                               // 171
      var fields = _.clone(doc);                                                                        // 172
      delete fields._id;                                                                                // 173
      self._published.set(id, self._sharedProjectionFn(doc));                                           // 174
      self._multiplexer.added(id, self._projectionFn(fields));                                          // 175
                                                                                                        // 176
      // After adding this document, the published set might be overflowed                              // 177
      // (exceeding capacity specified by limit). If so, push the maximum                               // 178
      // element to the buffer, we might want to save it in memory to reduce the                        // 179
      // amount of Mongo lookups in the future.                                                         // 180
      if (self._limit && self._published.size() > self._limit) {                                        // 181
        // XXX in theory the size of published is no more than limit+1                                  // 182
        if (self._published.size() !== self._limit + 1) {                                               // 183
          throw new Error("After adding to published, " +                                               // 184
                          (self._published.size() - self._limit) +                                      // 185
                          " documents are overflowing the set");                                        // 186
        }                                                                                               // 187
                                                                                                        // 188
        var overflowingDocId = self._published.maxElementId();                                          // 189
        var overflowingDoc = self._published.get(overflowingDocId);                                     // 190
                                                                                                        // 191
        if (EJSON.equals(overflowingDocId, id)) {                                                       // 192
          throw new Error("The document just added is overflowing the published set");                  // 193
        }                                                                                               // 194
                                                                                                        // 195
        self._published.remove(overflowingDocId);                                                       // 196
        self._multiplexer.removed(overflowingDocId);                                                    // 197
        self._addBuffered(overflowingDocId, overflowingDoc);                                            // 198
      }                                                                                                 // 199
    });                                                                                                 // 200
  },                                                                                                    // 201
  _removePublished: function (id) {                                                                     // 202
    var self = this;                                                                                    // 203
    Meteor._noYieldsAllowed(function () {                                                               // 204
      self._published.remove(id);                                                                       // 205
      self._multiplexer.removed(id);                                                                    // 206
      if (! self._limit || self._published.size() === self._limit)                                      // 207
        return;                                                                                         // 208
                                                                                                        // 209
      if (self._published.size() > self._limit)                                                         // 210
        throw Error("self._published got too big");                                                     // 211
                                                                                                        // 212
      // OK, we are publishing less than the limit. Maybe we should look in the                         // 213
      // buffer to find the next element past what we were publishing before.                           // 214
                                                                                                        // 215
      if (!self._unpublishedBuffer.empty()) {                                                           // 216
        // There's something in the buffer; move the first thing in it to                               // 217
        // _published.                                                                                  // 218
        var newDocId = self._unpublishedBuffer.minElementId();                                          // 219
        var newDoc = self._unpublishedBuffer.get(newDocId);                                             // 220
        self._removeBuffered(newDocId);                                                                 // 221
        self._addPublished(newDocId, newDoc);                                                           // 222
        return;                                                                                         // 223
      }                                                                                                 // 224
                                                                                                        // 225
      // There's nothing in the buffer.  This could mean one of a few things.                           // 226
                                                                                                        // 227
      // (a) We could be in the middle of re-running the query (specifically, we                        // 228
      // could be in _publishNewResults). In that case, _unpublishedBuffer is                           // 229
      // empty because we clear it at the beginning of _publishNewResults. In                           // 230
      // this case, our caller already knows the entire answer to the query and                         // 231
      // we don't need to do anything fancy here.  Just return.                                         // 232
      if (self._phase === PHASE.QUERYING)                                                               // 233
        return;                                                                                         // 234
                                                                                                        // 235
      // (b) We're pretty confident that the union of _published and                                    // 236
      // _unpublishedBuffer contain all documents that match selector. Because                          // 237
      // _unpublishedBuffer is empty, that means we're confident that _published                        // 238
      // contains all documents that match selector. So we have nothing to do.                          // 239
      if (self._safeAppendToBuffer)                                                                     // 240
        return;                                                                                         // 241
                                                                                                        // 242
      // (c) Maybe there are other documents out there that should be in our                            // 243
      // buffer. But in that case, when we emptied _unpublishedBuffer in                                // 244
      // _removeBuffered, we should have called _needToPollQuery, which will                            // 245
      // either put something in _unpublishedBuffer or set _safeAppendToBuffer                          // 246
      // (or both), and it will put us in QUERYING for that whole time. So in                           // 247
      // fact, we shouldn't be able to get here.                                                        // 248
                                                                                                        // 249
      throw new Error("Buffer inexplicably empty");                                                     // 250
    });                                                                                                 // 251
  },                                                                                                    // 252
  _changePublished: function (id, oldDoc, newDoc) {                                                     // 253
    var self = this;                                                                                    // 254
    Meteor._noYieldsAllowed(function () {                                                               // 255
      self._published.set(id, self._sharedProjectionFn(newDoc));                                        // 256
      var changed = LocalCollection._makeChangedFields(_.clone(newDoc), oldDoc);                        // 257
      changed = self._projectionFn(changed);                                                            // 258
      if (!_.isEmpty(changed))                                                                          // 259
        self._multiplexer.changed(id, changed);                                                         // 260
    });                                                                                                 // 261
  },                                                                                                    // 262
  _addBuffered: function (id, doc) {                                                                    // 263
    var self = this;                                                                                    // 264
    Meteor._noYieldsAllowed(function () {                                                               // 265
      self._unpublishedBuffer.set(id, self._sharedProjectionFn(doc));                                   // 266
                                                                                                        // 267
      // If something is overflowing the buffer, we just remove it from cache                           // 268
      if (self._unpublishedBuffer.size() > self._limit) {                                               // 269
        var maxBufferedId = self._unpublishedBuffer.maxElementId();                                     // 270
                                                                                                        // 271
        self._unpublishedBuffer.remove(maxBufferedId);                                                  // 272
                                                                                                        // 273
        // Since something matching is removed from cache (both published set and                       // 274
        // buffer), set flag to false                                                                   // 275
        self._safeAppendToBuffer = false;                                                               // 276
      }                                                                                                 // 277
    });                                                                                                 // 278
  },                                                                                                    // 279
  // Is called either to remove the doc completely from matching set or to move                         // 280
  // it to the published set later.                                                                     // 281
  _removeBuffered: function (id) {                                                                      // 282
    var self = this;                                                                                    // 283
    Meteor._noYieldsAllowed(function () {                                                               // 284
      self._unpublishedBuffer.remove(id);                                                               // 285
      // To keep the contract "buffer is never empty in STEADY phase unless the                         // 286
      // everything matching fits into published" true, we poll everything as                           // 287
      // soon as we see the buffer becoming empty.                                                      // 288
      if (! self._unpublishedBuffer.size() && ! self._safeAppendToBuffer)                               // 289
        self._needToPollQuery();                                                                        // 290
    });                                                                                                 // 291
  },                                                                                                    // 292
  // Called when a document has joined the "Matching" results set.                                      // 293
  // Takes responsibility of keeping _unpublishedBuffer in sync with _published                         // 294
  // and the effect of limit enforced.                                                                  // 295
  _addMatching: function (doc) {                                                                        // 296
    var self = this;                                                                                    // 297
    Meteor._noYieldsAllowed(function () {                                                               // 298
      var id = doc._id;                                                                                 // 299
      if (self._published.has(id))                                                                      // 300
        throw Error("tried to add something already published " + id);                                  // 301
      if (self._limit && self._unpublishedBuffer.has(id))                                               // 302
        throw Error("tried to add something already existed in buffer " + id);                          // 303
                                                                                                        // 304
      var limit = self._limit;                                                                          // 305
      var comparator = self._comparator;                                                                // 306
      var maxPublished = (limit && self._published.size() > 0) ?                                        // 307
        self._published.get(self._published.maxElementId()) : null;                                     // 308
      var maxBuffered = (limit && self._unpublishedBuffer.size() > 0)                                   // 309
        ? self._unpublishedBuffer.get(self._unpublishedBuffer.maxElementId())                           // 310
        : null;                                                                                         // 311
      // The query is unlimited or didn't publish enough documents yet or the                           // 312
      // new document would fit into published set pushing the maximum element                          // 313
      // out, then we need to publish the doc.                                                          // 314
      var toPublish = ! limit || self._published.size() < limit ||                                      // 315
        comparator(doc, maxPublished) < 0;                                                              // 316
                                                                                                        // 317
      // Otherwise we might need to buffer it (only in case of limited query).                          // 318
      // Buffering is allowed if the buffer is not filled up yet and all                                // 319
      // matching docs are either in the published set or in the buffer.                                // 320
      var canAppendToBuffer = !toPublish && self._safeAppendToBuffer &&                                 // 321
        self._unpublishedBuffer.size() < limit;                                                         // 322
                                                                                                        // 323
      // Or if it is small enough to be safely inserted to the middle or the                            // 324
      // beginning of the buffer.                                                                       // 325
      var canInsertIntoBuffer = !toPublish && maxBuffered &&                                            // 326
        comparator(doc, maxBuffered) <= 0;                                                              // 327
                                                                                                        // 328
      var toBuffer = canAppendToBuffer || canInsertIntoBuffer;                                          // 329
                                                                                                        // 330
      if (toPublish) {                                                                                  // 331
        self._addPublished(id, doc);                                                                    // 332
      } else if (toBuffer) {                                                                            // 333
        self._addBuffered(id, doc);                                                                     // 334
      } else {                                                                                          // 335
        // dropping it and not saving to the cache                                                      // 336
        self._safeAppendToBuffer = false;                                                               // 337
      }                                                                                                 // 338
    });                                                                                                 // 339
  },                                                                                                    // 340
  // Called when a document leaves the "Matching" results set.                                          // 341
  // Takes responsibility of keeping _unpublishedBuffer in sync with _published                         // 342
  // and the effect of limit enforced.                                                                  // 343
  _removeMatching: function (id) {                                                                      // 344
    var self = this;                                                                                    // 345
    Meteor._noYieldsAllowed(function () {                                                               // 346
      if (! self._published.has(id) && ! self._limit)                                                   // 347
        throw Error("tried to remove something matching but not cached " + id);                         // 348
                                                                                                        // 349
      if (self._published.has(id)) {                                                                    // 350
        self._removePublished(id);                                                                      // 351
      } else if (self._unpublishedBuffer.has(id)) {                                                     // 352
        self._removeBuffered(id);                                                                       // 353
      }                                                                                                 // 354
    });                                                                                                 // 355
  },                                                                                                    // 356
  _handleDoc: function (id, newDoc) {                                                                   // 357
    var self = this;                                                                                    // 358
    Meteor._noYieldsAllowed(function () {                                                               // 359
      var matchesNow = newDoc && self._matcher.documentMatches(newDoc).result;                          // 360
                                                                                                        // 361
      var publishedBefore = self._published.has(id);                                                    // 362
      var bufferedBefore = self._limit && self._unpublishedBuffer.has(id);                              // 363
      var cachedBefore = publishedBefore || bufferedBefore;                                             // 364
                                                                                                        // 365
      if (matchesNow && !cachedBefore) {                                                                // 366
        self._addMatching(newDoc);                                                                      // 367
      } else if (cachedBefore && !matchesNow) {                                                         // 368
        self._removeMatching(id);                                                                       // 369
      } else if (cachedBefore && matchesNow) {                                                          // 370
        var oldDoc = self._published.get(id);                                                           // 371
        var comparator = self._comparator;                                                              // 372
        var minBuffered = self._limit && self._unpublishedBuffer.size() &&                              // 373
          self._unpublishedBuffer.get(self._unpublishedBuffer.minElementId());                          // 374
                                                                                                        // 375
        if (publishedBefore) {                                                                          // 376
          // Unlimited case where the document stays in published once it                               // 377
          // matches or the case when we don't have enough matching docs to                             // 378
          // publish or the changed but matching doc will stay in published                             // 379
          // anyways.                                                                                   // 380
          //                                                                                            // 381
          // XXX: We rely on the emptiness of buffer. Be sure to maintain the                           // 382
          // fact that buffer can't be empty if there are matching documents not                        // 383
          // published. Notably, we don't want to schedule repoll and continue                          // 384
          // relying on this property.                                                                  // 385
          var staysInPublished = ! self._limit ||                                                       // 386
            self._unpublishedBuffer.size() === 0 ||                                                     // 387
            comparator(newDoc, minBuffered) <= 0;                                                       // 388
                                                                                                        // 389
          if (staysInPublished) {                                                                       // 390
            self._changePublished(id, oldDoc, newDoc);                                                  // 391
          } else {                                                                                      // 392
            // after the change doc doesn't stay in the published, remove it                            // 393
            self._removePublished(id);                                                                  // 394
            // but it can move into buffered now, check it                                              // 395
            var maxBuffered = self._unpublishedBuffer.get(                                              // 396
              self._unpublishedBuffer.maxElementId());                                                  // 397
                                                                                                        // 398
            var toBuffer = self._safeAppendToBuffer ||                                                  // 399
                  (maxBuffered && comparator(newDoc, maxBuffered) <= 0);                                // 400
                                                                                                        // 401
            if (toBuffer) {                                                                             // 402
              self._addBuffered(id, newDoc);                                                            // 403
            } else {                                                                                    // 404
              // Throw away from both published set and buffer                                          // 405
              self._safeAppendToBuffer = false;                                                         // 406
            }                                                                                           // 407
          }                                                                                             // 408
        } else if (bufferedBefore) {                                                                    // 409
          oldDoc = self._unpublishedBuffer.get(id);                                                     // 410
          // remove the old version manually instead of using _removeBuffered so                        // 411
          // we don't trigger the querying immediately.  if we end this block                           // 412
          // with the buffer empty, we will need to trigger the query poll                              // 413
          // manually too.                                                                              // 414
          self._unpublishedBuffer.remove(id);                                                           // 415
                                                                                                        // 416
          var maxPublished = self._published.get(                                                       // 417
            self._published.maxElementId());                                                            // 418
          var maxBuffered = self._unpublishedBuffer.size() &&                                           // 419
                self._unpublishedBuffer.get(                                                            // 420
                  self._unpublishedBuffer.maxElementId());                                              // 421
                                                                                                        // 422
          // the buffered doc was updated, it could move to published                                   // 423
          var toPublish = comparator(newDoc, maxPublished) < 0;                                         // 424
                                                                                                        // 425
          // or stays in buffer even after the change                                                   // 426
          var staysInBuffer = (! toPublish && self._safeAppendToBuffer) ||                              // 427
                (!toPublish && maxBuffered &&                                                           // 428
                 comparator(newDoc, maxBuffered) <= 0);                                                 // 429
                                                                                                        // 430
          if (toPublish) {                                                                              // 431
            self._addPublished(id, newDoc);                                                             // 432
          } else if (staysInBuffer) {                                                                   // 433
            // stays in buffer but changes                                                              // 434
            self._unpublishedBuffer.set(id, newDoc);                                                    // 435
          } else {                                                                                      // 436
            // Throw away from both published set and buffer                                            // 437
            self._safeAppendToBuffer = false;                                                           // 438
            // Normally this check would have been done in _removeBuffered but                          // 439
            // we didn't use it, so we need to do it ourself now.                                       // 440
            if (! self._unpublishedBuffer.size()) {                                                     // 441
              self._needToPollQuery();                                                                  // 442
            }                                                                                           // 443
          }                                                                                             // 444
        } else {                                                                                        // 445
          throw new Error("cachedBefore implies either of publishedBefore or bufferedBefore is true."); // 446
        }                                                                                               // 447
      }                                                                                                 // 448
    });                                                                                                 // 449
  },                                                                                                    // 450
  _fetchModifiedDocuments: function () {                                                                // 451
    var self = this;                                                                                    // 452
    Meteor._noYieldsAllowed(function () {                                                               // 453
      self._registerPhaseChange(PHASE.FETCHING);                                                        // 454
      // Defer, because nothing called from the oplog entry handler may yield,                          // 455
      // but fetch() yields.                                                                            // 456
      Meteor.defer(finishIfNeedToPollQuery(function () {                                                // 457
        while (!self._stopped && !self._needToFetch.empty()) {                                          // 458
          if (self._phase === PHASE.QUERYING) {                                                         // 459
            // While fetching, we decided to go into QUERYING mode, and then we                         // 460
            // saw another oplog entry, so _needToFetch is not empty. But we                            // 461
            // shouldn't fetch these documents until AFTER the query is done.                           // 462
            break;                                                                                      // 463
          }                                                                                             // 464
                                                                                                        // 465
          // Being in steady phase here would be surprising.                                            // 466
          if (self._phase !== PHASE.FETCHING)                                                           // 467
            throw new Error("phase in fetchModifiedDocuments: " + self._phase);                         // 468
                                                                                                        // 469
          self._currentlyFetching = self._needToFetch;                                                  // 470
          var thisGeneration = ++self._fetchGeneration;                                                 // 471
          self._needToFetch = new LocalCollection._IdMap;                                               // 472
          var waiting = 0;                                                                              // 473
          var fut = new Future;                                                                         // 474
          // This loop is safe, because _currentlyFetching will not be updated                          // 475
          // during this loop (in fact, it is never mutated).                                           // 476
          self._currentlyFetching.forEach(function (cacheKey, id) {                                     // 477
            waiting++;                                                                                  // 478
            self._mongoHandle._docFetcher.fetch(                                                        // 479
              self._cursorDescription.collectionName, id, cacheKey,                                     // 480
              finishIfNeedToPollQuery(function (err, doc) {                                             // 481
                try {                                                                                   // 482
                  if (err) {                                                                            // 483
                    Meteor._debug("Got exception while fetching documents: " +                          // 484
                                  err);                                                                 // 485
                    // If we get an error from the fetcher (eg, trouble                                 // 486
                    // connecting to Mongo), let's just abandon the fetch phase                         // 487
                    // altogether and fall back to polling. It's not like we're                         // 488
                    // getting live updates anyway.                                                     // 489
                    if (self._phase !== PHASE.QUERYING) {                                               // 490
                      self._needToPollQuery();                                                          // 491
                    }                                                                                   // 492
                  } else if (!self._stopped && self._phase === PHASE.FETCHING                           // 493
                             && self._fetchGeneration === thisGeneration) {                             // 494
                    // We re-check the generation in case we've had an explicit                         // 495
                    // _pollQuery call (eg, in another fiber) which should                              // 496
                    // effectively cancel this round of fetches.  (_pollQuery                           // 497
                    // increments the generation.)                                                      // 498
                    self._handleDoc(id, doc);                                                           // 499
                  }                                                                                     // 500
                } finally {                                                                             // 501
                  waiting--;                                                                            // 502
                  // Because fetch() never calls its callback synchronously,                            // 503
                  // this is safe (ie, we won't call fut.return() before the                            // 504
                  // forEach is done).                                                                  // 505
                  if (waiting === 0)                                                                    // 506
                    fut.return();                                                                       // 507
                }                                                                                       // 508
              }));                                                                                      // 509
          });                                                                                           // 510
          fut.wait();                                                                                   // 511
          // Exit now if we've had a _pollQuery call (here or in another fiber).                        // 512
          if (self._phase === PHASE.QUERYING)                                                           // 513
            return;                                                                                     // 514
          self._currentlyFetching = null;                                                               // 515
        }                                                                                               // 516
        // We're done fetching, so we can be steady, unless we've had a                                 // 517
        // _pollQuery call (here or in another fiber).                                                  // 518
        if (self._phase !== PHASE.QUERYING)                                                             // 519
          self._beSteady();                                                                             // 520
      }));                                                                                              // 521
    });                                                                                                 // 522
  },                                                                                                    // 523
  _beSteady: function () {                                                                              // 524
    var self = this;                                                                                    // 525
    Meteor._noYieldsAllowed(function () {                                                               // 526
      self._registerPhaseChange(PHASE.STEADY);                                                          // 527
      var writes = self._writesToCommitWhenWeReachSteady;                                               // 528
      self._writesToCommitWhenWeReachSteady = [];                                                       // 529
      self._multiplexer.onFlush(function () {                                                           // 530
        _.each(writes, function (w) {                                                                   // 531
          w.committed();                                                                                // 532
        });                                                                                             // 533
      });                                                                                               // 534
    });                                                                                                 // 535
  },                                                                                                    // 536
  _handleOplogEntryQuerying: function (op) {                                                            // 537
    var self = this;                                                                                    // 538
    Meteor._noYieldsAllowed(function () {                                                               // 539
      self._needToFetch.set(idForOp(op), op.ts.toString());                                             // 540
    });                                                                                                 // 541
  },                                                                                                    // 542
  _handleOplogEntrySteadyOrFetching: function (op) {                                                    // 543
    var self = this;                                                                                    // 544
    Meteor._noYieldsAllowed(function () {                                                               // 545
      var id = idForOp(op);                                                                             // 546
      // If we're already fetching this one, or about to, we can't optimize;                            // 547
      // make sure that we fetch it again if necessary.                                                 // 548
      if (self._phase === PHASE.FETCHING &&                                                             // 549
          ((self._currentlyFetching && self._currentlyFetching.has(id)) ||                              // 550
           self._needToFetch.has(id))) {                                                                // 551
        self._needToFetch.set(id, op.ts.toString());                                                    // 552
        return;                                                                                         // 553
      }                                                                                                 // 554
                                                                                                        // 555
      if (op.op === 'd') {                                                                              // 556
        if (self._published.has(id) ||                                                                  // 557
            (self._limit && self._unpublishedBuffer.has(id)))                                           // 558
          self._removeMatching(id);                                                                     // 559
      } else if (op.op === 'i') {                                                                       // 560
        if (self._published.has(id))                                                                    // 561
          throw new Error("insert found for already-existing ID in published");                         // 562
        if (self._unpublishedBuffer && self._unpublishedBuffer.has(id))                                 // 563
          throw new Error("insert found for already-existing ID in buffer");                            // 564
                                                                                                        // 565
        // XXX what if selector yields?  for now it can't but later it could                            // 566
        // have $where                                                                                  // 567
        if (self._matcher.documentMatches(op.o).result)                                                 // 568
          self._addMatching(op.o);                                                                      // 569
      } else if (op.op === 'u') {                                                                       // 570
        // Is this a modifier ($set/$unset, which may require us to poll the                            // 571
        // database to figure out if the whole document matches the selector) or                        // 572
        // a replacement (in which case we can just directly re-evaluate the                            // 573
        // selector)?                                                                                   // 574
        var isReplace = !_.has(op.o, '$set') && !_.has(op.o, '$unset');                                 // 575
        // If this modifier modifies something inside an EJSON custom type (ie,                         // 576
        // anything with EJSON$), then we can't try to use                                              // 577
        // LocalCollection._modify, since that just mutates the EJSON encoding,                         // 578
        // not the actual object.                                                                       // 579
        var canDirectlyModifyDoc =                                                                      // 580
          !isReplace && modifierCanBeDirectlyApplied(op.o);                                             // 581
                                                                                                        // 582
        var publishedBefore = self._published.has(id);                                                  // 583
        var bufferedBefore = self._limit && self._unpublishedBuffer.has(id);                            // 584
                                                                                                        // 585
        if (isReplace) {                                                                                // 586
          self._handleDoc(id, _.extend({_id: id}, op.o));                                               // 587
        } else if ((publishedBefore || bufferedBefore) &&                                               // 588
                   canDirectlyModifyDoc) {                                                              // 589
          // Oh great, we actually know what the document is, so we can apply                           // 590
          // this directly.                                                                             // 591
          var newDoc = self._published.has(id)                                                          // 592
            ? self._published.get(id) : self._unpublishedBuffer.get(id);                                // 593
          newDoc = EJSON.clone(newDoc);                                                                 // 594
                                                                                                        // 595
          newDoc._id = id;                                                                              // 596
          LocalCollection._modify(newDoc, op.o);                                                        // 597
          self._handleDoc(id, self._sharedProjectionFn(newDoc));                                        // 598
        } else if (!canDirectlyModifyDoc ||                                                             // 599
                   self._matcher.canBecomeTrueByModifier(op.o) ||                                       // 600
                   (self._sorter && self._sorter.affectedByModifier(op.o))) {                           // 601
          self._needToFetch.set(id, op.ts.toString());                                                  // 602
          if (self._phase === PHASE.STEADY)                                                             // 603
            self._fetchModifiedDocuments();                                                             // 604
        }                                                                                               // 605
      } else {                                                                                          // 606
        throw Error("XXX SURPRISING OPERATION: " + op);                                                 // 607
      }                                                                                                 // 608
    });                                                                                                 // 609
  },                                                                                                    // 610
  // Yields!                                                                                            // 611
  _runInitialQuery: function () {                                                                       // 612
    var self = this;                                                                                    // 613
    if (self._stopped)                                                                                  // 614
      throw new Error("oplog stopped surprisingly early");                                              // 615
                                                                                                        // 616
    self._runQuery();  // yields                                                                        // 617
                                                                                                        // 618
    if (self._stopped)                                                                                  // 619
      throw new Error("oplog stopped quite early");                                                     // 620
    // Allow observeChanges calls to return. (After this, it's possible for                             // 621
    // stop() to be called.)                                                                            // 622
    self._multiplexer.ready();                                                                          // 623
                                                                                                        // 624
    self._doneQuerying();  // yields                                                                    // 625
  },                                                                                                    // 626
                                                                                                        // 627
  // In various circumstances, we may just want to stop processing the oplog and                        // 628
  // re-run the initial query, just as if we were a PollingObserveDriver.                               // 629
  //                                                                                                    // 630
  // This function may not block, because it is called from an oplog entry                              // 631
  // handler.                                                                                           // 632
  //                                                                                                    // 633
  // XXX We should call this when we detect that we've been in FETCHING for "too                        // 634
  // long".                                                                                             // 635
  //                                                                                                    // 636
  // XXX We should call this when we detect Mongo failover (since that might                            // 637
  // mean that some of the oplog entries we have processed have been rolled                             // 638
  // back). The Node Mongo driver is in the middle of a bunch of huge                                   // 639
  // refactorings, including the way that it notifies you when primary                                  // 640
  // changes. Will put off implementing this until driver 1.4 is out.                                   // 641
  _pollQuery: function () {                                                                             // 642
    var self = this;                                                                                    // 643
    Meteor._noYieldsAllowed(function () {                                                               // 644
      if (self._stopped)                                                                                // 645
        return;                                                                                         // 646
                                                                                                        // 647
      // Yay, we get to forget about all the things we thought we had to fetch.                         // 648
      self._needToFetch = new LocalCollection._IdMap;                                                   // 649
      self._currentlyFetching = null;                                                                   // 650
      ++self._fetchGeneration;  // ignore any in-flight fetches                                         // 651
      self._registerPhaseChange(PHASE.QUERYING);                                                        // 652
                                                                                                        // 653
      // Defer so that we don't yield.  We don't need finishIfNeedToPollQuery                           // 654
      // here because SwitchedToQuery is not thrown in QUERYING mode.                                   // 655
      Meteor.defer(function () {                                                                        // 656
        self._runQuery();                                                                               // 657
        self._doneQuerying();                                                                           // 658
      });                                                                                               // 659
    });                                                                                                 // 660
  },                                                                                                    // 661
                                                                                                        // 662
  // Yields!                                                                                            // 663
  _runQuery: function () {                                                                              // 664
    var self = this;                                                                                    // 665
    var newResults, newBuffer;                                                                          // 666
                                                                                                        // 667
    // This while loop is just to retry failures.                                                       // 668
    while (true) {                                                                                      // 669
      // If we've been stopped, we don't have to run anything any more.                                 // 670
      if (self._stopped)                                                                                // 671
        return;                                                                                         // 672
                                                                                                        // 673
      newResults = new LocalCollection._IdMap;                                                          // 674
      newBuffer = new LocalCollection._IdMap;                                                           // 675
                                                                                                        // 676
      // Query 2x documents as the half excluded from the original query will go                        // 677
      // into unpublished buffer to reduce additional Mongo lookups in cases                            // 678
      // when documents are removed from the published set and need a                                   // 679
      // replacement.                                                                                   // 680
      // XXX needs more thought on non-zero skip                                                        // 681
      // XXX 2 is a "magic number" meaning there is an extra chunk of docs for                          // 682
      // buffer if such is needed.                                                                      // 683
      var cursor = self._cursorForQuery({ limit: self._limit * 2 });                                    // 684
      try {                                                                                             // 685
        cursor.forEach(function (doc, i) {  // yields                                                   // 686
          if (!self._limit || i < self._limit)                                                          // 687
            newResults.set(doc._id, doc);                                                               // 688
          else                                                                                          // 689
            newBuffer.set(doc._id, doc);                                                                // 690
        });                                                                                             // 691
        break;                                                                                          // 692
      } catch (e) {                                                                                     // 693
        // During failover (eg) if we get an exception we should log and retry                          // 694
        // instead of crashing.                                                                         // 695
        Meteor._debug("Got exception while polling query: " + e);                                       // 696
        Meteor._sleepForMs(100);                                                                        // 697
      }                                                                                                 // 698
    }                                                                                                   // 699
                                                                                                        // 700
    if (self._stopped)                                                                                  // 701
      return;                                                                                           // 702
                                                                                                        // 703
    self._publishNewResults(newResults, newBuffer);                                                     // 704
  },                                                                                                    // 705
                                                                                                        // 706
  // Transitions to QUERYING and runs another query, or (if already in QUERYING)                        // 707
  // ensures that we will query again later.                                                            // 708
  //                                                                                                    // 709
  // This function may not block, because it is called from an oplog entry                              // 710
  // handler. However, if we were not already in the QUERYING phase, it throws                          // 711
  // an exception that is caught by the closest surrounding                                             // 712
  // finishIfNeedToPollQuery call; this ensures that we don't continue running                          // 713
  // close that was designed for another phase inside PHASE.QUERYING.                                   // 714
  //                                                                                                    // 715
  // (It's also necessary whenever logic in this file yields to check that other                        // 716
  // phases haven't put us into QUERYING mode, though; eg,                                              // 717
  // _fetchModifiedDocuments does this.)                                                                // 718
  _needToPollQuery: function () {                                                                       // 719
    var self = this;                                                                                    // 720
    Meteor._noYieldsAllowed(function () {                                                               // 721
      if (self._stopped)                                                                                // 722
        return;                                                                                         // 723
                                                                                                        // 724
      // If we're not already in the middle of a query, we can query now                                // 725
      // (possibly pausing FETCHING).                                                                   // 726
      if (self._phase !== PHASE.QUERYING) {                                                             // 727
        self._pollQuery();                                                                              // 728
        throw new SwitchedToQuery;                                                                      // 729
      }                                                                                                 // 730
                                                                                                        // 731
      // We're currently in QUERYING. Set a flag to ensure that we run another                          // 732
      // query when we're done.                                                                         // 733
      self._requeryWhenDoneThisQuery = true;                                                            // 734
    });                                                                                                 // 735
  },                                                                                                    // 736
                                                                                                        // 737
  // Yields!                                                                                            // 738
  _doneQuerying: function () {                                                                          // 739
    var self = this;                                                                                    // 740
                                                                                                        // 741
    if (self._stopped)                                                                                  // 742
      return;                                                                                           // 743
    self._mongoHandle._oplogHandle.waitUntilCaughtUp();  // yields                                      // 744
    if (self._stopped)                                                                                  // 745
      return;                                                                                           // 746
    if (self._phase !== PHASE.QUERYING)                                                                 // 747
      throw Error("Phase unexpectedly " + self._phase);                                                 // 748
                                                                                                        // 749
    Meteor._noYieldsAllowed(function () {                                                               // 750
      if (self._requeryWhenDoneThisQuery) {                                                             // 751
        self._requeryWhenDoneThisQuery = false;                                                         // 752
        self._pollQuery();                                                                              // 753
      } else if (self._needToFetch.empty()) {                                                           // 754
        self._beSteady();                                                                               // 755
      } else {                                                                                          // 756
        self._fetchModifiedDocuments();                                                                 // 757
      }                                                                                                 // 758
    });                                                                                                 // 759
  },                                                                                                    // 760
                                                                                                        // 761
  _cursorForQuery: function (optionsOverwrite) {                                                        // 762
    var self = this;                                                                                    // 763
    return Meteor._noYieldsAllowed(function () {                                                        // 764
      // The query we run is almost the same as the cursor we are observing,                            // 765
      // with a few changes. We need to read all the fields that are relevant to                        // 766
      // the selector, not just the fields we are going to publish (that's the                          // 767
      // "shared" projection). And we don't want to apply any transform in the                          // 768
      // cursor, because observeChanges shouldn't use the transform.                                    // 769
      var options = _.clone(self._cursorDescription.options);                                           // 770
                                                                                                        // 771
      // Allow the caller to modify the options. Useful to specify different                            // 772
      // skip and limit values.                                                                         // 773
      _.extend(options, optionsOverwrite);                                                              // 774
                                                                                                        // 775
      options.fields = self._sharedProjection;                                                          // 776
      delete options.transform;                                                                         // 777
      // We are NOT deep cloning fields or selector here, which should be OK.                           // 778
      var description = new CursorDescription(                                                          // 779
        self._cursorDescription.collectionName,                                                         // 780
        self._cursorDescription.selector,                                                               // 781
        options);                                                                                       // 782
      return new Cursor(self._mongoHandle, description);                                                // 783
    });                                                                                                 // 784
  },                                                                                                    // 785
                                                                                                        // 786
                                                                                                        // 787
  // Replace self._published with newResults (both are IdMaps), invoking observe                        // 788
  // callbacks on the multiplexer.                                                                      // 789
  // Replace self._unpublishedBuffer with newBuffer.                                                    // 790
  //                                                                                                    // 791
  // XXX This is very similar to LocalCollection._diffQueryUnorderedChanges. We                         // 792
  // should really: (a) Unify IdMap and OrderedDict into Unordered/OrderedDict                          // 793
  // (b) Rewrite diff.js to use these classes instead of arrays and objects.                            // 794
  _publishNewResults: function (newResults, newBuffer) {                                                // 795
    var self = this;                                                                                    // 796
    Meteor._noYieldsAllowed(function () {                                                               // 797
                                                                                                        // 798
      // If the query is limited and there is a buffer, shut down so it doesn't                         // 799
      // stay in a way.                                                                                 // 800
      if (self._limit) {                                                                                // 801
        self._unpublishedBuffer.clear();                                                                // 802
      }                                                                                                 // 803
                                                                                                        // 804
      // First remove anything that's gone. Be careful not to modify                                    // 805
      // self._published while iterating over it.                                                       // 806
      var idsToRemove = [];                                                                             // 807
      self._published.forEach(function (doc, id) {                                                      // 808
        if (!newResults.has(id))                                                                        // 809
          idsToRemove.push(id);                                                                         // 810
      });                                                                                               // 811
      _.each(idsToRemove, function (id) {                                                               // 812
        self._removePublished(id);                                                                      // 813
      });                                                                                               // 814
                                                                                                        // 815
      // Now do adds and changes.                                                                       // 816
      // If self has a buffer and limit, the new fetched result will be                                 // 817
      // limited correctly as the query has sort specifier.                                             // 818
      newResults.forEach(function (doc, id) {                                                           // 819
        self._handleDoc(id, doc);                                                                       // 820
      });                                                                                               // 821
                                                                                                        // 822
      // Sanity-check that everything we tried to put into _published ended up                          // 823
      // there.                                                                                         // 824
      // XXX if this is slow, remove it later                                                           // 825
      if (self._published.size() !== newResults.size()) {                                               // 826
        throw Error("failed to copy newResults into _published!");                                      // 827
      }                                                                                                 // 828
      self._published.forEach(function (doc, id) {                                                      // 829
        if (!newResults.has(id))                                                                        // 830
          throw Error("_published has a doc that newResults doesn't; " + id);                           // 831
      });                                                                                               // 832
                                                                                                        // 833
      // Finally, replace the buffer                                                                    // 834
      newBuffer.forEach(function (doc, id) {                                                            // 835
        self._addBuffered(id, doc);                                                                     // 836
      });                                                                                               // 837
                                                                                                        // 838
      self._safeAppendToBuffer = newBuffer.size() < self._limit;                                        // 839
    });                                                                                                 // 840
  },                                                                                                    // 841
                                                                                                        // 842
  // This stop function is invoked from the onStop of the ObserveMultiplexer, so                        // 843
  // it shouldn't actually be possible to call it until the multiplexer is                              // 844
  // ready.                                                                                             // 845
  //                                                                                                    // 846
  // It's important to check self._stopped after every call in this file that                           // 847
  // can yield!                                                                                         // 848
  stop: function () {                                                                                   // 849
    var self = this;                                                                                    // 850
    if (self._stopped)                                                                                  // 851
      return;                                                                                           // 852
    self._stopped = true;                                                                               // 853
    _.each(self._stopHandles, function (handle) {                                                       // 854
      handle.stop();                                                                                    // 855
    });                                                                                                 // 856
                                                                                                        // 857
    // Note: we *don't* use multiplexer.onFlush here because this stop                                  // 858
    // callback is actually invoked by the multiplexer itself when it has                               // 859
    // determined that there are no handles left. So nothing is actually going                          // 860
    // to get flushed (and it's probably not valid to call methods on the                               // 861
    // dying multiplexer).                                                                              // 862
    _.each(self._writesToCommitWhenWeReachSteady, function (w) {                                        // 863
      w.committed();  // maybe yields?                                                                  // 864
    });                                                                                                 // 865
    self._writesToCommitWhenWeReachSteady = null;                                                       // 866
                                                                                                        // 867
    // Proactively drop references to potentially big things.                                           // 868
    self._published = null;                                                                             // 869
    self._unpublishedBuffer = null;                                                                     // 870
    self._needToFetch = null;                                                                           // 871
    self._currentlyFetching = null;                                                                     // 872
    self._oplogEntryHandle = null;                                                                      // 873
    self._listenersHandle = null;                                                                       // 874
                                                                                                        // 875
    Package.facts && Package.facts.Facts.incrementServerFact(                                           // 876
      "mongo-livedata", "observe-drivers-oplog", -1);                                                   // 877
  },                                                                                                    // 878
                                                                                                        // 879
  _registerPhaseChange: function (phase) {                                                              // 880
    var self = this;                                                                                    // 881
    Meteor._noYieldsAllowed(function () {                                                               // 882
      var now = new Date;                                                                               // 883
                                                                                                        // 884
      if (self._phase) {                                                                                // 885
        var timeDiff = now - self._phaseStartTime;                                                      // 886
        Package.facts && Package.facts.Facts.incrementServerFact(                                       // 887
          "mongo-livedata", "time-spent-in-" + self._phase + "-phase", timeDiff);                       // 888
      }                                                                                                 // 889
                                                                                                        // 890
      self._phase = phase;                                                                              // 891
      self._phaseStartTime = now;                                                                       // 892
    });                                                                                                 // 893
  }                                                                                                     // 894
});                                                                                                     // 895
                                                                                                        // 896
// Does our oplog tailing code support this cursor? For now, we are being very                          // 897
// conservative and allowing only simple queries with simple options.                                   // 898
// (This is a "static method".)                                                                         // 899
OplogObserveDriver.cursorSupported = function (cursorDescription, matcher) {                            // 900
  // First, check the options.                                                                          // 901
  var options = cursorDescription.options;                                                              // 902
                                                                                                        // 903
  // Did the user say no explicitly?                                                                    // 904
  if (options._disableOplog)                                                                            // 905
    return false;                                                                                       // 906
                                                                                                        // 907
  // skip is not supported: to support it we would need to keep track of all                            // 908
  // "skipped" documents or at least their ids.                                                         // 909
  // limit w/o a sort specifier is not supported: current implementation needs a                        // 910
  // deterministic way to order documents.                                                              // 911
  if (options.skip || (options.limit && !options.sort)) return false;                                   // 912
                                                                                                        // 913
  // If a fields projection option is given check if it is supported by                                 // 914
  // minimongo (some operators are not supported).                                                      // 915
  if (options.fields) {                                                                                 // 916
    try {                                                                                               // 917
      LocalCollection._checkSupportedProjection(options.fields);                                        // 918
    } catch (e) {                                                                                       // 919
      if (e.name === "MinimongoError")                                                                  // 920
        return false;                                                                                   // 921
      else                                                                                              // 922
        throw e;                                                                                        // 923
    }                                                                                                   // 924
  }                                                                                                     // 925
                                                                                                        // 926
  // We don't allow the following selectors:                                                            // 927
  //   - $where (not confident that we provide the same JS environment                                  // 928
  //             as Mongo, and can yield!)                                                              // 929
  //   - $near (has "interesting" properties in MongoDB, like the possibility                           // 930
  //            of returning an ID multiple times, though even polling maybe                            // 931
  //            have a bug there)                                                                       // 932
  //           XXX: once we support it, we would need to think more on how we                           // 933
  //           initialize the comparators when we create the driver.                                    // 934
  return !matcher.hasWhere() && !matcher.hasGeoQuery();                                                 // 935
};                                                                                                      // 936
                                                                                                        // 937
var modifierCanBeDirectlyApplied = function (modifier) {                                                // 938
  return _.all(modifier, function (fields, operation) {                                                 // 939
    return _.all(fields, function (value, field) {                                                      // 940
      return !/EJSON\$/.test(field);                                                                    // 941
    });                                                                                                 // 942
  });                                                                                                   // 943
};                                                                                                      // 944
                                                                                                        // 945
MongoInternals.OplogObserveDriver = OplogObserveDriver;                                                 // 946
                                                                                                        // 947
//////////////////////////////////////////////////////////////////////////////////////////////////////////

}).call(this);






(function () {

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                                      //
// packages/mongo-livedata/local_collection_driver.js                                                   //
//                                                                                                      //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                                                        //
LocalCollectionDriver = function () {                                                                   // 1
  var self = this;                                                                                      // 2
  self.noConnCollections = {};                                                                          // 3
};                                                                                                      // 4
                                                                                                        // 5
var ensureCollection = function (name, collections) {                                                   // 6
  if (!(name in collections))                                                                           // 7
    collections[name] = new LocalCollection(name);                                                      // 8
  return collections[name];                                                                             // 9
};                                                                                                      // 10
                                                                                                        // 11
_.extend(LocalCollectionDriver.prototype, {                                                             // 12
  open: function (name, conn) {                                                                         // 13
    var self = this;                                                                                    // 14
    if (!name)                                                                                          // 15
      return new LocalCollection;                                                                       // 16
    if (! conn) {                                                                                       // 17
      return ensureCollection(name, self.noConnCollections);                                            // 18
    }                                                                                                   // 19
    if (! conn._mongo_livedata_collections)                                                             // 20
      conn._mongo_livedata_collections = {};                                                            // 21
    // XXX is there a way to keep track of a connection's collections without                           // 22
    // dangling it off the connection object?                                                           // 23
    return ensureCollection(name, conn._mongo_livedata_collections);                                    // 24
  }                                                                                                     // 25
});                                                                                                     // 26
                                                                                                        // 27
// singleton                                                                                            // 28
LocalCollectionDriver = new LocalCollectionDriver;                                                      // 29
                                                                                                        // 30
//////////////////////////////////////////////////////////////////////////////////////////////////////////

}).call(this);






(function () {

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                                      //
// packages/mongo-livedata/remote_collection_driver.js                                                  //
//                                                                                                      //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                                                        //
MongoInternals.RemoteCollectionDriver = function (                                                      // 1
  mongo_url, options) {                                                                                 // 2
  var self = this;                                                                                      // 3
  self.mongo = new MongoConnection(mongo_url, options);                                                 // 4
};                                                                                                      // 5
                                                                                                        // 6
_.extend(MongoInternals.RemoteCollectionDriver.prototype, {                                             // 7
  open: function (name) {                                                                               // 8
    var self = this;                                                                                    // 9
    var ret = {};                                                                                       // 10
    _.each(                                                                                             // 11
      ['find', 'findOne', 'insert', 'update', , 'upsert',                                               // 12
       'remove', '_ensureIndex', '_dropIndex', '_createCappedCollection',                               // 13
       'dropCollection'],                                                                               // 14
      function (m) {                                                                                    // 15
        ret[m] = _.bind(self.mongo[m], self.mongo, name);                                               // 16
      });                                                                                               // 17
    return ret;                                                                                         // 18
  }                                                                                                     // 19
});                                                                                                     // 20
                                                                                                        // 21
                                                                                                        // 22
// Create the singleton RemoteCollectionDriver only on demand, so we                                    // 23
// only require Mongo configuration if it's actually used (eg, not if                                   // 24
// you're only trying to receive data from a remote DDP server.)                                        // 25
MongoInternals.defaultRemoteCollectionDriver = _.once(function () {                                     // 26
  var mongoUrl;                                                                                         // 27
  var connectionOptions = {};                                                                           // 28
                                                                                                        // 29
  AppConfig.configurePackage("mongo-livedata", function (config) {                                      // 30
    // This will keep running if mongo gets reconfigured.  That's not ideal, but                        // 31
    // should be ok for now.                                                                            // 32
    mongoUrl = config.url;                                                                              // 33
                                                                                                        // 34
    if (config.oplog)                                                                                   // 35
      connectionOptions.oplogUrl = config.oplog;                                                        // 36
  });                                                                                                   // 37
                                                                                                        // 38
  // XXX bad error since it could also be set directly in METEOR_DEPLOY_CONFIG                          // 39
  if (! mongoUrl)                                                                                       // 40
    throw new Error("MONGO_URL must be set in environment");                                            // 41
                                                                                                        // 42
                                                                                                        // 43
  return new MongoInternals.RemoteCollectionDriver(mongoUrl, connectionOptions);                        // 44
});                                                                                                     // 45
                                                                                                        // 46
//////////////////////////////////////////////////////////////////////////////////////////////////////////

}).call(this);






(function () {

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                                      //
// packages/mongo-livedata/collection.js                                                                //
//                                                                                                      //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                                                        //
// options.connection, if given, is a LivedataClient or LivedataServer                                  // 1
// XXX presently there is no way to destroy/clean up a Collection                                       // 2
                                                                                                        // 3
Meteor.Collection = function (name, options) {                                                          // 4
  var self = this;                                                                                      // 5
  if (! (self instanceof Meteor.Collection))                                                            // 6
    throw new Error('use "new" to construct a Meteor.Collection');                                      // 7
                                                                                                        // 8
  if (!name && (name !== null)) {                                                                       // 9
    Meteor._debug("Warning: creating anonymous collection. It will not be " +                           // 10
                  "saved or synchronized over the network. (Pass null for " +                           // 11
                  "the collection name to turn off this warning.)");                                    // 12
    name = null;                                                                                        // 13
  }                                                                                                     // 14
                                                                                                        // 15
  if (name !== null && typeof name !== "string") {                                                      // 16
    throw new Error(                                                                                    // 17
      "First argument to new Meteor.Collection must be a string or null");                              // 18
  }                                                                                                     // 19
                                                                                                        // 20
  if (options && options.methods) {                                                                     // 21
    // Backwards compatibility hack with original signature (which passed                               // 22
    // "connection" directly instead of in options. (Connections must have a "methods"                  // 23
    // method.)                                                                                         // 24
    // XXX remove before 1.0                                                                            // 25
    options = {connection: options};                                                                    // 26
  }                                                                                                     // 27
  // Backwards compatibility: "connection" used to be called "manager".                                 // 28
  if (options && options.manager && !options.connection) {                                              // 29
    options.connection = options.manager;                                                               // 30
  }                                                                                                     // 31
  options = _.extend({                                                                                  // 32
    connection: undefined,                                                                              // 33
    idGeneration: 'STRING',                                                                             // 34
    transform: null,                                                                                    // 35
    _driver: undefined,                                                                                 // 36
    _preventAutopublish: false                                                                          // 37
  }, options);                                                                                          // 38
                                                                                                        // 39
  switch (options.idGeneration) {                                                                       // 40
  case 'MONGO':                                                                                         // 41
    self._makeNewID = function () {                                                                     // 42
      var src = name ? DDP.randomStream('/collection/' + name) : Random;                                // 43
      return new Meteor.Collection.ObjectID(src.hexString(24));                                         // 44
    };                                                                                                  // 45
    break;                                                                                              // 46
  case 'STRING':                                                                                        // 47
  default:                                                                                              // 48
    self._makeNewID = function () {                                                                     // 49
      var src = name ? DDP.randomStream('/collection/' + name) : Random;                                // 50
      return src.id();                                                                                  // 51
    };                                                                                                  // 52
    break;                                                                                              // 53
  }                                                                                                     // 54
                                                                                                        // 55
  self._transform = LocalCollection.wrapTransform(options.transform);                                   // 56
                                                                                                        // 57
  if (! name || options.connection === null)                                                            // 58
    // note: nameless collections never have a connection                                               // 59
    self._connection = null;                                                                            // 60
  else if (options.connection)                                                                          // 61
    self._connection = options.connection;                                                              // 62
  else if (Meteor.isClient)                                                                             // 63
    self._connection = Meteor.connection;                                                               // 64
  else                                                                                                  // 65
    self._connection = Meteor.server;                                                                   // 66
                                                                                                        // 67
  if (!options._driver) {                                                                               // 68
    // XXX This check assumes that webapp is loaded so that Meteor.server !==                           // 69
    // null. We should fully support the case of "want to use a Mongo-backed                            // 70
    // collection from Node code without webapp", but we don't yet.                                     // 71
    // #MeteorServerNull                                                                                // 72
    if (name && self._connection === Meteor.server &&                                                   // 73
        typeof MongoInternals !== "undefined" &&                                                        // 74
        MongoInternals.defaultRemoteCollectionDriver) {                                                 // 75
      options._driver = MongoInternals.defaultRemoteCollectionDriver();                                 // 76
    } else {                                                                                            // 77
      options._driver = LocalCollectionDriver;                                                          // 78
    }                                                                                                   // 79
  }                                                                                                     // 80
                                                                                                        // 81
  self._collection = options._driver.open(name, self._connection);                                      // 82
  self._name = name;                                                                                    // 83
                                                                                                        // 84
  if (self._connection && self._connection.registerStore) {                                             // 85
    // OK, we're going to be a slave, replicating some remote                                           // 86
    // database, except possibly with some temporary divergence while                                   // 87
    // we have unacknowledged RPC's.                                                                    // 88
    var ok = self._connection.registerStore(name, {                                                     // 89
      // Called at the beginning of a batch of updates. batchSize is the number                         // 90
      // of update calls to expect.                                                                     // 91
      //                                                                                                // 92
      // XXX This interface is pretty janky. reset probably ought to go back to                         // 93
      // being its own function, and callers shouldn't have to calculate                                // 94
      // batchSize. The optimization of not calling pause/remove should be                              // 95
      // delayed until later: the first call to update() should buffer its                              // 96
      // message, and then we can either directly apply it at endUpdate time if                         // 97
      // it was the only update, or do pauseObservers/apply/apply at the next                           // 98
      // update() if there's another one.                                                               // 99
      beginUpdate: function (batchSize, reset) {                                                        // 100
        // pause observers so users don't see flicker when updating several                             // 101
        // objects at once (including the post-reconnect reset-and-reapply                              // 102
        // stage), and so that a re-sorting of a query can take advantage of the                        // 103
        // full _diffQuery moved calculation instead of applying change one at a                        // 104
        // time.                                                                                        // 105
        if (batchSize > 1 || reset)                                                                     // 106
          self._collection.pauseObservers();                                                            // 107
                                                                                                        // 108
        if (reset)                                                                                      // 109
          self._collection.remove({});                                                                  // 110
      },                                                                                                // 111
                                                                                                        // 112
      // Apply an update.                                                                               // 113
      // XXX better specify this interface (not in terms of a wire message)?                            // 114
      update: function (msg) {                                                                          // 115
        var mongoId = LocalCollection._idParse(msg.id);                                                 // 116
        var doc = self._collection.findOne(mongoId);                                                    // 117
                                                                                                        // 118
        // Is this a "replace the whole doc" message coming from the quiescence                         // 119
        // of method writes to an object? (Note that 'undefined' is a valid                             // 120
        // value meaning "remove it".)                                                                  // 121
        if (msg.msg === 'replace') {                                                                    // 122
          var replace = msg.replace;                                                                    // 123
          if (!replace) {                                                                               // 124
            if (doc)                                                                                    // 125
              self._collection.remove(mongoId);                                                         // 126
          } else if (!doc) {                                                                            // 127
            self._collection.insert(replace);                                                           // 128
          } else {                                                                                      // 129
            // XXX check that replace has no $ ops                                                      // 130
            self._collection.update(mongoId, replace);                                                  // 131
          }                                                                                             // 132
          return;                                                                                       // 133
        } else if (msg.msg === 'added') {                                                               // 134
          if (doc) {                                                                                    // 135
            throw new Error("Expected not to find a document already present for an add");              // 136
          }                                                                                             // 137
          self._collection.insert(_.extend({_id: mongoId}, msg.fields));                                // 138
        } else if (msg.msg === 'removed') {                                                             // 139
          if (!doc)                                                                                     // 140
            throw new Error("Expected to find a document already present for removed");                 // 141
          self._collection.remove(mongoId);                                                             // 142
        } else if (msg.msg === 'changed') {                                                             // 143
          if (!doc)                                                                                     // 144
            throw new Error("Expected to find a document to change");                                   // 145
          if (!_.isEmpty(msg.fields)) {                                                                 // 146
            var modifier = {};                                                                          // 147
            _.each(msg.fields, function (value, key) {                                                  // 148
              if (value === undefined) {                                                                // 149
                if (!modifier.$unset)                                                                   // 150
                  modifier.$unset = {};                                                                 // 151
                modifier.$unset[key] = 1;                                                               // 152
              } else {                                                                                  // 153
                if (!modifier.$set)                                                                     // 154
                  modifier.$set = {};                                                                   // 155
                modifier.$set[key] = value;                                                             // 156
              }                                                                                         // 157
            });                                                                                         // 158
            self._collection.update(mongoId, modifier);                                                 // 159
          }                                                                                             // 160
        } else {                                                                                        // 161
          throw new Error("I don't know how to deal with this message");                                // 162
        }                                                                                               // 163
                                                                                                        // 164
      },                                                                                                // 165
                                                                                                        // 166
      // Called at the end of a batch of updates.                                                       // 167
      endUpdate: function () {                                                                          // 168
        self._collection.resumeObservers();                                                             // 169
      },                                                                                                // 170
                                                                                                        // 171
      // Called around method stub invocations to capture the original versions                         // 172
      // of modified documents.                                                                         // 173
      saveOriginals: function () {                                                                      // 174
        self._collection.saveOriginals();                                                               // 175
      },                                                                                                // 176
      retrieveOriginals: function () {                                                                  // 177
        return self._collection.retrieveOriginals();                                                    // 178
      }                                                                                                 // 179
    });                                                                                                 // 180
                                                                                                        // 181
    if (!ok)                                                                                            // 182
      throw new Error("There is already a collection named '" + name + "'");                            // 183
  }                                                                                                     // 184
                                                                                                        // 185
  self._defineMutationMethods();                                                                        // 186
                                                                                                        // 187
  // autopublish                                                                                        // 188
  if (Package.autopublish && !options._preventAutopublish && self._connection                           // 189
      && self._connection.publish) {                                                                    // 190
    self._connection.publish(null, function () {                                                        // 191
      return self.find();                                                                               // 192
    }, {is_auto: true});                                                                                // 193
  }                                                                                                     // 194
};                                                                                                      // 195
                                                                                                        // 196
///                                                                                                     // 197
/// Main collection API                                                                                 // 198
///                                                                                                     // 199
                                                                                                        // 200
                                                                                                        // 201
_.extend(Meteor.Collection.prototype, {                                                                 // 202
                                                                                                        // 203
  _getFindSelector: function (args) {                                                                   // 204
    if (args.length == 0)                                                                               // 205
      return {};                                                                                        // 206
    else                                                                                                // 207
      return args[0];                                                                                   // 208
  },                                                                                                    // 209
                                                                                                        // 210
  _getFindOptions: function (args) {                                                                    // 211
    var self = this;                                                                                    // 212
    if (args.length < 2) {                                                                              // 213
      return { transform: self._transform };                                                            // 214
    } else {                                                                                            // 215
      check(args[1], Match.Optional(Match.ObjectIncluding({                                             // 216
        fields: Match.Optional(Match.OneOf(Object, undefined)),                                         // 217
        sort: Match.Optional(Match.OneOf(Object, Array, undefined)),                                    // 218
        limit: Match.Optional(Match.OneOf(Number, undefined)),                                          // 219
        skip: Match.Optional(Match.OneOf(Number, undefined))                                            // 220
     })));                                                                                              // 221
                                                                                                        // 222
      return _.extend({                                                                                 // 223
        transform: self._transform                                                                      // 224
      }, args[1]);                                                                                      // 225
    }                                                                                                   // 226
  },                                                                                                    // 227
                                                                                                        // 228
  find: function (/* selector, options */) {                                                            // 229
    // Collection.find() (return all docs) behaves differently                                          // 230
    // from Collection.find(undefined) (return 0 docs).  so be                                          // 231
    // careful about the length of arguments.                                                           // 232
    var self = this;                                                                                    // 233
    var argArray = _.toArray(arguments);                                                                // 234
    return self._collection.find(self._getFindSelector(argArray),                                       // 235
                                 self._getFindOptions(argArray));                                       // 236
  },                                                                                                    // 237
                                                                                                        // 238
  findOne: function (/* selector, options */) {                                                         // 239
    var self = this;                                                                                    // 240
    var argArray = _.toArray(arguments);                                                                // 241
    return self._collection.findOne(self._getFindSelector(argArray),                                    // 242
                                    self._getFindOptions(argArray));                                    // 243
  }                                                                                                     // 244
                                                                                                        // 245
});                                                                                                     // 246
                                                                                                        // 247
Meteor.Collection._publishCursor = function (cursor, sub, collection) {                                 // 248
  var observeHandle = cursor.observeChanges({                                                           // 249
    added: function (id, fields) {                                                                      // 250
      sub.added(collection, id, fields);                                                                // 251
    },                                                                                                  // 252
    changed: function (id, fields) {                                                                    // 253
      sub.changed(collection, id, fields);                                                              // 254
    },                                                                                                  // 255
    removed: function (id) {                                                                            // 256
      sub.removed(collection, id);                                                                      // 257
    }                                                                                                   // 258
  });                                                                                                   // 259
                                                                                                        // 260
  // We don't call sub.ready() here: it gets called in livedata_server, after                           // 261
  // possibly calling _publishCursor on multiple returned cursors.                                      // 262
                                                                                                        // 263
  // register stop callback (expects lambda w/ no args).                                                // 264
  sub.onStop(function () {observeHandle.stop();});                                                      // 265
};                                                                                                      // 266
                                                                                                        // 267
// protect against dangerous selectors.  falsey and {_id: falsey} are both                              // 268
// likely programmer error, and not what you want, particularly for destructive                         // 269
// operations.  JS regexps don't serialize over DDP but can be trivially                                // 270
// replaced by $regex.                                                                                  // 271
Meteor.Collection._rewriteSelector = function (selector) {                                              // 272
  // shorthand -- scalars match _id                                                                     // 273
  if (LocalCollection._selectorIsId(selector))                                                          // 274
    selector = {_id: selector};                                                                         // 275
                                                                                                        // 276
  if (!selector || (('_id' in selector) && !selector._id))                                              // 277
    // can't match anything                                                                             // 278
    return {_id: Random.id()};                                                                          // 279
                                                                                                        // 280
  var ret = {};                                                                                         // 281
  _.each(selector, function (value, key) {                                                              // 282
    // Mongo supports both {field: /foo/} and {field: {$regex: /foo/}}                                  // 283
    if (value instanceof RegExp) {                                                                      // 284
      ret[key] = convertRegexpToMongoSelector(value);                                                   // 285
    } else if (value && value.$regex instanceof RegExp) {                                               // 286
      ret[key] = convertRegexpToMongoSelector(value.$regex);                                            // 287
      // if value is {$regex: /foo/, $options: ...} then $options                                       // 288
      // override the ones set on $regex.                                                               // 289
      if (value.$options !== undefined)                                                                 // 290
        ret[key].$options = value.$options;                                                             // 291
    }                                                                                                   // 292
    else if (_.contains(['$or','$and','$nor'], key)) {                                                  // 293
      // Translate lower levels of $and/$or/$nor                                                        // 294
      ret[key] = _.map(value, function (v) {                                                            // 295
        return Meteor.Collection._rewriteSelector(v);                                                   // 296
      });                                                                                               // 297
    } else {                                                                                            // 298
      ret[key] = value;                                                                                 // 299
    }                                                                                                   // 300
  });                                                                                                   // 301
  return ret;                                                                                           // 302
};                                                                                                      // 303
                                                                                                        // 304
// convert a JS RegExp object to a Mongo {$regex: ..., $options: ...}                                   // 305
// selector                                                                                             // 306
var convertRegexpToMongoSelector = function (regexp) {                                                  // 307
  check(regexp, RegExp); // safety belt                                                                 // 308
                                                                                                        // 309
  var selector = {$regex: regexp.source};                                                               // 310
  var regexOptions = '';                                                                                // 311
  // JS RegExp objects support 'i', 'm', and 'g'. Mongo regex $options                                  // 312
  // support 'i', 'm', 'x', and 's'. So we support 'i' and 'm' here.                                    // 313
  if (regexp.ignoreCase)                                                                                // 314
    regexOptions += 'i';                                                                                // 315
  if (regexp.multiline)                                                                                 // 316
    regexOptions += 'm';                                                                                // 317
  if (regexOptions)                                                                                     // 318
    selector.$options = regexOptions;                                                                   // 319
                                                                                                        // 320
  return selector;                                                                                      // 321
};                                                                                                      // 322
                                                                                                        // 323
var throwIfSelectorIsNotId = function (selector, methodName) {                                          // 324
  if (!LocalCollection._selectorIsIdPerhapsAsObject(selector)) {                                        // 325
    throw new Meteor.Error(                                                                             // 326
      403, "Not permitted. Untrusted code may only " + methodName +                                     // 327
        " documents by ID.");                                                                           // 328
  }                                                                                                     // 329
};                                                                                                      // 330
                                                                                                        // 331
// 'insert' immediately returns the inserted document's new _id.                                        // 332
// The others return values immediately if you are in a stub, an in-memory                              // 333
// unmanaged collection, or a mongo-backed collection and you don't pass a                              // 334
// callback. 'update' and 'remove' return the number of affected                                        // 335
// documents. 'upsert' returns an object with keys 'numberAffected' and, if an                          // 336
// insert happened, 'insertedId'.                                                                       // 337
//                                                                                                      // 338
// Otherwise, the semantics are exactly like other methods: they take                                   // 339
// a callback as an optional last argument; if no callback is                                           // 340
// provided, they block until the operation is complete, and throw an                                   // 341
// exception if it fails; if a callback is provided, then they don't                                    // 342
// necessarily block, and they call the callback when they finish with error and                        // 343
// result arguments.  (The insert method provides the document ID as its result;                        // 344
// update and remove provide the number of affected docs as the result; upsert                          // 345
// provides an object with numberAffected and maybe insertedId.)                                        // 346
//                                                                                                      // 347
// On the client, blocking is impossible, so if a callback                                              // 348
// isn't provided, they just return immediately and any error                                           // 349
// information is lost.                                                                                 // 350
//                                                                                                      // 351
// There's one more tweak. On the client, if you don't provide a                                        // 352
// callback, then if there is an error, a message will be logged with                                   // 353
// Meteor._debug.                                                                                       // 354
//                                                                                                      // 355
// The intent (though this is actually determined by the underlying                                     // 356
// drivers) is that the operations should be done synchronously, not                                    // 357
// generating their result until the database has acknowledged                                          // 358
// them. In the future maybe we should provide a flag to turn this                                      // 359
// off.                                                                                                 // 360
_.each(["insert", "update", "remove"], function (name) {                                                // 361
  Meteor.Collection.prototype[name] = function (/* arguments */) {                                      // 362
    var self = this;                                                                                    // 363
    var args = _.toArray(arguments);                                                                    // 364
    var callback;                                                                                       // 365
    var insertId;                                                                                       // 366
    var ret;                                                                                            // 367
                                                                                                        // 368
    // Pull off any callback (or perhaps a 'callback' variable that was passed                          // 369
    // in undefined, like how 'upsert' does it).                                                        // 370
    if (args.length &&                                                                                  // 371
        (args[args.length - 1] === undefined ||                                                         // 372
         args[args.length - 1] instanceof Function)) {                                                  // 373
      callback = args.pop();                                                                            // 374
    }                                                                                                   // 375
                                                                                                        // 376
    if (name === "insert") {                                                                            // 377
      if (!args.length)                                                                                 // 378
        throw new Error("insert requires an argument");                                                 // 379
      // shallow-copy the document and generate an ID                                                   // 380
      args[0] = _.extend({}, args[0]);                                                                  // 381
      if ('_id' in args[0]) {                                                                           // 382
        insertId = args[0]._id;                                                                         // 383
        if (!insertId || !(typeof insertId === 'string'                                                 // 384
              || insertId instanceof Meteor.Collection.ObjectID))                                       // 385
          throw new Error("Meteor requires document _id fields to be non-empty strings or ObjectIDs");  // 386
      } else {                                                                                          // 387
        var generateId = true;                                                                          // 388
        // Don't generate the id if we're the client and the 'outermost' call                           // 389
        // This optimization saves us passing both the randomSeed and the id                            // 390
        // Passing both is redundant.                                                                   // 391
        if (self._connection && self._connection !== Meteor.server) {                                   // 392
          var enclosing = DDP._CurrentInvocation.get();                                                 // 393
          if (!enclosing) {                                                                             // 394
            generateId = false;                                                                         // 395
          }                                                                                             // 396
        }                                                                                               // 397
        if (generateId) {                                                                               // 398
          insertId = args[0]._id = self._makeNewID();                                                   // 399
        }                                                                                               // 400
      }                                                                                                 // 401
    } else {                                                                                            // 402
      args[0] = Meteor.Collection._rewriteSelector(args[0]);                                            // 403
                                                                                                        // 404
      if (name === "update") {                                                                          // 405
        // Mutate args but copy the original options object. We need to add                             // 406
        // insertedId to options, but don't want to mutate the caller's options                         // 407
        // object. We need to mutate `args` because we pass `args` into the                             // 408
        // driver below.                                                                                // 409
        var options = args[2] = _.clone(args[2]) || {};                                                 // 410
        if (options && typeof options !== "function" && options.upsert) {                               // 411
          // set `insertedId` if absent.  `insertedId` is a Meteor extension.                           // 412
          if (options.insertedId) {                                                                     // 413
            if (!(typeof options.insertedId === 'string'                                                // 414
                  || options.insertedId instanceof Meteor.Collection.ObjectID))                         // 415
              throw new Error("insertedId must be string or ObjectID");                                 // 416
          } else {                                                                                      // 417
            options.insertedId = self._makeNewID();                                                     // 418
          }                                                                                             // 419
        }                                                                                               // 420
      }                                                                                                 // 421
    }                                                                                                   // 422
                                                                                                        // 423
    // On inserts, always return the id that we generated; on all other                                 // 424
    // operations, just return the result from the collection.                                          // 425
    var chooseReturnValueFromCollectionResult = function (result) {                                     // 426
      if (name === "insert") {                                                                          // 427
        if (!insertId && result) {                                                                      // 428
          insertId = result;                                                                            // 429
        }                                                                                               // 430
        return insertId;                                                                                // 431
      } else {                                                                                          // 432
        return result;                                                                                  // 433
      }                                                                                                 // 434
    };                                                                                                  // 435
                                                                                                        // 436
    var wrappedCallback;                                                                                // 437
    if (callback) {                                                                                     // 438
      wrappedCallback = function (error, result) {                                                      // 439
        callback(error, ! error && chooseReturnValueFromCollectionResult(result));                      // 440
      };                                                                                                // 441
    }                                                                                                   // 442
                                                                                                        // 443
    // XXX see #MeteorServerNull                                                                        // 444
    if (self._connection && self._connection !== Meteor.server) {                                       // 445
      // just remote to another endpoint, propagate return value or                                     // 446
      // exception.                                                                                     // 447
                                                                                                        // 448
      var enclosing = DDP._CurrentInvocation.get();                                                     // 449
      var alreadyInSimulation = enclosing && enclosing.isSimulation;                                    // 450
                                                                                                        // 451
      if (Meteor.isClient && !wrappedCallback && ! alreadyInSimulation) {                               // 452
        // Client can't block, so it can't report errors by exception,                                  // 453
        // only by callback. If they forget the callback, give them a                                   // 454
        // default one that logs the error, so they aren't totally                                      // 455
        // baffled if their writes don't work because their database is                                 // 456
        // down.                                                                                        // 457
        // Don't give a default callback in simulation, because inside stubs we                         // 458
        // want to return the results from the local collection immediately and                         // 459
        // not force a callback.                                                                        // 460
        wrappedCallback = function (err) {                                                              // 461
          if (err)                                                                                      // 462
            Meteor._debug(name + " failed: " + (err.reason || err.stack));                              // 463
        };                                                                                              // 464
      }                                                                                                 // 465
                                                                                                        // 466
      if (!alreadyInSimulation && name !== "insert") {                                                  // 467
        // If we're about to actually send an RPC, we should throw an error if                          // 468
        // this is a non-ID selector, because the mutation methods only allow                           // 469
        // single-ID selectors. (If we don't throw here, we'll see flicker.)                            // 470
        throwIfSelectorIsNotId(args[0], name);                                                          // 471
      }                                                                                                 // 472
                                                                                                        // 473
      ret = chooseReturnValueFromCollectionResult(                                                      // 474
        self._connection.apply(self._prefix + name, args, {returnStubValue: true}, wrappedCallback)     // 475
      );                                                                                                // 476
                                                                                                        // 477
    } else {                                                                                            // 478
      // it's my collection.  descend into the collection object                                        // 479
      // and propagate any exception.                                                                   // 480
      args.push(wrappedCallback);                                                                       // 481
      try {                                                                                             // 482
        // If the user provided a callback and the collection implements this                           // 483
        // operation asynchronously, then queryRet will be undefined, and the                           // 484
        // result will be returned through the callback instead.                                        // 485
        var queryRet = self._collection[name].apply(self._collection, args);                            // 486
        ret = chooseReturnValueFromCollectionResult(queryRet);                                          // 487
      } catch (e) {                                                                                     // 488
        if (callback) {                                                                                 // 489
          callback(e);                                                                                  // 490
          return null;                                                                                  // 491
        }                                                                                               // 492
        throw e;                                                                                        // 493
      }                                                                                                 // 494
    }                                                                                                   // 495
                                                                                                        // 496
    // both sync and async, unless we threw an exception, return ret                                    // 497
    // (new document ID for insert, num affected for update/remove, object with                         // 498
    // numberAffected and maybe insertedId for upsert).                                                 // 499
    return ret;                                                                                         // 500
  };                                                                                                    // 501
});                                                                                                     // 502
                                                                                                        // 503
Meteor.Collection.prototype.upsert = function (selector, modifier,                                      // 504
                                               options, callback) {                                     // 505
  var self = this;                                                                                      // 506
  if (! callback && typeof options === "function") {                                                    // 507
    callback = options;                                                                                 // 508
    options = {};                                                                                       // 509
  }                                                                                                     // 510
  return self.update(selector, modifier,                                                                // 511
              _.extend({}, options, { _returnObject: true, upsert: true }),                             // 512
              callback);                                                                                // 513
};                                                                                                      // 514
                                                                                                        // 515
// We'll actually design an index API later. For now, we just pass through to                           // 516
// Mongo's, but make it synchronous.                                                                    // 517
Meteor.Collection.prototype._ensureIndex = function (index, options) {                                  // 518
  var self = this;                                                                                      // 519
  if (!self._collection._ensureIndex)                                                                   // 520
    throw new Error("Can only call _ensureIndex on server collections");                                // 521
  self._collection._ensureIndex(index, options);                                                        // 522
};                                                                                                      // 523
Meteor.Collection.prototype._dropIndex = function (index) {                                             // 524
  var self = this;                                                                                      // 525
  if (!self._collection._dropIndex)                                                                     // 526
    throw new Error("Can only call _dropIndex on server collections");                                  // 527
  self._collection._dropIndex(index);                                                                   // 528
};                                                                                                      // 529
Meteor.Collection.prototype._dropCollection = function () {                                             // 530
  var self = this;                                                                                      // 531
  if (!self._collection.dropCollection)                                                                 // 532
    throw new Error("Can only call _dropCollection on server collections");                             // 533
  self._collection.dropCollection();                                                                    // 534
};                                                                                                      // 535
Meteor.Collection.prototype._createCappedCollection = function (byteSize) {                             // 536
  var self = this;                                                                                      // 537
  if (!self._collection._createCappedCollection)                                                        // 538
    throw new Error("Can only call _createCappedCollection on server collections");                     // 539
  self._collection._createCappedCollection(byteSize);                                                   // 540
};                                                                                                      // 541
                                                                                                        // 542
Meteor.Collection.ObjectID = LocalCollection._ObjectID;                                                 // 543
                                                                                                        // 544
///                                                                                                     // 545
/// Remote methods and access control.                                                                  // 546
///                                                                                                     // 547
                                                                                                        // 548
// Restrict default mutators on collection. allow() and deny() take the                                 // 549
// same options:                                                                                        // 550
//                                                                                                      // 551
// options.insert {Function(userId, doc)}                                                               // 552
//   return true to allow/deny adding this document                                                     // 553
//                                                                                                      // 554
// options.update {Function(userId, docs, fields, modifier)}                                            // 555
//   return true to allow/deny updating these documents.                                                // 556
//   `fields` is passed as an array of fields that are to be modified                                   // 557
//                                                                                                      // 558
// options.remove {Function(userId, docs)}                                                              // 559
//   return true to allow/deny removing these documents                                                 // 560
//                                                                                                      // 561
// options.fetch {Array}                                                                                // 562
//   Fields to fetch for these validators. If any call to allow or deny                                 // 563
//   does not have this option then all fields are loaded.                                              // 564
//                                                                                                      // 565
// allow and deny can be called multiple times. The validators are                                      // 566
// evaluated as follows:                                                                                // 567
// - If neither deny() nor allow() has been called on the collection,                                   // 568
//   then the request is allowed if and only if the "insecure" smart                                    // 569
//   package is in use.                                                                                 // 570
// - Otherwise, if any deny() function returns true, the request is denied.                             // 571
// - Otherwise, if any allow() function returns true, the request is allowed.                           // 572
// - Otherwise, the request is denied.                                                                  // 573
//                                                                                                      // 574
// Meteor may call your deny() and allow() functions in any order, and may not                          // 575
// call all of them if it is able to make a decision without calling them all                           // 576
// (so don't include side effects).                                                                     // 577
                                                                                                        // 578
(function () {                                                                                          // 579
  var addValidator = function(allowOrDeny, options) {                                                   // 580
    // validate keys                                                                                    // 581
    var VALID_KEYS = ['insert', 'update', 'remove', 'fetch', 'transform'];                              // 582
    _.each(_.keys(options), function (key) {                                                            // 583
      if (!_.contains(VALID_KEYS, key))                                                                 // 584
        throw new Error(allowOrDeny + ": Invalid key: " + key);                                         // 585
    });                                                                                                 // 586
                                                                                                        // 587
    var self = this;                                                                                    // 588
    self._restricted = true;                                                                            // 589
                                                                                                        // 590
    _.each(['insert', 'update', 'remove'], function (name) {                                            // 591
      if (options[name]) {                                                                              // 592
        if (!(options[name] instanceof Function)) {                                                     // 593
          throw new Error(allowOrDeny + ": Value for `" + name + "` must be a function");               // 594
        }                                                                                               // 595
                                                                                                        // 596
        // If the transform is specified at all (including as 'null') in this                           // 597
        // call, then take that; otherwise, take the transform from the                                 // 598
        // collection.                                                                                  // 599
        if (options.transform === undefined) {                                                          // 600
          options[name].transform = self._transform;  // already wrapped                                // 601
        } else {                                                                                        // 602
          options[name].transform = LocalCollection.wrapTransform(                                      // 603
            options.transform);                                                                         // 604
        }                                                                                               // 605
                                                                                                        // 606
        self._validators[name][allowOrDeny].push(options[name]);                                        // 607
      }                                                                                                 // 608
    });                                                                                                 // 609
                                                                                                        // 610
    // Only update the fetch fields if we're passed things that affect                                  // 611
    // fetching. This way allow({}) and allow({insert: f}) don't result in                              // 612
    // setting fetchAllFields                                                                           // 613
    if (options.update || options.remove || options.fetch) {                                            // 614
      if (options.fetch && !(options.fetch instanceof Array)) {                                         // 615
        throw new Error(allowOrDeny + ": Value for `fetch` must be an array");                          // 616
      }                                                                                                 // 617
      self._updateFetch(options.fetch);                                                                 // 618
    }                                                                                                   // 619
  };                                                                                                    // 620
                                                                                                        // 621
  Meteor.Collection.prototype.allow = function(options) {                                               // 622
    addValidator.call(this, 'allow', options);                                                          // 623
  };                                                                                                    // 624
  Meteor.Collection.prototype.deny = function(options) {                                                // 625
    addValidator.call(this, 'deny', options);                                                           // 626
  };                                                                                                    // 627
})();                                                                                                   // 628
                                                                                                        // 629
                                                                                                        // 630
Meteor.Collection.prototype._defineMutationMethods = function() {                                       // 631
  var self = this;                                                                                      // 632
                                                                                                        // 633
  // set to true once we call any allow or deny methods. If true, use                                   // 634
  // allow/deny semantics. If false, use insecure mode semantics.                                       // 635
  self._restricted = false;                                                                             // 636
                                                                                                        // 637
  // Insecure mode (default to allowing writes). Defaults to 'undefined' which                          // 638
  // means insecure iff the insecure package is loaded. This property can be                            // 639
  // overriden by tests or packages wishing to change insecure mode behavior of                         // 640
  // their collections.                                                                                 // 641
  self._insecure = undefined;                                                                           // 642
                                                                                                        // 643
  self._validators = {                                                                                  // 644
    insert: {allow: [], deny: []},                                                                      // 645
    update: {allow: [], deny: []},                                                                      // 646
    remove: {allow: [], deny: []},                                                                      // 647
    upsert: {allow: [], deny: []}, // dummy arrays; can't set these!                                    // 648
    fetch: [],                                                                                          // 649
    fetchAllFields: false                                                                               // 650
  };                                                                                                    // 651
                                                                                                        // 652
  if (!self._name)                                                                                      // 653
    return; // anonymous collection                                                                     // 654
                                                                                                        // 655
  // XXX Think about method namespacing. Maybe methods should be                                        // 656
  // "Meteor:Mongo:insert/NAME"?                                                                        // 657
  self._prefix = '/' + self._name + '/';                                                                // 658
                                                                                                        // 659
  // mutation methods                                                                                   // 660
  if (self._connection) {                                                                               // 661
    var m = {};                                                                                         // 662
                                                                                                        // 663
    _.each(['insert', 'update', 'remove'], function (method) {                                          // 664
      m[self._prefix + method] = function (/* ... */) {                                                 // 665
        // All the methods do their own validation, instead of using check().                           // 666
        check(arguments, [Match.Any]);                                                                  // 667
        var args = _.toArray(arguments);                                                                // 668
        try {                                                                                           // 669
          // For an insert, if the client didn't specify an _id, generate one                           // 670
          // now; because this uses DDP.randomStream, it will be consistent with                        // 671
          // what the client generated. We generate it now rather than later so                         // 672
          // that if (eg) an allow/deny rule does an insert to the same                                 // 673
          // collection (not that it really should), the generated _id will                             // 674
          // still be the first use of the stream and will be consistent.                               // 675
          //                                                                                            // 676
          // However, we don't actually stick the _id onto the document yet,                            // 677
          // because we want allow/deny rules to be able to differentiate                               // 678
          // between arbitrary client-specified _id fields and merely                                   // 679
          // client-controlled-via-randomSeed fields.                                                   // 680
          var generatedId = null;                                                                       // 681
          if (method === "insert" && !_.has(args[0], '_id')) {                                          // 682
            generatedId = self._makeNewID();                                                            // 683
          }                                                                                             // 684
                                                                                                        // 685
          if (this.isSimulation) {                                                                      // 686
            // In a client simulation, you can do any mutation (even with a                             // 687
            // complex selector).                                                                       // 688
            if (generatedId !== null)                                                                   // 689
              args[0]._id = generatedId;                                                                // 690
            return self._collection[method].apply(                                                      // 691
              self._collection, args);                                                                  // 692
          }                                                                                             // 693
                                                                                                        // 694
          // This is the server receiving a method call from the client.                                // 695
                                                                                                        // 696
          // We don't allow arbitrary selectors in mutations from the client: only                      // 697
          // single-ID selectors.                                                                       // 698
          if (method !== 'insert')                                                                      // 699
            throwIfSelectorIsNotId(args[0], method);                                                    // 700
                                                                                                        // 701
          if (self._restricted) {                                                                       // 702
            // short circuit if there is no way it will pass.                                           // 703
            if (self._validators[method].allow.length === 0) {                                          // 704
              throw new Meteor.Error(                                                                   // 705
                403, "Access denied. No allow validators set on restricted " +                          // 706
                  "collection for method '" + method + "'.");                                           // 707
            }                                                                                           // 708
                                                                                                        // 709
            var validatedMethodName =                                                                   // 710
                  '_validated' + method.charAt(0).toUpperCase() + method.slice(1);                      // 711
            args.unshift(this.userId);                                                                  // 712
            method === 'insert' && args.push(generatedId);                                              // 713
            return self[validatedMethodName].apply(self, args);                                         // 714
          } else if (self._isInsecure()) {                                                              // 715
            if (generatedId !== null)                                                                   // 716
              args[0]._id = generatedId;                                                                // 717
            // In insecure mode, allow any mutation (with a simple selector).                           // 718
            // XXX This is kind of bogus.  Instead of blindly passing whatever                          // 719
            //     we get from the network to this function, we should actually                         // 720
            //     know the correct arguments for the function and pass just                            // 721
            //     them.  For example, if you have an extraneous extra null                             // 722
            //     argument and this is Mongo on the server, the _wrapAsync'd                           // 723
            //     functions like update will get confused and pass the                                 // 724
            //     "fut.resolver()" in the wrong slot, where _update will never                         // 725
            //     invoke it. Bam, broken DDP connection.  Probably should just                         // 726
            //     take this whole method and write it three times, invoking                            // 727
            //     helpers for the common code.                                                         // 728
            return self._collection[method].apply(self._collection, args);                              // 729
          } else {                                                                                      // 730
            // In secure mode, if we haven't called allow or deny, then nothing                         // 731
            // is permitted.                                                                            // 732
            throw new Meteor.Error(403, "Access denied");                                               // 733
          }                                                                                             // 734
        } catch (e) {                                                                                   // 735
          if (e.name === 'MongoError' || e.name === 'MinimongoError') {                                 // 736
            throw new Meteor.Error(409, e.toString());                                                  // 737
          } else {                                                                                      // 738
            throw e;                                                                                    // 739
          }                                                                                             // 740
        }                                                                                               // 741
      };                                                                                                // 742
    });                                                                                                 // 743
    // Minimongo on the server gets no stubs; instead, by default                                       // 744
    // it wait()s until its result is ready, yielding.                                                  // 745
    // This matches the behavior of macromongo on the server better.                                    // 746
    // XXX see #MeteorServerNull                                                                        // 747
    if (Meteor.isClient || self._connection === Meteor.server)                                          // 748
      self._connection.methods(m);                                                                      // 749
  }                                                                                                     // 750
};                                                                                                      // 751
                                                                                                        // 752
                                                                                                        // 753
Meteor.Collection.prototype._updateFetch = function (fields) {                                          // 754
  var self = this;                                                                                      // 755
                                                                                                        // 756
  if (!self._validators.fetchAllFields) {                                                               // 757
    if (fields) {                                                                                       // 758
      self._validators.fetch = _.union(self._validators.fetch, fields);                                 // 759
    } else {                                                                                            // 760
      self._validators.fetchAllFields = true;                                                           // 761
      // clear fetch just to make sure we don't accidentally read it                                    // 762
      self._validators.fetch = null;                                                                    // 763
    }                                                                                                   // 764
  }                                                                                                     // 765
};                                                                                                      // 766
                                                                                                        // 767
Meteor.Collection.prototype._isInsecure = function () {                                                 // 768
  var self = this;                                                                                      // 769
  if (self._insecure === undefined)                                                                     // 770
    return !!Package.insecure;                                                                          // 771
  return self._insecure;                                                                                // 772
};                                                                                                      // 773
                                                                                                        // 774
var docToValidate = function (validator, doc, generatedId) {                                            // 775
  var ret = doc;                                                                                        // 776
  if (validator.transform) {                                                                            // 777
    ret = EJSON.clone(doc);                                                                             // 778
    // If you set a server-side transform on your collection, then you don't get                        // 779
    // to tell the difference between "client specified the ID" and "server                             // 780
    // generated the ID", because transforms expect to get _id.  If you want to                         // 781
    // do that check, you can do it with a specific                                                     // 782
    // `C.allow({insert: f, transform: null})` validator.                                               // 783
    if (generatedId !== null) {                                                                         // 784
      ret._id = generatedId;                                                                            // 785
    }                                                                                                   // 786
    ret = validator.transform(ret);                                                                     // 787
  }                                                                                                     // 788
  return ret;                                                                                           // 789
};                                                                                                      // 790
                                                                                                        // 791
Meteor.Collection.prototype._validatedInsert = function (userId, doc,                                   // 792
                                                         generatedId) {                                 // 793
  var self = this;                                                                                      // 794
                                                                                                        // 795
  // call user validators.                                                                              // 796
  // Any deny returns true means denied.                                                                // 797
  if (_.any(self._validators.insert.deny, function(validator) {                                         // 798
    return validator(userId, docToValidate(validator, doc, generatedId));                               // 799
  })) {                                                                                                 // 800
    throw new Meteor.Error(403, "Access denied");                                                       // 801
  }                                                                                                     // 802
  // Any allow returns true means proceed. Throw error if they all fail.                                // 803
  if (_.all(self._validators.insert.allow, function(validator) {                                        // 804
    return !validator(userId, docToValidate(validator, doc, generatedId));                              // 805
  })) {                                                                                                 // 806
    throw new Meteor.Error(403, "Access denied");                                                       // 807
  }                                                                                                     // 808
                                                                                                        // 809
  // If we generated an ID above, insert it now: after the validation, but                              // 810
  // before actually inserting.                                                                         // 811
  if (generatedId !== null)                                                                             // 812
    doc._id = generatedId;                                                                              // 813
                                                                                                        // 814
  self._collection.insert.call(self._collection, doc);                                                  // 815
};                                                                                                      // 816
                                                                                                        // 817
var transformDoc = function (validator, doc) {                                                          // 818
  if (validator.transform)                                                                              // 819
    return validator.transform(doc);                                                                    // 820
  return doc;                                                                                           // 821
};                                                                                                      // 822
                                                                                                        // 823
// Simulate a mongo `update` operation while validating that the access                                 // 824
// control rules set by calls to `allow/deny` are satisfied. If all                                     // 825
// pass, rewrite the mongo operation to use $in to set the list of                                      // 826
// document ids to change ##ValidatedChange                                                             // 827
Meteor.Collection.prototype._validatedUpdate = function(                                                // 828
    userId, selector, mutator, options) {                                                               // 829
  var self = this;                                                                                      // 830
                                                                                                        // 831
  options = options || {};                                                                              // 832
                                                                                                        // 833
  if (!LocalCollection._selectorIsIdPerhapsAsObject(selector))                                          // 834
    throw new Error("validated update should be of a single ID");                                       // 835
                                                                                                        // 836
  // We don't support upserts because they don't fit nicely into allow/deny                             // 837
  // rules.                                                                                             // 838
  if (options.upsert)                                                                                   // 839
    throw new Meteor.Error(403, "Access denied. Upserts not " +                                         // 840
                           "allowed in a restricted collection.");                                      // 841
                                                                                                        // 842
  // compute modified fields                                                                            // 843
  var fields = [];                                                                                      // 844
  _.each(mutator, function (params, op) {                                                               // 845
    if (op.charAt(0) !== '$') {                                                                         // 846
      throw new Meteor.Error(                                                                           // 847
        403, "Access denied. In a restricted collection you can only update documents, not replace them. Use a Mongo update operator, such as '$set'.");
    } else if (!_.has(ALLOWED_UPDATE_OPERATIONS, op)) {                                                 // 849
      throw new Meteor.Error(                                                                           // 850
        403, "Access denied. Operator " + op + " not allowed in a restricted collection.");             // 851
    } else {                                                                                            // 852
      _.each(_.keys(params), function (field) {                                                         // 853
        // treat dotted fields as if they are replacing their                                           // 854
        // top-level part                                                                               // 855
        if (field.indexOf('.') !== -1)                                                                  // 856
          field = field.substring(0, field.indexOf('.'));                                               // 857
                                                                                                        // 858
        // record the field we are trying to change                                                     // 859
        if (!_.contains(fields, field))                                                                 // 860
          fields.push(field);                                                                           // 861
      });                                                                                               // 862
    }                                                                                                   // 863
  });                                                                                                   // 864
                                                                                                        // 865
  var findOptions = {transform: null};                                                                  // 866
  if (!self._validators.fetchAllFields) {                                                               // 867
    findOptions.fields = {};                                                                            // 868
    _.each(self._validators.fetch, function(fieldName) {                                                // 869
      findOptions.fields[fieldName] = 1;                                                                // 870
    });                                                                                                 // 871
  }                                                                                                     // 872
                                                                                                        // 873
  var doc = self._collection.findOne(selector, findOptions);                                            // 874
  if (!doc)  // none satisfied!                                                                         // 875
    return 0;                                                                                           // 876
                                                                                                        // 877
  var factoriedDoc;                                                                                     // 878
                                                                                                        // 879
  // call user validators.                                                                              // 880
  // Any deny returns true means denied.                                                                // 881
  if (_.any(self._validators.update.deny, function(validator) {                                         // 882
    if (!factoriedDoc)                                                                                  // 883
      factoriedDoc = transformDoc(validator, doc);                                                      // 884
    return validator(userId,                                                                            // 885
                     factoriedDoc,                                                                      // 886
                     fields,                                                                            // 887
                     mutator);                                                                          // 888
  })) {                                                                                                 // 889
    throw new Meteor.Error(403, "Access denied");                                                       // 890
  }                                                                                                     // 891
  // Any allow returns true means proceed. Throw error if they all fail.                                // 892
  if (_.all(self._validators.update.allow, function(validator) {                                        // 893
    if (!factoriedDoc)                                                                                  // 894
      factoriedDoc = transformDoc(validator, doc);                                                      // 895
    return !validator(userId,                                                                           // 896
                      factoriedDoc,                                                                     // 897
                      fields,                                                                           // 898
                      mutator);                                                                         // 899
  })) {                                                                                                 // 900
    throw new Meteor.Error(403, "Access denied");                                                       // 901
  }                                                                                                     // 902
                                                                                                        // 903
  // Back when we supported arbitrary client-provided selectors, we actually                            // 904
  // rewrote the selector to include an _id clause before passing to Mongo to                           // 905
  // avoid races, but since selector is guaranteed to already just be an ID, we                         // 906
  // don't have to any more.                                                                            // 907
                                                                                                        // 908
  return self._collection.update.call(                                                                  // 909
    self._collection, selector, mutator, options);                                                      // 910
};                                                                                                      // 911
                                                                                                        // 912
// Only allow these operations in validated updates. Specifically                                       // 913
// whitelist operations, rather than blacklist, so new complex                                          // 914
// operations that are added aren't automatically allowed. A complex                                    // 915
// operation is one that does more than just modify its target                                          // 916
// field. For now this contains all update operations except '$rename'.                                 // 917
// http://docs.mongodb.org/manual/reference/operators/#update                                           // 918
var ALLOWED_UPDATE_OPERATIONS = {                                                                       // 919
  $inc:1, $set:1, $unset:1, $addToSet:1, $pop:1, $pullAll:1, $pull:1,                                   // 920
  $pushAll:1, $push:1, $bit:1                                                                           // 921
};                                                                                                      // 922
                                                                                                        // 923
// Simulate a mongo `remove` operation while validating access control                                  // 924
// rules. See #ValidatedChange                                                                          // 925
Meteor.Collection.prototype._validatedRemove = function(userId, selector) {                             // 926
  var self = this;                                                                                      // 927
                                                                                                        // 928
  var findOptions = {transform: null};                                                                  // 929
  if (!self._validators.fetchAllFields) {                                                               // 930
    findOptions.fields = {};                                                                            // 931
    _.each(self._validators.fetch, function(fieldName) {                                                // 932
      findOptions.fields[fieldName] = 1;                                                                // 933
    });                                                                                                 // 934
  }                                                                                                     // 935
                                                                                                        // 936
  var doc = self._collection.findOne(selector, findOptions);                                            // 937
  if (!doc)                                                                                             // 938
    return 0;                                                                                           // 939
                                                                                                        // 940
  // call user validators.                                                                              // 941
  // Any deny returns true means denied.                                                                // 942
  if (_.any(self._validators.remove.deny, function(validator) {                                         // 943
    return validator(userId, transformDoc(validator, doc));                                             // 944
  })) {                                                                                                 // 945
    throw new Meteor.Error(403, "Access denied");                                                       // 946
  }                                                                                                     // 947
  // Any allow returns true means proceed. Throw error if they all fail.                                // 948
  if (_.all(self._validators.remove.allow, function(validator) {                                        // 949
    return !validator(userId, transformDoc(validator, doc));                                            // 950
  })) {                                                                                                 // 951
    throw new Meteor.Error(403, "Access denied");                                                       // 952
  }                                                                                                     // 953
                                                                                                        // 954
  // Back when we supported arbitrary client-provided selectors, we actually                            // 955
  // rewrote the selector to {_id: {$in: [ids that we found]}} before passing to                        // 956
  // Mongo to avoid races, but since selector is guaranteed to already just be                          // 957
  // an ID, we don't have to any more.                                                                  // 958
                                                                                                        // 959
  return self._collection.remove.call(self._collection, selector);                                      // 960
};                                                                                                      // 961
                                                                                                        // 962
//////////////////////////////////////////////////////////////////////////////////////////////////////////

}).call(this);


/* Exports */
if (typeof Package === 'undefined') Package = {};
Package['mongo-livedata'] = {
  MongoInternals: MongoInternals,
  MongoTest: MongoTest
};

})();

//# sourceMappingURL=mongo-livedata.js.map
