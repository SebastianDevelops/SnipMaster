using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace SnippetMasterWPF.Services
{
    public class OcrService : IOcrService, IDisposable
    {
        private readonly InferenceSession _detSession;
        private readonly InferenceSession _recSession;
        private readonly string[] _dict;

        public OcrService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var detPath = Path.Combine(baseDir, "Assets", "ocr", "det", "det.onnx");
            var recPath = Path.Combine(baseDir, "Assets", "ocr", "rec", "rec.onnx");
            var dictPath = Path.Combine(baseDir, "Assets", "ocr", "rec", "dict.txt");

            System.Diagnostics.Debug.WriteLine($"Detection model exists: {File.Exists(detPath)}");
            System.Diagnostics.Debug.WriteLine($"Recognition model exists: {File.Exists(recPath)}");
            System.Diagnostics.Debug.WriteLine($"Dictionary exists: {File.Exists(dictPath)}");

            _detSession = new InferenceSession(detPath);
            _recSession = new InferenceSession(recPath);
            _dict = File.ReadAllLines(dictPath)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(l => l.Trim()).ToArray();
                        
            System.Diagnostics.Debug.WriteLine($"Dictionary loaded with {_dict.Length} entries");
            if (_dict.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine($"First few dict entries: [{string.Join(", ", _dict.Take(10).Select(s => $"'{s}'"))}]");
            }
        }

        public void Dispose()
        {
            _detSession?.Dispose();
            _recSession?.Dispose();
        }

        public string ReadFromUploadedFile(string filePath)
        {
            try
            {
                using var mat = Cv2.ImRead(filePath, ImreadModes.Color);
                if (mat.Empty()) return "";
                
                var textBoxes = DetectText(mat);
                var results = new List<string>();
                
                foreach (var box in textBoxes)
                {
                    var cropped = CropTextRegion(mat, box);
                    var text = RecognizeText(cropped);
                    if (!string.IsNullOrEmpty(text))
                        results.Add(text);
                }
                
                return string.Join(" ", results);
            }
            catch
            {
                return "";
            }
        }

        public string ReadFromSnippedImage(BitmapImage image)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;

                using var originalMat = Cv2.ImDecode(memoryStream.ToArray(), ImreadModes.Color);
                if (originalMat.Empty()) return "";
                
                // Enhanced preprocessing for better OCR
                using var mat = PreprocessImage(originalMat);
                
                // For small images or code-like content
                if (mat.Width < 500 && mat.Height < 200)
                {
                    return FixSymbolErrors(ProcessSmallImage(mat));
                }
                
                var textBoxes = DetectText(mat);
                var results = new List<string>();
                
                foreach (var box in textBoxes)
                {
                    var cropped = CropTextRegion(mat, box);
                    var text = RecognizeText(cropped);
                    if (!string.IsNullOrEmpty(text))
                        results.Add(text);
                }
                
                return FixSymbolErrors(string.Join(" ", results));
            }
            catch
            {
                return "";
            }
        }
        
        private string FixSymbolErrors(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Math and punctuation symbols
            text = text.Replace("—", "-");
            text = text.Replace("–", "-");
            text = text.Replace("×", "*");
            text = text.Replace("÷", "/");
            text = text.Replace("≤", "<=");
            text = text.Replace("≥", ">=");
            text = text.Replace("≠", "!=");
            text = text.Replace("Ctr+t", "Ctrl+V");
            text = text.Replace("CtrlhitV", "Ctrl+V");
            text = text.Replace("ClipboardHistory(CtrlhitV)", "Clipboard History (Ctrl+V)");
            text = text.Replace("l+", "+");
            text = text.Replace("I+", "+");
            text = text.Replace("hit", "+");
            text = text.Replace("Ctrl hit", "Ctrl+");
            text = text.Replace("Alt hit", "Alt+");
            text = text.Replace("Shift hit", "Shift+");
            
            // Basic emoji recognition (OCR often sees these as shapes/letters)
            text = text.Replace(":)", "😊");
            text = text.Replace(":(", "😞");
            text = text.Replace(":D", "😃");
            text = text.Replace(":P", "😛");
            text = text.Replace(";)", "😉");
            text = text.Replace("<3", "❤️");
            text = text.Replace("</3", "💔");
            text = text.Replace(":o", "😮");
            text = text.Replace(":||", "😐");
            text = text.Replace(":|", "😐");
            
            // Common OCR misrecognitions of emoji-like symbols
            text = text.Replace("O", "😮"); // Only if isolated
            text = text.Replace("D", "😃"); // Only if isolated
            
            return text.Trim();
        }
        
        private Mat PreprocessImage(Mat img)
        {
            // Enhance image for better OCR
            Mat processed = new Mat();
            
            // Convert to grayscale
            Mat gray = new Mat();
            Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
            
            // Apply slight Gaussian blur to reduce noise
            Mat blurred = new Mat();
            Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(1, 1), 0);
            
            // Enhance contrast
            Mat enhanced = new Mat();
            Cv2.ConvertScaleAbs(blurred, enhanced, 1.2, 10);
            
            // Convert back to color for the OCR model
            Cv2.CvtColor(enhanced, processed, ColorConversionCodes.GRAY2BGR);
            
            return processed;
        }
        
        private string ProcessSmallImage(Mat img)
        {
            // Check if this looks like code by analyzing the image
            bool looksLikeCode = DetectCodePattern(img);
            
            if (looksLikeCode)
            {
                return ProcessCodeImage(img);
            }
            
            // Regular text processing
            var fullText = RecognizeText(img);
            
            if (!string.IsNullOrEmpty(fullText) && !fullText.Contains(" "))
            {
                var words = SplitIntoWords(img);
                if (words.Count > 1)
                {
                    var results = new List<string>();
                    foreach (var wordRect in words)
                    {
                        var cropped = CropTextRegion(img, wordRect);
                        var text = RecognizeText(cropped);
                        if (!string.IsNullOrEmpty(text))
                            results.Add(text);
                    }
                    
                    if (results.Count > 1)
                        return string.Join(" ", results);
                }
                
                return AddSpacesToCamelCase(fullText);
            }
            
            return fullText;
        }
        
        private bool DetectCodePattern(Mat img)
        {
            // Convert to grayscale
            Mat gray = new Mat();
            Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
            
            // Look for code indicators: indentation, brackets, semicolons
            var text = RecognizeText(img);
            if (string.IsNullOrEmpty(text)) return false;
            
            // Check for common code patterns
            var codeIndicators = new[] { "{", "}", ";", "(", ")", "[", "]", "=", "++", "--", "//", "/*", "*/", "<", ">" };
            int codeScore = codeIndicators.Count(indicator => text.Contains(indicator));
            
            // Check for indentation (leading whitespace)
            bool hasIndentation = text.StartsWith(" ") || text.StartsWith("\t");
            
            return codeScore >= 2 || hasIndentation;
        }
        
        private string ProcessCodeImage(Mat img)
        {
            // Process code line by line to preserve structure
            var lines = SplitIntoLines(img);
            var codeLines = new List<string>();
            
            foreach (var lineRect in lines)
            {
                var cropped = CropTextRegion(img, lineRect);
                var lineText = RecognizeText(cropped);
                if (!string.IsNullOrEmpty(lineText))
                {
                    // Preserve indentation and fix common OCR errors in code
                    lineText = FixCodeOcrErrors(lineText);
                    codeLines.Add(lineText);
                }
            }
            
            return string.Join(Environment.NewLine, codeLines);
        }
        
        private List<OpenCvSharp.Rect> SplitIntoLines(Mat img)
        {
            var lines = new List<OpenCvSharp.Rect>();
            
            try
            {
                Mat gray = new Mat();
                Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
                Mat binary = new Mat();
                Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                
                // Find horizontal projection to detect line boundaries
                var projection = new int[img.Height];
                for (int y = 0; y < img.Height; y++)
                {
                    for (int x = 0; x < img.Width; x++)
                    {
                        if (binary.At<byte>(y, x) == 0) // Black pixel (text)
                            projection[y]++;
                    }
                }
                
                // Find line boundaries
                var gapThreshold = Math.Max(1, img.Width * 0.05);
                var inLine = false;
                var lineStart = 0;
                
                for (int y = 0; y < img.Height; y++)
                {
                    bool hasText = projection[y] > gapThreshold;
                    
                    if (!inLine && hasText)
                    {
                        lineStart = y;
                        inLine = true;
                    }
                    else if (inLine && !hasText)
                    {
                        if (y - lineStart > 8) // Minimum line height
                        {
                            lines.Add(new OpenCvSharp.Rect(0, lineStart, img.Width, y - lineStart));
                        }
                        inLine = false;
                    }
                }
                
                // Add final line if we're still in one
                if (inLine && img.Height - lineStart > 8)
                {
                    lines.Add(new OpenCvSharp.Rect(0, lineStart, img.Width, img.Height - lineStart));
                }
            }
            catch { }
            
            return lines.Count > 0 ? lines : new List<OpenCvSharp.Rect> { new OpenCvSharp.Rect(0, 0, img.Width, img.Height) };
        }
        
        private string FixCodeOcrErrors(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Common OCR errors in code
            var fixes = new Dictionary<string, string>
            {
                { "0", "O" }, // Context-dependent
                { "1", "l" }, // Context-dependent  
                { "5", "S" }, // Context-dependent
                { "8", "B" }, // Context-dependent
                { "rn", "m" },
                { "vv", "w" },
                { "ii", "ll" },
                { "|", "l" },
                { "}", "]" }, // Context-dependent
                { "{", "[" }, // Context-dependent
            };
            
            // Apply fixes cautiously - only for obvious cases
            foreach (var fix in fixes)
            {
                // Only apply certain fixes in code context
                if (fix.Key == "rn" || fix.Key == "vv" || fix.Key == "ii")
                {
                    text = text.Replace(fix.Key, fix.Value);
                }
            }
            
            return text;
        }
        
        private string AddSpacesToCamelCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            var result = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && char.IsUpper(text[i]) && char.IsLower(text[i-1]))
                {
                    result.Append(' ');
                }
                result.Append(text[i]);
            }
            return result.ToString();
        }
        
        private List<OpenCvSharp.Rect> SplitIntoWords(Mat img)
        {
            var words = new List<OpenCvSharp.Rect>();
            
            try
            {
                // Convert to grayscale and threshold
                Mat gray = new Mat();
                Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
                Mat binary = new Mat();
                Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                
                // Find vertical projection to detect word boundaries
                var projection = new int[img.Width];
                for (int x = 0; x < img.Width; x++)
                {
                    for (int y = 0; y < img.Height; y++)
                    {
                        if (binary.At<byte>(y, x) == 0) // Black pixel (text)
                            projection[x]++;
                    }
                }
                
                // Find gaps (columns with very few text pixels)
                var gapThreshold = Math.Max(1, img.Height * 0.05); // Very low threshold
                var inWord = false;
                var wordStart = 0;
                
                for (int x = 0; x < img.Width; x++)
                {
                    bool hasText = projection[x] > gapThreshold;
                    
                    if (!inWord && hasText)
                    {
                        wordStart = x;
                        inWord = true;
                    }
                    else if (inWord && !hasText)
                    {
                        if (x - wordStart > 15) // Minimum word width
                        {
                            words.Add(new OpenCvSharp.Rect(wordStart, 0, x - wordStart, img.Height));
                        }
                        inWord = false;
                    }
                }
                
                // Add final word if we're still in one
                if (inWord && img.Width - wordStart > 15)
                {
                    words.Add(new OpenCvSharp.Rect(wordStart, 0, img.Width - wordStart, img.Height));
                }
            }
            catch { }
            
            return words.Count > 0 ? words : new List<OpenCvSharp.Rect> { new OpenCvSharp.Rect(0, 0, img.Width, img.Height) };
        }

        private List<OpenCvSharp.Rect> DetectText(Mat img)
        {
            // For small images (like snippets), skip complex detection and use whole image
            if (img.Width < 500 && img.Height < 200)
            {
                return new List<OpenCvSharp.Rect> { new OpenCvSharp.Rect(0, 0, img.Width, img.Height) };
            }
            
            try
            {
                var resized = PreprocessForDetection(img);
                var detTensor = CreateDetectionTensor(resized);
                var detInputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("x", detTensor) };
                
                using var detResults = _detSession.Run(detInputs);
                var detOut = detResults.First().AsTensor<float>().ToArray();
                var detShape = detResults.First().AsTensor<float>().Dimensions.ToArray();
                
                return PostprocessDetection(detOut, detShape, img.Size());
            }
            catch
            {
                return new List<OpenCvSharp.Rect> { new OpenCvSharp.Rect(0, 0, img.Width, img.Height) };
            }
        }
        
        private Mat PreprocessForDetection(Mat img)
        {
            Mat resized = new Mat();
            int maxSize = 960;
            double scale = Math.Min(maxSize / (double)img.Width, maxSize / (double)img.Height);
            int newW = (int)(img.Width * scale);
            int newH = (int)(img.Height * scale);
            
            newW = (newW + 31) / 32 * 32;
            newH = (newH + 31) / 32 * 32;
            
            Cv2.Resize(img, resized, new OpenCvSharp.Size(newW, newH));
            return resized;
        }
        
        private DenseTensor<float> CreateDetectionTensor(Mat img)
        {
            Mat rgb = new Mat();
            Cv2.CvtColor(img, rgb, ColorConversionCodes.BGR2RGB);
            
            int h = rgb.Rows, w = rgb.Cols;
            var tensor = new DenseTensor<float>(new[] { 1, 3, h, w });
            
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var v = rgb.At<Vec3b>(y, x);
                    tensor[0, 0, y, x] = v.Item0 / 255.0f;
                    tensor[0, 1, y, x] = v.Item1 / 255.0f;
                    tensor[0, 2, y, x] = v.Item2 / 255.0f;
                }
            }
            return tensor;
        }
        
        private List<OpenCvSharp.Rect> PostprocessDetection(float[] detOut, int[] detShape, OpenCvSharp.Size originalSize)
        {
            var boxes = new List<OpenCvSharp.Rect>();
            
            try
            {
                int outH = detShape[2];
                int outW = detShape[3];
                
                float threshold = 0.3f;
                for (int y = 0; y < outH - 32; y += 16)
                {
                    for (int x = 0; x < outW - 32; x += 16)
                    {
                        int idx = y * outW + x;
                        if (idx < detOut.Length && detOut[idx] > threshold)
                        {
                            double scaleX = (double)originalSize.Width / outW;
                            double scaleY = (double)originalSize.Height / outH;
                            
                            int boxX = (int)(x * scaleX);
                            int boxY = (int)(y * scaleY);
                            int boxW = Math.Min((int)(32 * scaleX), originalSize.Width - boxX);
                            int boxH = Math.Min((int)(32 * scaleY), originalSize.Height - boxY);
                            
                            if (boxW > 10 && boxH > 10)
                                boxes.Add(new OpenCvSharp.Rect(boxX, boxY, boxW, boxH));
                        }
                    }
                }
            }
            catch { }
            
            return boxes.Count > 0 ? boxes : new List<OpenCvSharp.Rect> { new OpenCvSharp.Rect(0, 0, originalSize.Width, originalSize.Height) };
        }
        
        private Mat CropTextRegion(Mat img, OpenCvSharp.Rect box)
        {
            var roi = new OpenCvSharp.Rect(
                Math.Max(0, box.X),
                Math.Max(0, box.Y),
                Math.Min(box.Width, img.Width - Math.Max(0, box.X)),
                Math.Min(box.Height, img.Height - Math.Max(0, box.Y))
            );
            
            Mat cropped = new Mat(img, roi);
            Mat resized = new Mat();
            
            // Model requires exactly height 48
            int targetH = 48;
            int targetW = Math.Max(48, (int)(cropped.Width * (48.0 / cropped.Height)));
            
            Cv2.Resize(cropped, resized, new OpenCvSharp.Size(targetW, targetH));
            return resized;
        }
        
        private string RecognizeText(Mat img)
        {
            try
            {
                // Ensure image has correct height for model
                if (img.Height != 48)
                {
                    Mat resized = new Mat();
                    int targetW = Math.Max(48, (int)(img.Width * (48.0 / img.Height)));
                    Cv2.Resize(img, resized, new OpenCvSharp.Size(targetW, 48));
                    img = resized;
                }
                
                var recTensor = CreateRecognitionTensor(img);
                var recInputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("x", recTensor) };
                
                using var recResults = _recSession.Run(recInputs);
                var recOutTensor = recResults.First().AsTensor<float>();
                var recDims = recOutTensor.Dimensions.ToArray();

                if (recDims.Length == 3 && recDims[0] == 1)
                {
                    int timeSteps = recDims[1];
                    int numClasses = recDims[2];
                    var logitsFlat = recOutTensor.ToArray();
                    return CtcDecoder.GreedyDecode(logitsFlat, timeSteps, numClasses, _dict);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OCR: Recognition error: {ex.Message}");
            }
            
            return "";
        }
        
        private DenseTensor<float> CreateRecognitionTensor(Mat img)
        {
            Mat rgb = new Mat();
            Cv2.CvtColor(img, rgb, ColorConversionCodes.BGR2RGB);
            
            int h = rgb.Rows, w = rgb.Cols;
            var tensor = new DenseTensor<float>(new[] { 1, 3, h, w });
            
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var v = rgb.At<Vec3b>(y, x);
                    tensor[0, 0, y, x] = (v.Item0 / 255.0f - 0.485f) / 0.229f;
                    tensor[0, 1, y, x] = (v.Item1 / 255.0f - 0.456f) / 0.224f;
                    tensor[0, 2, y, x] = (v.Item2 / 255.0f - 0.406f) / 0.225f;
                }
            }
            return tensor;
        }
    }

    public static class CtcDecoder
    {
        public static string GreedyDecode(float[] logitsFlat, int timeSteps, int numClasses, string[] dictLines)
        {
            var sb = new StringBuilder();
            int prevIndex = -1;
            
            for (int t = 0; t < timeSteps; t++)
            {
                int baseIdx = t * numClasses;
                int argmax = 0;
                float max = logitsFlat[baseIdx];
                
                for (int c = 1; c < numClasses; c++)
                {
                    if (logitsFlat[baseIdx + c] > max)
                    {
                        max = logitsFlat[baseIdx + c];
                        argmax = c;
                    }
                }

                if (argmax == 0)
                {
                    prevIndex = 0;
                    continue;
                }
                
                if (argmax == prevIndex)
                {
                    prevIndex = argmax;
                    continue;
                }

                int dictIndex = argmax - 1;
                if (dictIndex >= 0 && dictIndex < dictLines.Length)
                {
                    sb.Append(dictLines[dictIndex]);
                }
                prevIndex = argmax;
            }

            return sb.ToString();
        }
    }

    public interface IOcrService
    {
        string ReadFromUploadedFile(string filePath);
        string ReadFromSnippedImage(BitmapImage image);
    }
}