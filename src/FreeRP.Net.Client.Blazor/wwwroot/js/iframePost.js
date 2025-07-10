
window.setFrpUser = (frpuser) => {
    window.frpuser = frpuser;
};

window.setFrpTheme = (theme) => {
    window.frptheme = theme;
};

window.setFrpToken = (token) => {
    window.frptoken = token;
};

window.sendMessageToPlugin = (id, name, data) => {
    const frame = document.getElementById(id);
    if (frame) {
        frame.contentWindow.postMessage(name, data);
    }
};

window.addEventListener('message', function (event) {
    if (event.data == "User") {
        event.source.postMessage({ "User": window.frpuser }, "*");
    }
    else if (event.data == "Theme") {
        event.source.postMessage({ "Theme": window.frptheme }, "*");
    }
    else if (event.data == "Token") {
        event.source.postMessage({ "Token": window.frptoken }, "*");
    }
});

