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

function filterTextItemsAwayFromForms(textItems, formFieldBoxes, viewport) {
    // Filter out text items that overlap with form field areas
    return textItems.filter(item => {
        const itemX = item.transform[4];
        const itemY = viewport.height - item.transform[5]; // Convert to screen coordinates
        const itemWidth = item.width;
        const itemHeight = item.height;
        
        // Check if text item overlaps with any form field
        return !formFieldBoxes.some(field => {
            return itemX < field.x + field.width &&
                   itemX + itemWidth > field.x &&
                   itemY < field.y + field.height &&
                   itemY + itemHeight > field.y;
        });
    });
}

function groupTextItemsIntoBlocks(items) {
    if (!items || items.length === 0) return [];
    
    // Filter out any items that are purely whitespace or have no string content
    const validItems = items.filter(item => item.str && item.str.trim() !== '');

    if (validItems.length === 0) return [];

    // Sort valid items from top-to-bottom, then left-to-right
    const sortedItems = validItems.sort((a, b) => {
        const yDiff = b.transform[5] - a.transform[5];
        return yDiff !== 0 ? yDiff : a.transform[4] - b.transform[4];
    });
    
    const blocks = [];
    
    // Robust Initialization: Start with the first valid, sorted item.
    let currentBlock = { items: [sortedItems[0]], text: sortedItems[0].str };
    
    for (let i = 1; i < sortedItems.length; i++) {
        const item = sortedItems[i];
        const lastItem = currentBlock.items[currentBlock.items.length - 1];
        
        const yDiff = Math.abs(item.transform[5] - lastItem.transform[5]);
        const lineHeight = lastItem.height || 12;
        const horizontalGap = item.transform[4] - (lastItem.transform[4] + lastItem.width);
        
        // Condition 1: Items are on the same line.
        if (yDiff < 3) { // A small vertical tolerance for same-line text
            const spacing = horizontalGap > lastItem.height * 0.4 ? '  ' : ' ';
            currentBlock.items.push(item);
            currentBlock.text += spacing + item.str;
        } 
        // Condition 2: Item is on a new line and vertically close enough to be in the same block.
        else if (yDiff < lineHeight * 1.6) { 
            currentBlock.items.push(item);
            currentBlock.text += '\n' + item.str;
        } 
        // Condition 3: Item starts a new block.
        else {
            const finalized = finalizeBlock(currentBlock);
            if (finalized) blocks.push(finalized);
            currentBlock = { items: [item], text: item.str };
        }
    }
    
    // Add the last block to the array
    const finalized = finalizeBlock(currentBlock);
    if (finalized) blocks.push(finalized);

    return blocks;
}

function finalizeBlock(block) {
    const items = block.items;
    if (!items || items.length === 0) return null;

    // --- This new logic is simpler and more reliable ---

    const minX = Math.min(...items.map(i => i.transform[4]));
    const maxX = Math.max(...items.map(i => i.transform[4] + i.width));
    
    // The top of the box is the top of the highest character.
    const blockTop = Math.max(...items.map(i => i.transform[5] + i.height));
    // The bottom of the box is the baseline of the lowest character.
    const blockBottom = Math.min(...items.map(i => i.transform[5]));

    const blockWidth = maxX - minX;
    const blockHeight = blockTop - blockBottom;

    // Add a consistent padding for interaction
    const padding = 4; // Increased padding for a better visual feel

    return {
        ...block,
        bounds: {
            x: minX - padding,
            y: blockBottom - padding, // The y-coordinate is the bottom of the box
            width: blockWidth + (padding * 2),
            height: blockHeight + (padding * 2),
            // Store the accurate top coordinate for CSS positioning
            top: blockTop + padding 
        }
    };
}

