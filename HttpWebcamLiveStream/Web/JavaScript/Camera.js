function GetVideoFrame() {

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
        setTimeout(function () { GetVideoFrame(); }, 0);
    }

    xhr.onerror = function () {
        setTimeout(function () { GetVideoFrame(); }, 0);
    }

    xhr.onload = function () {
        setTimeout(function () { GetVideoFrame(); }, 0);
    }

    xhr.send();
}

GetVideoFrame();