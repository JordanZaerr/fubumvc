﻿'use strict';
/*globals console:true, ErrorCollector:true*/
if (typeof console === "undefined") {
    console = {
        log: function () { },
        debug: function () { },
        info: function () { },
        warn: function () { },
        error: function () { },
        trace: function () { }
    };
}

var SerenityErrorCollector = (function (global, console) {
    var self = {
        errors: [],
        registerError: function (msg) {
            self.errors.push(msg);
        }
    };

    var log = function (msg) {
        self.registerError(msg);
    };

    function decorateWithLog(callback) {
        return function (msg) {
            log(msg);
            if (callback) {
                global.onerror = log; //callback(msg);
            }
        };
    }

    global.onerror = decorateWithLog(global.onerror);

    console.error = decorateWithLog(console.error);
    console.warn = decorateWithLog(console.warn);

    return self;
})(window, console);