let pdfDoc = null;
let originalBase64Data = null;
let edits = [];
let pdfjsLib = null;

// Initialize PDF.js
window.addEventListener('DOMContentLoaded', async () => {
    try {
        const pdfModule = await import('https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.0.379/pdf.min.mjs');
        pdfjsLib = pdfModule;
        
        // Set worker source
        if (pdfjsLib.GlobalWorkerOptions) {
            pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.0.379/pdf.worker.min.mjs';
        }
        
        document.getElementById('saveBtn').addEventListener('click', savePdf);
        console.log('PDF.js initialized');
    } catch (error) {
        console.error('Failed to load PDF.js:', error);
    }
});

window.loadPdfFromBase64 = async function(base64Data) {
    try {
        console.log('Loading PDF from base64...');
        originalBase64Data = base64Data;
        
        const binaryString = atob(base64Data);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        
        pdfDoc = await pdfjsLib.getDocument(bytes).promise;
        console.log('PDF loaded, rendering pages...');
        renderAllPages();
    } catch (error) {
        console.error('Error loading PDF:', error);
    }
};

async function renderAllPages() {
    try {
        const container = document.getElementById('pdfContainer');
        const instructions = document.getElementById('instructions');
        const saveBtn = document.getElementById('saveBtn');
        
        container.innerHTML = '';
        instructions.style.display = 'none';
        saveBtn.style.display = 'block';
        
        for (let i = 1; i <= pdfDoc.numPages; i++) {
            const page = await pdfDoc.getPage(i);
            const viewport = page.getViewport({ scale: 1.5 });
            
            const pageDiv = document.createElement('div');
            pageDiv.className = 'pdf-page-container';
            pageDiv.style.position = 'relative';
            pageDiv.style.width = viewport.width + 'px';
            pageDiv.style.height = viewport.height + 'px';
            pageDiv.style.margin = '20px auto';
            
            const canvas = document.createElement('canvas');
            canvas.className = 'pdf-page';
            canvas.width = viewport.width;
            canvas.height = viewport.height;
            pageDiv.appendChild(canvas);
            
            const textLayerDiv = document.createElement('div');
            textLayerDiv.className = 'textLayer';
            textLayerDiv.style.position = 'absolute';
            textLayerDiv.style.left = '0';
            textLayerDiv.style.top = '0';
            textLayerDiv.style.width = '100%';
            textLayerDiv.style.height = '100%';
            pageDiv.appendChild(textLayerDiv);
            
            container.appendChild(pageDiv);
            
            await page.render({ canvasContext: canvas.getContext('2d'), viewport }).promise;
            await createAndBindTextLayer(textLayerDiv, page, viewport, i);
        }
    } catch (error) {
        console.error('Error rendering pages:', error);
    }
}

async function createAndBindTextLayer(textLayerDiv, page, viewport, pageNum) {
    const textContent = await page.getTextContent();
    
    await pdfjsLib.renderTextLayer({
        textContentSource: textContent,
        container: textLayerDiv,
        viewport: viewport,
        textDivs: []
    }).promise;
    
    const textSpans = textLayerDiv.children;
    let textContentIndex = 0;
    
    for (const span of textSpans) {
        if (span.textContent.trim()) {
            const originalItem = textContent.items[textContentIndex];
            span.title = 'Click to edit: ' + originalItem.str;
            span.style.cursor = 'text';
            
            span.addEventListener('mouseover', () => span.style.backgroundColor = 'rgba(0,120,204,0.3)');
            span.addEventListener('mouseout', () => span.style.backgroundColor = 'transparent');
            
            span.addEventListener('click', (e) => {
                e.stopPropagation();
                editTextSpan(span, pageNum, textContentIndex, originalItem);
            });
            textContentIndex++;
        }
    }
}

function editTextSpan(span, pageNum, textIndex, originalItem) {
    // Mark span as being edited and make parent fully visible
    span.classList.add('editing');
    const textLayer = span.parentNode;
    const originalOpacity = textLayer.style.opacity;
    textLayer.style.opacity = '1';
    
    const input = document.createElement('input');
    input.className = 'edit-box';
    input.style.position = 'absolute';
    input.style.left = span.style.left;
    input.style.top = span.style.top;
    input.style.fontSize = span.style.fontSize;
    input.style.fontFamily = span.style.fontFamily;
    input.style.transform = span.style.transform;
    input.style.transformOrigin = span.style.transformOrigin;
    input.style.minWidth = '50px';
    input.value = span.textContent;
    input.placeholder = 'Enter text...';
    
    span.parentNode.appendChild(input);
    input.focus();
    input.select();
    
    const finishEdit = () => {
        span.classList.remove('editing');
        textLayer.style.opacity = originalOpacity;
        
        if (input.value !== span.textContent) {
            edits.push({
                pageNum,
                newText: input.value,
                original: {
                    str: originalItem.str,
                    transform: originalItem.transform,
                    width: originalItem.width,
                    height: originalItem.height,
                    fontName: originalItem.fontName
                }
            });
            span.textContent = input.value;
        }
        
        input.remove();
    };
    
    input.addEventListener('blur', finishEdit);
    
    input.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            finishEdit();
        } else if (e.key === 'Escape') {
            span.classList.remove('editing');
            textLayer.style.opacity = originalOpacity;
            input.remove();
        }
    });
}

async function savePdf() {
    try {
        const modifiedPdfBytes = await createModifiedPdf();
        
        // Create blob and download directly
        const blob = new Blob([modifiedPdfBytes], { type: 'application/pdf' });
        const url = URL.createObjectURL(blob);
        
        const a = document.createElement('a');
        a.href = url;
        a.download = `edited_${new Date().toISOString().slice(0,19).replace(/:/g,'-')}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        
        // Just notify C# that save completed
        window.chrome.webview.postMessage({
            action: 'saveCompleted'
        });
    } catch (error) {
        console.error('Error saving PDF:', error);
        alert('Error saving PDF: ' + error.message);
    }
}

async function createModifiedPdf() {
    const { PDFDocument, rgb } = await import('https://unpkg.com/pdf-lib@1.17.1/dist/pdf-lib.esm.js');
    
    // Convert base64 to ArrayBuffer for pdf-lib
    const binaryString = atob(originalBase64Data);
    const arrayBuffer = new ArrayBuffer(binaryString.length);
    const uint8Array = new Uint8Array(arrayBuffer);
    for (let i = 0; i < binaryString.length; i++) {
        uint8Array[i] = binaryString.charCodeAt(i);
    }
    
    const pdfDocLib = await PDFDocument.load(arrayBuffer);
    const pages = pdfDocLib.getPages();
    
    for (const edit of edits) {
        const page = pages[edit.pageNum - 1];
        const original = edit.original;
        const tx = original.transform;
        const fontHeight = original.height;
        const textWidth = original.width;
        
        const x = tx[4];
        const y = tx[5];
        
        page.drawRectangle({
            x: x,
            y: y,
            width: textWidth,
            height: fontHeight,
            color: rgb(1, 1, 1)
        });
        
        page.drawText(edit.newText, {
            x: x,
            y: y,
            size: fontHeight
        });
    }
    
    return await pdfDocLib.save();
}
