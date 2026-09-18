using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;

namespace WinDesktopHarness;

internal static class PdfExporter
{
    // ISO A4 at 300 DPI: 210 x 297 mm.
    private const int PageWidth = 2480;
    private const int PageHeight = 3508;
    private const int Margin = 210;

    // A4 in PDF points (72 pt/inch).
    private const string A4MediaBox = "0 0 595.28 841.89";

    public static void Write(string path, IReadOnlyList<ResultRow> sourceRows)
    {
        var rows = sourceRows.Count == 0
            ? new List<ResultRow> { new("sample-file.dat", "完成", "此內容為模擬資料，用於確認 PDF 版面。") }
            : sourceRows.ToList();

        // The public harness intentionally emits one readable report page per
        // result instead of a dense database-style table.
        var pageImages = rows.Select((row, index) => RenderPage(row, index + 1, rows.Count)).ToList();
        var tempPath = path + ".tmp";
        try
        {
            WriteImagePdf(tempPath, pageImages);
            using (var check = File.OpenRead(tempPath))
            {
                Span<byte> header = stackalloc byte[4];
                if (check.Read(header) != 4 || header[0] != (byte)'%' || header[1] != (byte)'P' ||
                    header[2] != (byte)'D' || header[3] != (byte)'F')
                {
                    throw new InvalidDataException("輸出的檔案不是有效的 PDF。");
                }
            }
            File.Move(tempPath, path, true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static byte[] RenderPage(ResultRow row, int pageNumber, int pageCount)
    {
        using var bitmap = new Bitmap(PageWidth, PageHeight, PixelFormat.Format24bppRgb);
        bitmap.SetResolution(300, 300);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.White);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

        using var titleFont = PxFont(82, FontStyle.Bold);
        using var subtitleFont = PxFont(38);
        using var sectionFont = PxFont(42, FontStyle.Bold);
        using var labelFont = PxFont(34, FontStyle.Bold);
        using var bodyFont = PxFont(44);
        using var bodyBold = PxFont(48, FontStyle.Bold);
        using var resultFont = PxFont(58, FontStyle.Bold);
        using var smallFont = PxFont(30);

        using var textBrush = new SolidBrush(Color.FromArgb(31, 41, 55));
        using var mutedBrush = new SolidBrush(Color.FromArgb(102, 112, 133));
        using var accentBrush = new SolidBrush(Color.FromArgb(46, 74, 113));
        using var successBrush = new SolidBrush(Color.FromArgb(33, 130, 92));
        using var warningBrush = new SolidBrush(Color.FromArgb(170, 73, 55));
        using var softBrush = new SolidBrush(Color.FromArgb(247, 249, 252));
        using var successSoft = new SolidBrush(Color.FromArgb(235, 247, 241));
        using var warningSoft = new SolidBrush(Color.FromArgb(252, 240, 237));
        using var borderPen = new Pen(Color.FromArgb(220, 224, 230), 3f);
        using var accentPen = new Pen(Color.FromArgb(46, 74, 113), 10f);

        var contentWidth = PageWidth - Margin * 2;
        var y = Margin;

        g.DrawString("檔案檢查報告", titleFont, textBrush, Margin, y);
        y += 108;
        g.DrawString("Windows 桌面介面測試版", subtitleFont, mutedBrush, Margin, y);
        y += 92;
        g.DrawLine(accentPen, Margin, y, PageWidth - Margin, y);
        y += 92;

        g.DrawString("報告資訊", sectionFont, textBrush, Margin, y);
        y += 76;
        var infoRect = new Rectangle(Margin, y, contentWidth, 330);
        g.FillRectangle(softBrush, infoRect);
        g.DrawRectangle(borderPen, infoRect);
        DrawInfoRow(g, "檔案", row.FileName, infoRect.Left + 48, infoRect.Top + 44,
            contentWidth - 96, labelFont, bodyFont, textBrush, mutedBrush);
        DrawInfoRow(g, "產生時間", DateTime.Now.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture),
            infoRect.Left + 48, infoRect.Top + 154, contentWidth - 96,
            labelFont, bodyFont, textBrush, mutedBrush);
        y += 410;

        g.DrawString("檢查結果", sectionFont, textBrush, Margin, y);
        y += 78;
        var warning = row.Status is "失敗" or "需注意";
        var resultRect = new Rectangle(Margin, y, contentWidth, 620);
        g.FillRectangle(warning ? warningSoft : successSoft, resultRect);
        g.DrawRectangle(borderPen, resultRect);

        var badgeRect = new Rectangle(resultRect.Left + 48, resultRect.Top + 48, 340, 96);
        using (var badgeBrush = new SolidBrush(warning
                   ? Color.FromArgb(170, 73, 55)
                   : Color.FromArgb(33, 130, 92)))
        {
            g.FillRectangle(badgeBrush, badgeRect);
        }
        using var badgeFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(row.Status, bodyBold, Brushes.White, badgeRect, badgeFormat);

        g.DrawString(warning ? "結果需要注意" : "檢查完成", resultFont,
            warning ? warningBrush : successBrush, resultRect.Left + 48, resultRect.Top + 190);

        var detailRect = new RectangleF(resultRect.Left + 48, resultRect.Top + 310,
            resultRect.Width - 96, 235);
        using var wrap = new StringFormat
        {
            Trimming = StringTrimming.EllipsisWord,
            FormatFlags = 0,
            LineAlignment = StringAlignment.Near
        };
        g.DrawString(row.Detail, bodyFont, textBrush, detailRect, wrap);
        y += 720;

        g.DrawString("說明", sectionFont, textBrush, Margin, y);
        y += 76;
        var noteRect = new Rectangle(Margin, y, contentWidth, 430);
        g.FillRectangle(softBrush, noteRect);
        g.DrawRectangle(borderPen, noteRect);
        var noteText = "此文件為介面與文件輸出流程的模擬報告，用來確認 A4 尺寸、中文字級、資訊層級與列印閱讀性。";
        g.DrawString(noteText, bodyFont, textBrush,
            new RectangleF(noteRect.Left + 48, noteRect.Top + 48, noteRect.Width - 96, noteRect.Height - 96), wrap);

        var footerY = PageHeight - Margin - 52;
        g.DrawLine(borderPen, Margin, footerY - 38, PageWidth - Margin, footerY - 38);
        g.DrawString("Desktop UI test document", smallFont, mutedBrush, Margin, footerY);
        var pageText = $"第 {pageNumber} / {pageCount} 頁";
        var pageSize = g.MeasureString(pageText, smallFont);
        g.DrawString(pageText, smallFont, mutedBrush, PageWidth - Margin - pageSize.Width, footerY);

        using var stream = new MemoryStream();
        var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 94L);
        bitmap.Save(stream, codec, parameters);
        return stream.ToArray();
    }

