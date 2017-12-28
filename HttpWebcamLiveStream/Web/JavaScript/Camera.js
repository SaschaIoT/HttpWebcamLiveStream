var webSocketVideoFrame;
var frameTime;

function GetVideoFrames() {

    webSocketVideoFrame = new WebSocket('ws://' + location.host + "/VideoFrame");
    webSocketVideoFrame.binaryType = "arraybuffer";

    webSocketVideoFrame.onopen = function () {
        webSocketHelper.waitUntilWebsocketReady(function () {
            webSocketVideoFrame.send(JSON.stringify({ command: "VideoFrame" }));
        }, webSocketVideoFrame, 0);
    };

    webSocketVideoFrame.onmessage = function () {

        var bytearray = new Uint8Array(event.data);

        var blob = new Blob([event.data], { type: "image/jpeg" });
        var url = createObjectURL(blob);
        document.querySelector("#videoFrame").src = url;

        webSocketHelper.waitUntilWebsocketReady(function () {
            webSocketVideoFrame.send(JSON.stringify({ command: "VideoFrame" }));
        }, webSocketVideoFrame, 0);

        frameTime = new Date().getTime();
    };
}

function createObjectURL(blob) {
    var URL = window.URL || window.webkitURL;
    if (URL && URL.createObjectURL) {
        return URL.createObjectURL(blob);
    } else {
        return null;
    }
}

function KeepAliveGetVideoFrames() {

    var duration = 0;
    if (frameTime !== undefined) {
        duration = new Date().getTime() - frameTime
    }

    if (frameTime !== undefined
        && duration <= 1000) {

        setTimeout(function () {
            KeepAliveGetVideoFrames();
        }, 100);
    } else {

        if (webSocketVideoFrame !== undefined) {
            try {
                webSocketVideoFrame.close();
            } catch (e) { }
        }

        GetVideoFrames();

        setTimeout(function () {
            KeepAliveGetVideoFrames();
        }, 4000);
    }
}

KeepAliveGetVideoFrames();