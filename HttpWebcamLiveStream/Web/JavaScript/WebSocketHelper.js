(function () {
    var webSocketHelper = {
        waitUntilWebsocketReady: function (callback, webSocket, retries) {

            retries++;

            if (retries === 20) {
                webSocket.close();
                return;
            }

            if (webSocket.readyState === 1) {
                callback();
            } else {
                setTimeout(function () {
                    webSocketHelper.waitUntilWebsocketReady(callback, webSocket, retries);
                }, 1);
            }
        }
    };

    window.webSocketHelper = webSocketHelper;
})();