async function createAndBindTextLayer(textLayerDiv, page, viewport, pageNum) {
    // Phase 1: Process PDF Annotations (Form Fields)
    const annotations = await page.getAnnotations();
    const formFieldBoxes = [];
    
    annotations.forEach(annotation => {
        if (annotation.subtype === 'Widget' && annotation.fieldType === 'Tx') {
            const rect = pdfjsLib.Util.transform(viewport.transform, annotation.rect);
            const x = Math.min(rect[0], rect[2]);
            const y = Math.min(rect[1], rect[3]);
            const width = Math.abs(rect[2] - rect[0]);
            const height = Math.abs(rect[3] - rect[1]);
            
            const fieldDiv = document.createElement('div');
            fieldDiv.className = 'form-field-overlay';
            fieldDiv.style.position = 'absolute';
            fieldDiv.style.left = x + 'px';
            fieldDiv.style.top = y + 'px';
            fieldDiv.style.width = width + 'px';
            fieldDiv.style.height = height + 'px';
            fieldDiv.style.border = '2px solid #28a745';
            fieldDiv.style.backgroundColor = 'rgba(40, 167, 69, 0.1)';
            fieldDiv.style.cursor = 'text';
            
            fieldDiv.addEventListener('click', (e) => {
                e.stopPropagation();
                editFormField(fieldDiv, pageNum, annotation, { x, y, width, height });
            });
            
            textLayerDiv.appendChild(fieldDiv);
            formFieldBoxes.push({ x, y, width, height });
        }
    });
    
    // Phase 2: Use PDF.js native text layer for exact positioning
    const textContent = await page.getTextContent();
    
    await pdfjsLib.renderTextLayer({
        textContentSource: textContent,
        container: textLayerDiv,
        viewport: viewport
    }).promise;
    
    // Phase 3: Convert PDF.js text spans to editable overlays
    setTimeout(() => {
        const textSpans = textLayerDiv.querySelectorAll('span');
        const processedSpans = new Set();
        
        textSpans.forEach(span => {
            if (processedSpans.has(span) || !span.textContent.trim()) return;
            
            // Check if span overlaps with form fields
            const spanRect = span.getBoundingClientRect();
            const containerRect = textLayerDiv.getBoundingClientRect();
            const relativeX = spanRect.left - containerRect.left;
            const relativeY = spanRect.top - containerRect.top;
            
            const overlapsForm = formFieldBoxes.some(field => 
                relativeX < field.x + field.width &&
                relativeX + spanRect.width > field.x &&
                relativeY < field.y + field.height &&
                relativeY + spanRect.height > field.y
            );
            
            if (!overlapsForm) {
                // Group adjacent spans into blocks
                const blockSpans = [span];
                let currentSpan = span;
                
                // Look for adjacent spans
                while (currentSpan.nextElementSibling) {
                    const nextSpan = currentSpan.nextElementSibling;
                    if (processedSpans.has(nextSpan)) break;
                    
                    const currentRect = currentSpan.getBoundingClientRect();
                    const nextRect = nextSpan.getBoundingClientRect();
                    
                    // Check if spans are on same line or close vertically
                    if (Math.abs(currentRect.top - nextRect.top) < 5 || 
                        Math.abs(currentRect.bottom - nextRect.top) < currentRect.height) {
                        blockSpans.push(nextSpan);
                        processedSpans.add(nextSpan);
                        currentSpan = nextSpan;
                    } else {
                        break;
                    }
                }
                
                // Create overlay for the block
                if (blockSpans.length > 0) {
                    const firstRect = blockSpans[0].getBoundingClientRect();
                    const lastRect = blockSpans[blockSpans.length - 1].getBoundingClientRect();
                    
                    const blockLeft = Math.min(firstRect.left, lastRect.left) - containerRect.left;
                    const blockTop = Math.min(firstRect.top, lastRect.top) - containerRect.top;
                    const blockRight = Math.max(firstRect.right, lastRect.right) - containerRect.left;
                    const blockBottom = Math.max(firstRect.bottom, lastRect.bottom) - containerRect.top;
                    
                    const blockDiv = document.createElement('div');
                    blockDiv.className = 'text-block-overlay';
                    blockDiv.style.position = 'absolute';
                    blockDiv.style.left = (blockLeft - 2) + 'px';
                    blockDiv.style.top = (blockTop - 2) + 'px';
                    blockDiv.style.width = (blockRight - blockLeft + 4) + 'px';
                    blockDiv.style.height = (blockBottom - blockTop + 4) + 'px';
                    blockDiv.style.cursor = 'text';
                    
                    const blockText = blockSpans.map(s => s.textContent).join(' ');
                    const blockData = {
                        text: blockText,
                        spans: blockSpans,
                        bounds: {
                            x: blockLeft - 2,
                            y: blockTop - 2,
                            width: blockRight - blockLeft + 4,
                            height: blockBottom - blockTop + 4
                        }
                    };
                    
                    blockDiv.addEventListener('click', (e) => {
                        e.stopPropagation();
                        editTextBlock(blockDiv, pageNum, blockData);
                    });
                    
                    textLayerDiv.appendChild(blockDiv);
                    blockSpans.forEach(s => processedSpans.add(s));
                }
            }
        });
    }, 100);
}

