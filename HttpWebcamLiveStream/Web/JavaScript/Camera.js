//Maximum frames of the video stream per second. The less frames the less network traffic.
var maximumVideoFramesPerSecond = 30;

var maximumVideoFramesPerSecondTimeout = 1000.0 / maximumVideoFramesPerSecond;
var getVideoFrameTimeout = 2000;
var lastVideoFrameTime = new Date();

function GetVideoFrame() {

    lastVideoFrameTime = new Date();

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "VideoFrame" + new Date().getTime().toString(), true);
    xhr.responseType = "arraybuffer";
    
    xhr.timeout = getVideoFrameTimeout;
    xhr.ontimeout = function () {
        GetVideoFrameAfterTimeout();
    }

    xhr.onerror = function () {
        GetVideoFrameAfterTimeout();
    }

    xhr.onload = function () {

        if (xhr.status === 200) {
            var blob = new Blob([xhr.response], { type: "image/jpeg" });
            var url = webkitURL.createObjectURL(blob);
            document.querySelector("#videoFrame").src = url;
        }

        GetVideoFrameAfterTimeout();
    }

    xhr.send();
}

function GetVideoFrameAfterTimeout() {
    var videoFrameTimeToLastFrame = new Date() - lastVideoFrameTime;

    if (videoFrameTimeToLastFrame >= maximumVideoFramesPerSecondTimeout) {
        setTimeout(function () { GetVideoFrame(); }, 0);

    } else {
        var nextVideoFrameTimeout = maximumVideoFramesPerSecondTimeout - videoFrameTimeToLastFrame;
        setTimeout(function () { GetVideoFrame(); }, nextVideoFrameTimeout);
    }
}

GetVideoFrame();