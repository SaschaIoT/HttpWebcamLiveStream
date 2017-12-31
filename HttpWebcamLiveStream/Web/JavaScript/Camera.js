var webSocketVideoFrame;
var frameTime;
var videoFrameElement = document.querySelector("#videoFrame");
var lastImageUrl;

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
        lastImageUrl = createObjectURL(blob);
        videoFrameElement.src = lastImageUrl;
        
        frameTime = new Date().getTime();
    };
}

videoFrameElement.addEventListener("load", function (e) {
    URL.revokeObjectURL(lastImageUrl);

    webSocketHelper.waitUntilWebsocketReady(function () {
        webSocketVideoFrame.send(JSON.stringify({ command: "VideoFrame" }));
    }, webSocketVideoFrame, 0);
});

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