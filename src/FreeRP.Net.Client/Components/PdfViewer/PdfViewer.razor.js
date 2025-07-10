export function loadPdf(viewerId, b64, url) {

    var container = document.getElementById(viewerId);
    container.innerHTML = "";
    var maxWidth = container.parentElement.clientWidth - 20;
    var maxHeight = 0;
    var currPage = 1;
    var numPages = 0;
    var thePDF = null;

    
    pdfjsLib.GlobalWorkerOptions.workerSrc = '//mozilla.github.io/pdf.js/build/pdf.worker.mjs';

    var loadingTask;

    if (b64 != '') {
        loadingTask = pdfjsLib.getDocument({ data: atob(b64) });
    }
    else {
        loadingTask = pdfjsLib.getDocument(url);
    }

    loadingTask.promise.then(function (pdf) {
        console.log('PDF loaded');
        thePDF = pdf;
        numPages = pdf.numPages;
        thePDF.getPage(1).then(handlePages);
    }, function (reason) {
        // PDF loading error
        console.error(reason);
    });

    function handlePages(page) {
        var viewport = page.getViewport({ scale: 1.5 });
        var canvas = document.createElement("canvas");
        var context = canvas.getContext('2d');

        var outputScale = window.devicePixelRatio || 1;
        canvas.width = Math.floor(viewport.width * outputScale);
        canvas.height = Math.floor(viewport.height * outputScale);
        maxHeight += canvas.height;

        var transform = outputScale !== 1
            ? [outputScale, 0, 0, outputScale, 0, 0]
            : null;

        page.render({
            canvasContext: context,
            transform: transform,
            viewport: viewport
        });
        
        container.appendChild(canvas);
        var scale = maxWidth / canvas.width;
        if (scale > 1)
            scale = 1;
        container.style.transform = "scale(" + scale + ")"
        container.style.height = Math.floor(maxHeight * scale) + "px";

        currPage++;
        if (thePDF !== null && currPage <= numPages) {
            thePDF.getPage(currPage).then(handlePages);
        }
    }
}