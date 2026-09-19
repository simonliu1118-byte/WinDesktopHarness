namespace WinDesktopHarness;

internal static class AcceptanceMode
{
    private const string EnvironmentName = "WINHARNESS_ACCEPTANCE";

    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable(EnvironmentName), "1", StringComparison.Ordinal);

    public static void Apply(Control root, int rowsPerCategory = 500)
    {
        var rows = BuildRows(rowsPerCategory);
        foreach (var page in FindRecordPages(root))
            page.SetRows(rows);

        if (root is Form form && !form.Text.Contains("驗收模式", StringComparison.Ordinal))
            form.Text += " — 驗收模式";
    }

    private static List<ResultRow> BuildRows(int rowsPerCategory)
    {
        var count = Math.Max(1, rowsPerCategory);
        var now = DateTime.Now;
        var rows = new List<ResultRow>(count * 2);

        for (var i = 0; i < count; i++)
        {
            var createdAt = now.AddMinutes(-i * 17);
            var longName = i % 37 == 0;
            var creationFile = longName
                ? $"測試用非常非常長的中文檔名_用來確認欄位省略與詳細資料顯示_{i + 1:000}.jpg"
                : $"sample-create-{i + 1:000}.jpg";
            var creationFailed = i % 19 == 0;
            rows.Add(new ResultRow(
                creationFile,
                creationFailed ? "失敗" : "完成",
                creationFailed ? "模擬失敗，用於驗收清單篩選與錯誤狀態。" : "模擬完成，用於驗收大量紀錄清單。",
                "建立",
                createdAt,
                $"R-{i + 1:000000}"));

            var verificationFile = longName
                ? $"驗收用超長中文檔名_確認清單捲動搜尋與欄位截斷_{i + 1:000}.png"
                : $"sample-check-{i + 1:000}.png";
            var verificationFailed = i % 13 == 0;
            rows.Add(new ResultRow(
                verificationFile,
                verificationFailed ? "驗證失敗" : "驗證成功",
                verificationFailed ? "模擬失敗結果，用於驗收結果篩選。" : "模擬成功結果，用於驗收大量驗證紀錄。",
                "驗證",
                createdAt.AddMinutes(-3),
                $"Q-{i + 1:000000}"));
        }

        return rows;
    }

    private static IEnumerable<RecordPage> FindRecordPages(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is RecordPage page)
                yield return page;

            foreach (var nested in FindRecordPages(child))
                yield return nested;
        }
    }
}
