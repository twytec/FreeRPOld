import "./pdfjs-dist/build/pdf.mjs";
import "./pdfjs-dist/build/pdf.worker.mjs";

/**
 * Convert a PDF to image per page with text
 * @param {string} base64 - PDF as base64 string
 * @param {string} url - PDf as URL
 * @param {number} scale - Scale PDF. Default is 1.5
 * @returns {PdfPage[]} - Array of PdfPage
 */
export async function getPdfPages(base64, url, scale = 1.5) {
    var { pdfjsLib } = globalThis;
    pdfjsLib.GlobalWorkerOptions.workerSrc = globalThis.pdfjsWorker;

    const pages = [];
    let currPage = 1;

    var loadingTask;
    if (base64 != '') {
        loadingTask = pdfjsLib.getDocument({ data: atob(base64) });
    }
    else {
        loadingTask = pdfjsLib.getDocument(url);
    }

    const thePDF = await loadingTask.promise;
    const numPages = thePDF.numPages;

    while (currPage <= numPages) {
        const page = await thePDF.getPage(currPage);
        const text = await page.getTextContent();

        const viewport = page.getViewport({ scale: scale });
        const canvas = document.createElement("canvas");
        const context = canvas.getContext('2d');

        const outputScale = window.devicePixelRatio || 1;
        canvas.width = Math.floor(viewport.width * outputScale);
        canvas.height = Math.floor(viewport.height * outputScale);

        await page.render({
            canvasContext: context,
            viewport: viewport
        }).promise;

        currPage++;
        const img = canvas.toDataURL('image/jpeg');
        const pdfPage = new PdfPage(img, canvas.width, canvas.height);
        pages.push(pdfPage);
        text.items.forEach(element => {
            let width = element.width * scale;
            let height = element.height * scale;
            let x = element.transform[4] * scale;

            //calculate top-left from bottom-left
            let y = pdfPage.height - element.height - (element.transform[5] * scale);

            pdfPage.addText(width, height, x, y, element.str);
        });
    }

    return pages;
}

class PdfPage {
    /**
     * Create PDF page
     * @param {string} image as Data Url 
     * @param {number} width PDF page width
     * @param {number} height PDF page height
     */
    constructor(image, width, height)
    {
        this.image = image;
        this.width = width;
        this.height = height;
        this.textContent = [];
    }

    /**
     * Add text to PDF page
     * @param {number} width - text width
     * @param {number} height - text height
     * @param {number} x - x posttion from left
     * @param {number} y - y position form top
     * @param {string} text 
     */
    addText(width, height, x, y, text)
    {
        let t = new PdfPageText(width, height, x, y, text);
        this.textContent.push(t);
    }
}

class PdfPageText {
    /**
     * Add text to PDF page
     * @param {number} width - text width
     * @param {number} height - text height
     * @param {number} x - x position from left
     * @param {number} y - y position form top
     * @param {string} text - The text
     */
    constructor(width, height, x, y, text)
    {
        this.width = width;
        this.height = height;
        this.x = x;
        this.y = y;
        this.text = text;
    }
}