function editFormField(fieldDiv, pageNum, annotation, bounds) {
    // Hide original overlay
    fieldDiv.style.display = 'none';
    const textLayer = fieldDiv.parentNode;
    
    // Create input element for form field
    const inputElement = document.createElement('input');
    inputElement.type = 'text';
    inputElement.className = 'form-field-input';
    inputElement.style.position = 'absolute';
    inputElement.style.left = bounds.x + 'px';
    inputElement.style.top = bounds.y + 'px';
    inputElement.style.width = bounds.width + 'px';
    inputElement.style.height = bounds.height + 'px';
    inputElement.style.border = '2px solid #28a745';
    inputElement.style.padding = '4px';
    inputElement.style.fontSize = '12px';
    inputElement.style.zIndex = '1001';
    inputElement.value = annotation.fieldValue || '';
    
    textLayer.appendChild(inputElement);
    inputElement.focus();
    inputElement.select();
    
    // Finish editing handler
    const finishEdit = (e) => {
        if (!inputElement.contains(e.target)) {
            const newValue = inputElement.value;
            
            // Save form field edit
            if (newValue !== (annotation.fieldValue || '')) {
                edits.push({
                    pageNum,
                    type: 'formField',
                    fieldName: annotation.fieldName,
                    value: newValue,
                    original: annotation
                });
            }
            
            // Cleanup
            fieldDiv.style.display = 'block';
            inputElement.remove();
            document.removeEventListener('mousedown', finishEdit);
        }
    };
    
    // Add global click listener
    setTimeout(() => {
        document.addEventListener('mousedown', finishEdit);
    }, 100);
    
    // Escape key handler
    inputElement.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            fieldDiv.style.display = 'block';
            inputElement.remove();
            document.removeEventListener('mousedown', finishEdit);
        } else if (e.key === 'Enter') {
            inputElement.blur();
        }
    });
}

