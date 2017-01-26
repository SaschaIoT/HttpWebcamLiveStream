//Maximum frames of the video stream per second. The less frames the less network traffic.
var maximumVideoFramesPerSecond = 30;

var maximumVideoFramesPerSecondTimeout = 1000.0 / maximumVideoFramesPerSecond;
var getVideoFrameTimeout = 2000;
var lastVideoFrameTime = new Date();

function GetVideoFrame() {

    lastVideoFrameTime = new Date();

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "VideoFrame" + new Date().getTime().toString() + ".html", true);
    xhr.responseType = "blob";

    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4) {
            if (xhr.status === 200) {
                var urlCreator = window.URL || window.webkitURL;
                var imageUrl = urlCreator.createObjectURL(xhr.response);
                document.querySelector("#videoFrame").src = imageUrl;
            }
        }
    }

    xhr.timeout = getVideoFrameTimeout;
    xhr.ontimeout = function () {
        GetVideoFrameAfterTimeout();
    }

    xhr.onerror = function () {
        GetVideoFrameAfterTimeout();
    }

    xhr.onload = function () {
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