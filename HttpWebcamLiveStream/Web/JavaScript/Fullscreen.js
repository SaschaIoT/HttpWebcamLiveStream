var videoFrameElement = document.getElementById("videoFrame");
var bodyElement = document.getElementsByTagName("body")[0];

videoFrameElement.addEventListener("dblclick", function (e) {
    toggleFullScreen();
});

videoFrameElement.addEventListener("touchstart", function (e) {
    toggleFullScreen();
}, { passive: true });

function toggleFullScreen() {
    if (videoFrameElement.classList.contains("video-frame")) {
        videoFrameElement.classList.add("video-frame-full-screen");
        videoFrameElement.classList.remove("video-frame");
        bodyElement.classList.add("body-full-screen");

        if (showControls === true) {
            document.getElementById("video-settings").classList.add("video-settings-hide");
        }
    } else {
        videoFrameElement.classList.add("video-frame");
        videoFrameElement.classList.remove("video-frame-full-screen");
        bodyElement.classList.remove("body-full-screen");

        if (showControls === true) {
            document.getElementById("video-settings").classList.remove("video-settings-hide");
        }
    }
}