function editTextBlock(blockDiv, pageNum, block) {
    // 1. Hide original overlay and spans
    blockDiv.style.display = 'none';
    const textLayer = blockDiv.parentNode;
    const originalOpacity = textLayer.style.opacity;
    textLayer.style.opacity = '1';
    
    // Hide original text spans
    block.spans.forEach(span => span.style.display = 'none');
    
    // Extract font properties from first span
    const firstSpan = block.spans[0];
    const computedStyle = window.getComputedStyle(firstSpan);
    const fontSize = parseInt(computedStyle.fontSize) || 12;
    const fontName = computedStyle.fontFamily || 'Arial';
    
    // 2. Create contenteditable div with exact positioning and styling
    const editableDiv = document.createElement('div');
    editableDiv.className = 'contenteditable-text';
    editableDiv.contentEditable = true;
    editableDiv.style.left = blockDiv.style.left;
    editableDiv.style.top = blockDiv.style.top;
    editableDiv.style.width = blockDiv.style.width;
    editableDiv.style.height = blockDiv.style.height;
    editableDiv.style.fontSize = fontSize + 'px';
    editableDiv.style.fontFamily = fontName.replace(/[+]/g, ' ');
    editableDiv.style.lineHeight = '1.2';
    
    // 3. Insert text with proper HTML formatting
    const htmlContent = block.text.replace(/\n/g, '<br>');
    editableDiv.innerHTML = htmlContent;
    
    // 4. Create resize frame with 8 handles
    const resizeFrame = document.createElement('div');
    resizeFrame.className = 'resize-frame';
    resizeFrame.style.left = blockDiv.style.left;
    resizeFrame.style.top = blockDiv.style.top;
    resizeFrame.style.width = blockDiv.style.width;
    resizeFrame.style.height = blockDiv.style.height;
    
    const handles = ['nw', 'n', 'ne', 'e', 'se', 's', 'sw', 'w'];
    handles.forEach(handle => {
        const handleDiv = document.createElement('div');
        handleDiv.className = `resize-handle ${handle}`;
        handleDiv.addEventListener('mousedown', (e) => startResize(e, handle, resizeFrame, editableDiv));
        resizeFrame.appendChild(handleDiv);
    });
    
    // 5. Add elements to DOM
    textLayer.appendChild(resizeFrame);
    textLayer.appendChild(editableDiv);
    
    // 6. Show floating toolbar
    showFloatingToolbar(resizeFrame, fontSize);
    
    // 7. Focus editor
    editableDiv.focus();
    
    // 8. Global click handler for finishing edit
    const finishEdit = (e) => {
        // Check if click is outside editor and toolbar
        if (!editableDiv.contains(e.target) && 
            !document.getElementById('floatingToolbar').contains(e.target) &&
            !resizeFrame.contains(e.target)) {
            
            // Save changes
            const newContent = editableDiv.innerHTML.replace(/<br>/g, '\n');
            if (newContent !== block.text) {
                edits.push({
                    pageNum,
                    newText: editableDiv.innerHTML, // Keep HTML for rich text
                    type: 'block',
                    original: {
                        block: block,
                        bounds: block.bounds,
                        fontSize: fontSize,
                        fontName: fontName
                    }
                });
                block.text = newContent;
            }
            
            // Cleanup
            blockDiv.style.display = 'block';
            block.spans.forEach(span => span.style.display = 'block');
            textLayer.style.opacity = originalOpacity;
            hideFloatingToolbar();
            resizeFrame.remove();
            editableDiv.remove();
            document.removeEventListener('mousedown', finishEdit);
        }
    };
    
    // Add global click listener with delay to allow toolbar interactions
    setTimeout(() => {
        document.addEventListener('mousedown', finishEdit);
    }, 100);
    
    // Escape key handler
    editableDiv.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            blockDiv.style.display = 'block';
            block.spans.forEach(span => span.style.display = 'block');
            textLayer.style.opacity = originalOpacity;
            hideFloatingToolbar();
            resizeFrame.remove();
            editableDiv.remove();
            document.removeEventListener('mousedown', finishEdit);
        }
    });
}

