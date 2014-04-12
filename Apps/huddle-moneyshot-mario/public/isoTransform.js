
  // ========================= getStyleProperty by kangax ===============================
  // http://perfectionkills.com/feature-testing-css-properties/

  var getStyleProperty = (function(){

    var prefixes = ['Moz', 'Webkit', 'Khtml', 'O', 'Ms'];
    var _cache = { };

    function getStyleProperty(propName, element) {
      element = element || document.documentElement;
      var style = element.style,
          prefixed,
          uPropName,
          i, l;

      // check cache only when no element is given
      if (arguments.length === 1 && typeof _cache[propName] === 'string') {
        return _cache[propName];
      }
      // test standard property first
      if (typeof style[propName] === 'string') {
        return (_cache[propName] = propName);
      }
    
      // capitalize
      uPropName = propName.charAt(0).toUpperCase() + propName.slice(1);

      // test vendor specific properties
      for (i=0, l=prefixes.length; i<l; i++) {
        prefixed = prefixes[i] + uPropName;
        if (typeof style[prefixed] === 'string') {
          return (_cache[propName] = prefixed);
        }
      }
    }

    return getStyleProperty;
  }());

  var transformProp = getStyleProperty('transform');

  // ========================= miniModernizr ===============================
  // <3<3<3 and thanks to Faruk and Paul for doing the heavy lifting

  /*!
   * Modernizr v1.6ish: miniModernizr for Isotope
   * http://www.modernizr.com
   *
   * Developed by: 
   * - Faruk Ates  http://farukat.es/
   * - Paul Irish  http://paulirish.com/
   *
   * Copyright (c) 2009-2010
   * Dual-licensed under the BSD or MIT licenses.
   * http://www.modernizr.com/license/
   */

 
  /*
   * This version whittles down the script just to check support for
   * CSS transitions, transforms, and 3D transforms.
  */
  
  var docElement = document.documentElement,
      vendorCSSPrefixes = ' -o- -moz- -ms- -webkit- -khtml- '.split(' '),
      tests = [
        {
          name : 'csstransforms',
          getResult : function() {
            return !!transformProp;
          }
        },
        {
          name : 'csstransforms3d',
          getResult : function() {
            var test = !!getStyleProperty('perspective');
            // double check for Chrome's false positive
            if ( test ){
              var st = document.createElement('style'),
                  div = document.createElement('div'),
                  mq = '@media (' + vendorCSSPrefixes.join('transform-3d),(') + 'modernizr)';

              st.textContent = mq + '{#modernizr{height:3px}}';
              (document.head || document.getElementsByTagName('head')[0]).appendChild(st);
              div.id = 'modernizr';
              docElement.appendChild(div);

              test = div.offsetHeight === 3;

              st.parentNode.removeChild(st);
              div.parentNode.removeChild(div);
            }
            return !!test;
          }
        },
        {
          name : 'csstransitions',
          getResult : function() {
            return !!getStyleProperty('transitionProperty');
          }
        }
      ],

      i, len = tests.length
  ;

  if ( window.Modernizr ) {
    // if there's a previous Modernzir, check if there are necessary tests
    for ( i=0; i < len; i++ ) {
      var test = tests[i];
      if ( !Modernizr.hasOwnProperty( test.name ) ) {
        // if test hasn't been run, use addTest to run it
        Modernizr.addTest( test.name, test.getResult );
      }
    }
  } else {
    // or create new mini Modernizr that just has the 3 tests
    window.Modernizr = (function(){

      var miniModernizr = {
            _version : '1.6ish: miniModernizr for Isotope'
          },
          classes = [],
          test, result, className;

      // Run through tests
      for ( i=0; i < len; i++ ) {
        test = tests[i];
        result = test.getResult();
        miniModernizr[ test.name ] = result;
        className = ( result ?  '' : 'no-' ) + test.name;
        classes.push( className );
      }

      // Add the new classes to the <html> element.
      docElement.className += ' ' + classes.join( ' ' );

      return miniModernizr;
    })();
  }



  // ========================= isoTransform ===============================

  /**
   *  provides hooks for .css({ scale: value, translate: [x, y] })
   *  Progressively enhanced CSS transforms
   *  Uses hardware accelerated 3D transforms for Safari
   *  or falls back to 2D transforms.
   */
  
  if ( Modernizr.csstransforms ) {
    
        // i.e. transformFnNotations.scale(0.5) >> 'scale3d( 0.5, 0.5, 1)'
    var transformFnNotations = Modernizr.csstransforms3d ? 
      { // 3D transform functions
        translate : function ( position ) {
          return 'translate3d(' + position[0] + 'px, ' + position[1] + 'px, 0) ';
        },
        scale : function ( scale ) {
          return 'scale3d(' + scale + ', ' + scale + ', 1) ';
        }
      } :
      { // 2D transform functions
        translate : function ( position ) {
          return 'translate(' + position[0] + 'px, ' + position[1] + 'px) ';
        },
        scale : function ( scale ) {
          return 'scale(' + scale + ') ';
        }
      }
    ;

    function setIsoTransform ( elem, name, value ) {
      var $elem = $(elem),
          // unpack current transform data
          data =  $elem.data('isoTransform') || {},
          newData = {},
          fnName,
          transformObj = {},
          transformValue;

      // i.e. newData.scale = 0.5
      newData[ name ] = value;
      // extend new value over current data
      $.extend( data, newData );

      for ( fnName in data ) {
        transformValue = data[ fnName ];
        transformObj[ fnName ] = transformFnNotations[ fnName ]( transformValue );
      }

      // get proper order
      // ideally, we could loop through this give an array, but since we only have
      // a couple transforms we're keeping track of, we'll do it like so
      var translateFn = transformObj.translate || '',
          scaleFn = transformObj.scale || '',
          // sorting so translate always comes first
          valueFns = translateFn + scaleFn;

      // set data back in elem
      $elem.data( 'isoTransform', data );

      // set name to vendor specific property
      elem.style[ transformProp ] = valueFns;
    }
   
    // ==================== scale ===================
  
    $.cssNumber.scale = true;
  
    $.cssHooks.scale = {
      set: function( elem, value ) {

        if ( typeof value === 'string' ) {
          value = parseFloat( value );
        }

        setIsoTransform( elem, 'scale', value );

      },
      get: function( elem, computed ) {
        var transform = $.data( elem, 'isoTransform' );
        return transform && transform.scale ? transform.scale : 1;
      }
    };

    $.fx.step.scale = function( fx ) {
      $.cssHooks.scale.set( fx.elem, fx.now+fx.unit );
    };
  
  
    // ==================== translate ===================
    
    $.cssNumber.translate = true;
  
    $.cssHooks.translate = {
      set: function( elem, value ) {

        // all this would be for public ease-of-use,
        // but we're leaving it out since this add-on is
        // only for internal-plugin use
        // if ( typeof value === 'string' ) {
        //   value = value.split(' ');
        // }
        // 
        //  
        // var i, val;
        // for ( i = 0; i < 2; i++ ) {
        //   val = value[i];
        //   if ( typeof val === 'string' ) {
        //     val = parseInt( val );
        //   }
        // }

        setIsoTransform( elem, 'translate', value );

      },
    
      get: function( elem, computed ) {
        var transform = $.data( elem, 'isoTransform' );
        return transform && transform.translate ? transform.translate : [ 0, 0 ];
      }
    };

  }