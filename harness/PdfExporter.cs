using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;

namespace WinDesktopHarness;

internal static class PdfExporter
{
    private const int PageWidth = 1240;
    private const int PageHeight = 1754;
    private const int Margin = 86;
    private const int RowsPerPage = 18;

    public static void Write(string path, IReadOnlyList<ResultRow> sourceRows)
    {
        var rows = sourceRows.Count == 0
            ? new List<ResultRow> { new("sample-file.dat", "完成", "此列為模擬內容，用於確認 PDF 排版。") }
            : sourceRows.ToList();

        var pageImages = new List<byte[]>();
        var pageCount = Math.Max(1, (int)Math.Ceiling(rows.Count / (double)RowsPerPage));
        for (var page = 0; page < pageCount; page++)
        {
            var pageRows = rows.Skip(page * RowsPerPage).Take(RowsPerPage).ToList();
            pageImages.Add(RenderPage(pageRows, page + 1, pageCount, rows.Count));
        }

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
                    throw new InvalidDataException("輸出的檔案不是有效的 PDF。 ");
                }
            }

            File.Move(tempPath, path, true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static byte[] RenderPage(IReadOnlyList<ResultRow> rows, int pageNumber, int pageCount, int totalRows)
    {
        using var bitmap = new Bitmap(PageWidth, PageHeight, PixelFormat.Format24bppRgb);
        bitmap.SetResolution(150, 150);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.White);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var titleFont = new Font("Microsoft JhengHei UI", 24f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var subtitleFont = new Font("Microsoft JhengHei UI", 14f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var labelFont = new Font("Microsoft JhengHei UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var bodyFont = new Font("Microsoft JhengHei UI", 13f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var bodyBold = new Font("Microsoft JhengHei UI", 13f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var smallFont = new Font("Microsoft JhengHei UI", 10f, FontStyle.Regular, GraphicsUnit.Pixel);

        using var textBrush = new SolidBrush(Color.FromArgb(31, 41, 55));
        using var mutedBrush = new SolidBrush(Color.FromArgb(102, 112, 133));
        using var accentBrush = new SolidBrush(Color.FromArgb(46, 74, 113));
        using var softBrush = new SolidBrush(Color.FromArgb(246, 248, 250));
        using var borderPen = new Pen(Color.FromArgb(222, 226, 232), 1f);
        using var accentPen = new Pen(Color.FromArgb(46, 74, 113), 4f);

        var y = Margin;
        g.DrawString("桌面程式測試報告", titleFont, textBrush, Margin, y);
        y += 44;
        g.DrawString("此文件僅用於驗證介面、中文排版與 PDF 匯出流程。", subtitleFont, mutedBrush, Margin, y);
        y += 52;
        g.DrawLine(accentPen, Margin, y, PageWidth - Margin, y);
        y += 34;

        var summaryRect = new Rectangle(Margin, y, PageWidth - Margin * 2, 100);
        g.FillRectangle(softBrush, summaryRect);
        g.DrawRectangle(borderPen, summaryRect);
        g.DrawString("測試摘要", labelFont, mutedBrush, summaryRect.Left + 22, summaryRect.Top + 16);
        g.DrawString($"模擬結果 {totalRows} 筆", bodyBold, textBrush, summaryRect.Left + 22, summaryRect.Top + 47);
        g.DrawString(DateTime.Now.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture), bodyFont, textBrush,
            summaryRect.Left + 370, summaryRect.Top + 47);
        y += 132;

        var tableLeft = Margin;
        var tableWidth = PageWidth - Margin * 2;
        var colFile = 330;
        var colStatus = 150;
        var colDetail = tableWidth - colFile - colStatus;
        const int headerHeight = 50;
        const int rowHeight = 66;

        g.FillRectangle(softBrush, tableLeft, y, tableWidth, headerHeight);
        g.DrawRectangle(borderPen, tableLeft, y, tableWidth, headerHeight);
        g.DrawString("檔案", bodyBold, textBrush, tableLeft + 14, y + 15);
        g.DrawString("狀態", bodyBold, textBrush, tableLeft + colFile + 14, y + 15);
        g.DrawString("說明", bodyBold, textBrush, tableLeft + colFile + colStatus + 14, y + 15);
        y += headerHeight;

        foreach (var row in rows)
        {
            var rowRect = new Rectangle(tableLeft, y, tableWidth, rowHeight);
            g.DrawRectangle(borderPen, rowRect);
            DrawEllipsized(g, row.FileName, bodyFont, textBrush,
                new RectangleF(tableLeft + 14, y + 14, colFile - 28, rowHeight - 22));
            DrawEllipsized(g, row.Status, bodyBold,
                row.Status == "失敗" ? Brushes.Firebrick : accentBrush,
                new RectangleF(tableLeft + colFile + 14, y + 14, colStatus - 28, rowHeight - 22));
            DrawEllipsized(g, row.Detail, bodyFont, textBrush,
                new RectangleF(tableLeft + colFile + colStatus + 14, y + 14, colDetail - 28, rowHeight - 22));
            y += rowHeight;
        }

        var footerY = PageHeight - Margin - 30;
        g.DrawLine(borderPen, Margin, footerY - 16, PageWidth - Margin, footerY - 16);
        g.DrawString("Generic Windows desktop UI test document", smallFont, mutedBrush, Margin, footerY);
        var pageText = $"第 {pageNumber} / {pageCount} 頁";
        var size = g.MeasureString(pageText, smallFont);
        g.DrawString(pageText, smallFont, mutedBrush, PageWidth - Margin - size.Width, footerY);

        using var stream = new MemoryStream();
        var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
        bitmap.Save(stream, codec, parameters);
        return stream.ToArray();
    }

    private static void DrawEllipsized(Graphics g, string text, Font font, Brush brush, RectangleF rect)
    {
        using var format = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
            LineAlignment = StringAlignment.Near
        };
        g.DrawString(text, font, brush, rect, format);
    }

    private static void WriteImagePdf(string path, IReadOnlyList<byte[]> jpegPages)
    {
        var objects = new List<byte[]>();
        var pageObjectIds = new List<int>();
        var imageObjectIds = new List<int>();

        objects.Add(Array.Empty<byte>()); // 1: catalog, filled after page tree id is known.
        objects.Add(Array.Empty<byte>()); // 2: pages tree.

        for (var i = 0; i < jpegPages.Count; i++)
        {
            var pageId = objects.Count + 1;
            pageObjectIds.Add(pageId);
            objects.Add(Array.Empty<byte>());

            var imageId = objects.Count + 1;
            imageObjectIds.Add(imageId);
            objects.Add(Array.Empty<byte>());

            var contentId = objects.Count + 1;
            var content = $"q\n595 0 0 842 0 0 cm\n/Im{i + 1} Do\nQ\n";
            objects.Add(StreamObject(Encoding.ASCII.GetBytes(content), ""));

            var image = jpegPages[i];
            var imageDict = $"/Type /XObject /Subtype /Image /Width {PageWidth} /Height {PageHeight} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode ";
            objects[imageId - 1] = StreamObject(image, imageDict);

            var page = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /XObject << /Im{i + 1} {imageId} 0 R >> >> /Contents {contentId} 0 R >>";
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
        {
            WriteAscii(output, $"{offsets[i]:0000000000} 00000 n \n");
        }
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