function startResize(e, handle, frame, editableDiv) {
    e.preventDefault();
    e.stopPropagation();
    
    const startX = e.clientX;
    const startY = e.clientY;
    const startWidth = parseInt(frame.style.width);
    const startHeight = parseInt(frame.style.height);
    const startLeft = parseInt(frame.style.left);
    const startTop = parseInt(frame.style.top);
    
    const onMouseMove = (e) => {
        const deltaX = e.clientX - startX;
        const deltaY = e.clientY - startY;
        
        let newWidth = startWidth;
        let newHeight = startHeight;
        let newLeft = startLeft;
        let newTop = startTop;
        
        // Calculate new dimensions based on handle direction
        if (handle.includes('e')) newWidth = startWidth + deltaX;
        if (handle.includes('w')) { 
            newWidth = startWidth - deltaX; 
            newLeft = startLeft + deltaX; 
        }
        if (handle.includes('s')) newHeight = startHeight + deltaY;
        if (handle.includes('n')) { 
            newHeight = startHeight - deltaY; 
            newTop = startTop + deltaY; 
        }
        
        // Apply minimum constraints and update both frame and editor
        if (newWidth > 60) {
            frame.style.width = newWidth + 'px';
            editableDiv.style.width = newWidth + 'px';
            if (handle.includes('w')) {
                frame.style.left = newLeft + 'px';
                editableDiv.style.left = newLeft + 'px';
            }
        }
        
        if (newHeight > 30) {
            frame.style.height = newHeight + 'px';
            editableDiv.style.height = newHeight + 'px';
            if (handle.includes('n')) {
                frame.style.top = newTop + 'px';
                editableDiv.style.top = newTop + 'px';
            }
        }
        
        // Reposition toolbar to stay above the frame
        const toolbar = document.getElementById('floatingToolbar');
        const frameRect = frame.getBoundingClientRect();
        const topPos = frameRect.top + window.scrollY - 50;
        const leftPos = frameRect.left + window.scrollX + (frameRect.width / 2) - (toolbar.offsetWidth / 2);
        
        toolbar.style.left = leftPos + 'px';
        toolbar.style.top = topPos + 'px';
    };
    
    const onMouseUp = () => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);
        
        // Ensure toolbar is properly positioned after resize
        showFloatingToolbar(frame, parseInt(editableDiv.style.fontSize));
    };
    
    window.addEventListener('mousemove', onMouseMove);
    window.addEventListener('mouseup', onMouseUp);
}

function showFloatingToolbar(frame, fontSize) {
    const toolbar = document.getElementById('floatingToolbar');
    const frameRect = frame.getBoundingClientRect();
    
    // The key fix is adding window.scrollY to the top calculation
    const topPos = frameRect.top + window.scrollY - 45; // 45px above the frame
    const leftPos = frameRect.left + window.scrollX + (frameRect.width / 2) - (toolbar.offsetWidth / 2);
    
    toolbar.style.left = leftPos + 'px';
    toolbar.style.top = topPos + 'px';
    toolbar.style.display = 'flex'; // Use flex for better alignment of items inside
    
    // Ensure the font size select reflects the actual font size
    document.getElementById('fontSizeSelect').value = Math.round(fontSize);
}

