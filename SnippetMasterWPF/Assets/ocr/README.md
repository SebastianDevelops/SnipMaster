# PaddleOCR Integration

This folder contains the ONNX models for PaddleOCR text detection and recognition.

## Files Structure
```
Assets/ocr/
├── det/
│   └── det.onnx          # Text detection model
└── rec/
    ├── rec.onnx          # Text recognition model (English)
    └── dict.txt          # Character dictionary for recognition
```

## Implementation

PaddleOCR is now the default and only OCR engine used in SnippetMasterWPF. The implementation is in `Services/OcrService.cs` and uses:
- ONNX Runtime for model inference
- OpenCV Sharp for image preprocessing
- Custom CTC decoder for text recognition

## Model Information
- **Detection Model**: PaddleOCR v5 detection model (det.onnx)
- **Recognition Model**: English text recognition model (rec.onnx)
- **Dictionary**: English character set (dict.txt)

## Performance Notes
- PaddleOCR typically performs better on:
  - Rotated text
  - Complex backgrounds
  - Multiple text orientations
  - Handwritten text (with appropriate models)

- Tesseract typically performs better on:
  - Clean, high-contrast text
  - Standard document layouts
  - When you need specific language support

## Adding Other Languages
To add support for other languages:
1. Download the corresponding `rec.onnx` and `dict.txt` from the PaddleOCR model repository
2. Replace the files in the `rec/` folder
3. Restart the application

## Troubleshooting
- If OCR returns empty results, check the debug output in Visual Studio
- Ensure all model files exist in the correct locations
- Try adjusting the detection threshold in `PaddleOcrService.cs` (binThreshold variable)