    private static Font PxFont(float pixels, FontStyle style = FontStyle.Regular) =>
        new("Microsoft JhengHei UI", pixels, style, GraphicsUnit.Pixel);

    private static void DrawInfoRow(Graphics g, string label, string value, int x, int y, int width,
        Font labelFont, Font bodyFont, Brush textBrush, Brush mutedBrush)
    {
        g.DrawString(label, labelFont, mutedBrush, x, y);
        g.DrawString(value, bodyFont, textBrush,
            new RectangleF(x + 270, y - 6, width - 270, 82),
            new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
    }

    private static void WriteImagePdf(string path, IReadOnlyList<byte[]> jpegPages)
    {
        var objects = new List<byte[]>();
        var pageObjectIds = new List<int>();
        objects.Add(Array.Empty<byte>()); // catalog
        objects.Add(Array.Empty<byte>()); // pages tree

        for (var i = 0; i < jpegPages.Count; i++)
        {
            var pageId = objects.Count + 1;
            pageObjectIds.Add(pageId);
            objects.Add(Array.Empty<byte>());

            var imageId = objects.Count + 1;
            objects.Add(Array.Empty<byte>());

            var contentId = objects.Count + 1;
            var content = $"q\n595.28 0 0 841.89 0 0 cm\n/Im{i + 1} Do\nQ\n";
            objects.Add(StreamObject(Encoding.ASCII.GetBytes(content), ""));

            var image = jpegPages[i];
            var imageDict = $"/Type /XObject /Subtype /Image /Width {PageWidth} /Height {PageHeight} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode ";
            objects[imageId - 1] = StreamObject(image, imageDict);

            var page = $"<< /Type /Page /Parent 2 0 R /MediaBox [{A4MediaBox}] /Resources << /XObject << /Im{i + 1} {imageId} 0 R >> >> /Contents {contentId} 0 R >>";
            objects[pageId - 1] = Encoding.ASCII.GetBytes(page);
        }

        objects[0] = Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>");
        var kids = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));
        objects[1] = Encoding.ASCII.GetBytes($"<< /Type /Pages /Count {pageObjectIds.Count} /Kids [ {kids} ] >>");

        using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteAscii(output, "%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            WriteAscii(output, $"{i + 1} 0 obj\n");
            output.Write(objects[i]);
            WriteAscii(output, "\nendobj\n");
        }

        var xref = output.Position;
        WriteAscii(output, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(output, "0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
            WriteAscii(output, $"{offsets[i]:0000000000} 00000 n \n");
        WriteAscii(output, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
    }

    private static byte[] StreamObject(byte[] bytes, string dictionaryEntries)
    {
        using var stream = new MemoryStream();
        WriteAscii(stream, $"<< {dictionaryEntries}/Length {bytes.Length} >>\nstream\n");
        stream.Write(bytes);
        WriteAscii(stream, "\nendstream");
        return stream.ToArray();
    }

    private static void WriteAscii(Stream stream, string text)
    {
        var bytes = Encoding.Latin1.GetBytes(text);
        stream.Write(bytes);
    }
}