function hideFloatingToolbar() {
    document.getElementById('floatingToolbar').style.display = 'none';
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

function parseHtmlToSegments(html) {
    // Enhanced HTML parser for contenteditable output
    const lines = [];
    
    // Split by <br> tags for line breaks
    const htmlLines = html.split(/<br\s*\/?>/i);
    
    htmlLines.forEach(line => {
        const segments = [];
        let currentText = '';
        let isBold = false;
        let isItalic = false;
        
        // Parse HTML tags using a more robust approach
        const tagRegex = /<\/?(?:b|strong|i|em)>/gi;
        const parts = line.split(tagRegex);
        const tags = line.match(tagRegex) || [];
        
        let tagIndex = 0;
        parts.forEach((part, index) => {
            // Process text content
            if (part && part.trim()) {
                currentText += part;
            }
            
            // Process tag if exists
            if (tagIndex < tags.length) {
                const tag = tags[tagIndex].toLowerCase();
                
                if (tag === '<b>' || tag === '<strong>') {
                    if (currentText) {
                        segments.push({ text: currentText, bold: isBold, italic: isItalic });
                        currentText = '';
                    }
                    isBold = true;
                } else if (tag === '</b>' || tag === '</strong>') {
                    if (currentText) {
                        segments.push({ text: currentText, bold: isBold, italic: isItalic });
                        currentText = '';
                    }
                    isBold = false;
                } else if (tag === '<i>' || tag === '<em>') {
                    if (currentText) {
                        segments.push({ text: currentText, bold: isBold, italic: isItalic });
                        currentText = '';
                    }
                    isItalic = true;
                } else if (tag === '</i>' || tag === '</em>') {
                    if (currentText) {
                        segments.push({ text: currentText, bold: isBold, italic: isItalic });
                        currentText = '';
                    }
                    isItalic = false;
                }
                tagIndex++;
            }
        });
        
        // Add remaining text
        if (currentText) {
            segments.push({ text: currentText, bold: isBold, italic: isItalic });
        }
        
        // Only add line if it has content
        if (segments.length > 0) {
            lines.push({ segments });
        }
    });
    
    return lines;
}

async function createModifiedPdf() {
    const { PDFDocument, rgb, StandardFonts } = await import('https://unpkg.com/pdf-lib@1.17.1/dist/pdf-lib.esm.js');
    
    const binaryString = atob(originalBase64Data);
    const arrayBuffer = new ArrayBuffer(binaryString.length);
    const uint8Array = new Uint8Array(arrayBuffer);
    for (let i = 0; i < binaryString.length; i++) {
        uint8Array[i] = binaryString.charCodeAt(i);
    }
    
    const pdfDocLib = await PDFDocument.load(arrayBuffer);
    const pages = pdfDocLib.getPages();
    
    // Embed standard fonts
    const helvetica = await pdfDocLib.embedFont(StandardFonts.Helvetica);
    const helveticaBold = await pdfDocLib.embedFont(StandardFonts.HelveticaBold);
    const times = await pdfDocLib.embedFont(StandardFonts.TimesRoman);
    
    // Get form for form field edits
    const form = pdfDocLib.getForm();
    
    for (const edit of edits) {
        const page = pages[edit.pageNum - 1];
        
        if (edit.type === 'formField') {
            // Handle form field edits using PDF-native APIs
            try {
                const textField = form.getTextField(edit.fieldName);
                textField.setText(edit.value);
                // Optional: textField.enableReadOnly(); to prevent further editing
            } catch (error) {
                console.warn(`Could not update form field '${edit.fieldName}':`, error);
            }
        } else if (edit.type === 'block') {
            const bounds = edit.original.bounds;
            const fontSize = edit.original.fontSize || 12;
            const fontName = edit.original.fontName || 'Helvetica';
            
            // Select appropriate font
            let font = helvetica;
            if (fontName.includes('Bold')) font = helveticaBold;
            else if (fontName.includes('Times')) font = times;
            
            // 1. Draw the whiteout rectangle using the ACCURATE bounds
            page.drawRectangle({
                x: bounds.x,
                y: bounds.y,
                width: bounds.width,
                height: bounds.height,
                color: rgb(1, 1, 1)
            });
            
            // 2. Parse HTML content for rich text rendering
            const htmlContent = edit.newText;
            const lines = parseHtmlToSegments(htmlContent);
            const lineHeight = fontSize * 1.2;
            
            // 3. Draw text from the TOP DOWN for stable positioning
            const startY = bounds.top - fontSize;
            
            let currentY = startY;
            lines.forEach(line => {
                let currentX = bounds.x + 4; // Start with left padding
                
                line.segments.forEach(segment => {
                    if (segment.text && segment.text.trim()) {
                        // Select appropriate font based on formatting
                        let segmentFont = font;
                        if (segment.bold && segment.italic) {
                            segmentFont = helveticaBold; // Use bold for bold+italic combination
                        } else if (segment.bold) {
                            segmentFont = helveticaBold;
                        } else if (segment.italic) {
                            segmentFont = times; // Use Times for italic
                        }
                        
                        // Draw the text segment
                        page.drawText(segment.text, {
                            x: currentX,
                            y: currentY,
                            size: fontSize,
                            font: segmentFont,
                            color: rgb(0, 0, 0)
                        });
                        
                        // Advance X position for next segment
                        currentX += segmentFont.widthOfTextAtSize(segment.text, fontSize);
                    }
                });
                
                // Move to next line
                currentY -= lineHeight;
            });
        } else {
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
    }
    
    return await pdfDocLib.save();
}