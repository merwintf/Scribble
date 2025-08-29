using Tesseract;

// 1) Preprocess your image first (resize to ~300–600 DPI, grayscale, threshold, deskew) -> save to tempPath

var tessdataPath = @"./tessdata";             // contains eng.traineddata (prefer tessdata_best)
var langs = "eng";                             // e.g., "eng", or "eng+osd" if you need orientation
using var engine = new TesseractEngine(tessdataPath, langs, EngineMode.LstmOnly);

// Helpful configs (only set what’s relevant)
engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_.:/");
engine.SetVariable("load_system_dawg", "F");   // good for codes
engine.SetVariable("load_freq_dawg", "F");
engine.SetVariable("preserve_interword_spaces", "1");

using var img = Pix.LoadFromFile(tempPath);

// Pick the right PSM for your layout:
// 6 = uniform block, 7 = single line, 11/12 = sparse text
using var page = engine.Process(img, PageSegMode.SingleBlock);
string text = page.GetText();
float meanConf = page.GetMeanConfidence();     // 0.0 – 1.0

Console.WriteLine($"Conf: {meanConf:F2}");
Console.WriteLine(text);
