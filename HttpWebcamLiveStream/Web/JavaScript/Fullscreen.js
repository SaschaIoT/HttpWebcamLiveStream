var videoFrameContainerElement = document.getElementById("videoFrame");

videoFrameContainerElement.addEventListener("dblclick", function (e) {
    toggleFullScreen();
});

videoFrameContainerElement.addEventListener("touchstart", function (e) {
    toggleFullScreen();
});

function toggleFullScreen() {
    if (videoFrameContainerElement.classList.contains("video-frame")) {
        videoFrameContainerElement.classList.add("video-frame-full-screen");
        videoFrameContainerElement.classList.remove("video-frame");
    } else {
        videoFrameContainerElement.classList.add("video-frame");
        videoFrameContainerElement.classList.remove("video-frame-full-screen");
    }
}