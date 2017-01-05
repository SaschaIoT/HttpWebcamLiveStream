//Maximum frames of the video stream per second. The less frames the less network traffic.
//(the current server side webcam stream delivers 30 frames, so I limit the frames to 30)
var maximumVideoFramesPerSecond = 30;

var xhttpRequestTimeout = 2000;
var videoFrameTimeout = 1000.0 / maximumVideoFramesPerSecond;
var lastVideoFrameTime = new Date();

function GetVideoFrame() {

    lastVideoFrameTime = new Date();

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "http://minwinpc/VideoFrame" + new Date().getTime().toString() + ".html", true);
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

    xhr.timeout = xhttpRequestTimeout;
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
    var lastVideoFrameTimeToLast = new Date() - lastVideoFrameTime;

    if (lastVideoFrameTimeToLast >= videoFrameTimeout) {
        setTimeout(function () { GetVideoFrame(); }, 0);

    } else {
        var nextVideoFrameTimeout = videoFrameTimeout - lastVideoFrameTimeToLast;
        setTimeout(function () { GetVideoFrame(); }, nextVideoFrameTimeout);
    }
}

GetVideoFrame();