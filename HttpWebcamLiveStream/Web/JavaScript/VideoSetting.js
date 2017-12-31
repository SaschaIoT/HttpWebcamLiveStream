var supportedVideoSettings;

var videoResolutionElement = document.getElementById("videoResolution");
var videoSubtypeElement = document.getElementById("videoSubtype");
var videoQualityElement = document.getElementById("videoQuality");
var usedThreadsElement = document.getElementById("usedThreads");
var saveElement = document.getElementById("save");

var videoSettingRequest = new XMLHttpRequest();
videoSettingRequest.open("GET", "VideoSetting", true);
videoSettingRequest.responseType = "json";
videoSettingRequest.onload = function () {
    var status = videoSettingRequest.status;
    if (status == 200) {
        var videoSetting = videoSettingRequest.response;

        document.querySelector('#videoQuality > option[value="' + videoSetting.VideoQuality.toString() + '"]').selected = true;
        document.querySelector('#usedThreads > option[value="' + videoSetting.UsedThreads.toString() + '"]').selected = true;
        
        var supportedVideoSettingRequest = new XMLHttpRequest();
        supportedVideoSettingRequest.open("GET", "SupportedVideoSettings", true);
        supportedVideoSettingRequest.responseType = "json";
        supportedVideoSettingRequest.onload = function () {
            var status = supportedVideoSettingRequest.status;
            if (status == 200) {
                supportedVideoSettings = supportedVideoSettingRequest.response;
                
                //Set video subtypes
                supportedVideoSettings.forEach(function (videoSetting) {
                   
                    var videoSubtypeOption = document.createElement('option');
                    videoSubtypeOption.value = videoSetting.VideoSubtype;
                    videoSubtypeOption.innerHTML = videoSetting.VideoSubtype;
                    videoSubtypeElement.appendChild(videoSubtypeOption);
                });

                document.querySelector('#videoSubtype > option[value="' + videoSetting.VideoSubtype.toString() + '"]').selected = true;

                //Set video resolutions
                SetVideoResolutions(videoSetting.VideoSubtype);

                document.querySelector('#videoResolution > option[value="' + videoSetting.VideoResolution.toString() + '"]').selected = true;
            }
        };
        supportedVideoSettingRequest.send();
    }
};

function SetVideoResolutions(videoSubtype) {

    var videoResolution = null;

    if (videoResolutionElement.options.length > 0) {
        videoResolution = videoResolutionElement.options[videoResolutionElement.options.selectedIndex].value;
    }

    while (videoResolutionElement.options.length > 0) {
        videoResolutionElement.remove(0);
    }

    var videoResolutions = supportedVideoSettings.filter(function (svs) { return svs.VideoSubtype === videoSubtype; })[0].VideoResolutions;

    var containOldVideoResolution = videoResolution === null ? false : videoResolutions.filter(function (svs) { return svs.toString() === videoResolution; }).length == 1;

    videoResolutions.forEach(function (videoResolution) {

        var videoSubtypeVideoResolution = document.createElement('option');
        videoSubtypeVideoResolution.value = videoResolution;
        videoSubtypeVideoResolution.innerHTML = GetVideoResolutionName(videoResolution);
        videoResolutionElement.appendChild(videoSubtypeVideoResolution);
    });

    if(containOldVideoResolution === true) {
        document.querySelector('#videoResolution > option[value="' + videoResolution.toString() + '"]').selected = true;
    }
}

function GetVideoResolutionName(videoResolution) {

    if (videoResolution == 0) {
        return "HD1080p";
    }
    else if (videoResolution == 1) {
        return "HD720p";
    }
    else if (videoResolution == 2) {
        return "SD1024_768";
    }
    else if (videoResolution == 3) {
        return "SD800_600";
    }
    else if (videoResolution == 4) {
        return "SD640_480";
    }
}

videoSubtypeElement.addEventListener("change", function () {
    SetVideoResolutions(videoSubtypeElement.value);
});

saveElement.addEventListener("click", function () {
    SetVideoSetting();
});

saveElement.addEventListener("touchstart", function () {
    SetVideoSetting();
}, { passive: true });

function SetVideoSetting() {
    var videoSubtype = videoSubtypeElement.options[videoSubtypeElement.options.selectedIndex].value;
    var videoResolution = videoResolutionElement.options[videoResolutionElement.options.selectedIndex].value;
    var videoQuality = videoQualityElement.options[videoQualityElement.options.selectedIndex].value;
    var usedThreads = usedThreadsElement.options[usedThreadsElement.options.selectedIndex].value;

    var saveRequest = new XMLHttpRequest();
    var videoSetting = { "VideoSubtype": videoSubtype, "VideoResolution": parseInt(videoResolution, 10), "VideoQuality": parseFloat(videoQuality), "UsedThreads": parseInt(usedThreads, 10) };
    saveRequest.open("GET", "SaveVideoSetting/<RequestBody>" + JSON.stringify(videoSetting) + "</RequestBody>", true);
    saveRequest.responseType = "json";
    saveRequest.send();
}

videoSettingRequest.send();