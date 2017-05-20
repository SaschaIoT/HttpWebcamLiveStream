var showControls = document.location.pathname !== undefined ? document.location.pathname.toLowerCase().startsWith("/control") : false;

if (showControls === true) {
    document.getElementById("video-settings").classList.remove("video-settings-hide");
}