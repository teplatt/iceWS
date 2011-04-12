/**
* Copyright (c)2011 Tracy Platt (te_platt@yahoo.com)
* 
* Dual licensed under the MIT and GPL licenses. 
**/

function iceWS(uri) {
    if (!window.WebSocket) {
        alert("Your browser does not support webSockets");
    }
    this.iceF = function (functionName, returnFunction) {
        var wssDataSeparator = String.fromCharCode(4, 3);
        wssData = functionName + wssDataSeparator + returnFunction + wssDataSeparator;
        for (var i = 2; i < arguments.length; i++) {
            wssData += arguments[i];
            wssData += wssDataSeparator;
        }
        this.webSocket.send(wssData);
    }
    this.webSocket = new WebSocket(uri);
    this.webSocket.onopen = function (evt) { onIceOpen(evt) };
    this.webSocket.onclose = function (evt) { onIceClose(evt) };
    this.webSocket.onmessage = function (evt) { onIceMessage(evt) };
    this.webSocket.onerror = function (evt) { onIceError(evt) };
}

function onIceMessage(evt) {
    //vals[0] is the name of the function that was called that got us here
    //vals[1] is the name of the function to call
    //vals[2] and up are the strings to send as parameters to the new function
    var vals = evt.data.split(String.fromCharCode(4));
    var fn = new Function("term", "return " + vals[1] + "(term);");
    var params = [];
    params.push(vals[0]) //just in case we need to know something about where we came from
    for (var i = 2; i < vals.length; i++) {
        params.push(vals[i]);
    }
    fn(params);
}

function onIceOpen(evt) {
//add whatever logic for when the socket opens
}

function onIceClose(evt) {
    //add whatever logic for when the socket closes
}

function onIceError(evt) {
    //add whatever logic for when the socket has an error
}