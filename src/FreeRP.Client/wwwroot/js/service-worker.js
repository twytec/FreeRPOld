
var frpServerUrl = "";
let myDotNet;

self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

async function onInstall(event) {
    console.info('Service worker: Install');
}

async function onActivate(event) {
    console.info('Service worker: Activate');
}

async function onFetch(event) {
    if (myDotNet === undefined) {
        return fetch(event.request);
    }
    
}

//self.addEventListener("message", (evt) => {
//    frpServerUrl = evt.data;
//});

//navigator.serviceWorker.ready.then((registration) => {
//    DotNet.invokeMethodAsync("FreeRP.Client", "MessageFromServiceWorker", evt);
//});

//navigator.serviceWorker.addEventListener("message", (evt) => {
//    DotNet.invokeMethodAsync("FreeRP.Client", "MessageFromServiceWorker", evt);
//});

//window.messageToServiceWorker = (msg) => {
//    navigator.serviceWorker.ready
//        .then((registration) => {
//            if (registration.active) {
//                registration.active.postMessage(msg);
//            }
//        });
//};