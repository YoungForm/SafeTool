namespace SafeTool.Application.Services;

public class ModelComputeService
{
    private readonly ComponentLibraryService _lib;
    public ModelComputeService(ComponentLibraryService lib) { _lib = lib; }

    public object Compute(ProjectModelService.Function f)
    {
        int i = f.Model.I.Count, l = f.Model.L.Count, o = f.Model.O.Count;
        bool redundant = new[] { i, l, o }.Any(n => n >= 2);
        var method = f.Options?.GetValueOrDefault("AnnexKMethod") ?? "simplified";
        var testEquip = (f.Options?.GetValueOrDefault("testEquip") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
        string cat = f.Standard.Equals("ISO13849", StringComparison.OrdinalIgnoreCase) || f.Standard.Equals("both", StringComparison.OrdinalIgnoreCase)
            ? (redundant ? "Cat3" : (testEquip ? "Cat2" : "Cat1"))
            : "N/A";
        double pfhd = 0.0;
        double dcSum = 0.0; int dcCount = 0;
        int seriesI = f.Model.I.Count; int seriesL = f.Model.L.Count; int seriesO = f.Model.O.Count;
        string monI = f.Options?.GetValueOrDefault("I.monitor") ?? "none";
        string monL = f.Options?.GetValueOrDefault("L.monitor") ?? "none";
        string monO = f.Options?.GetValueOrDefault("O.monitor") ?? "none";
        string drI = f.Options?.GetValueOrDefault("I.demandRate") ?? "";
        string drL = f.Options?.GetValueOrDefault("L.demandRate") ?? "";
        string drO = f.Options?.GetValueOrDefault("O.demandRate") ?? "";
        foreach (var d in f.Model.I.Concat(f.Model.L).Concat(f.Model.O))
        {
            var rec = _lib.Get(d.Id);
            if (rec?.Parameters != null)
            {
                var v = rec.Parameters.GetValueOrDefault("PFHd") ?? rec.Parameters.GetValueOrDefault("pfhd") ?? "";
                if (double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var num)) pfhd += num;
                var dc = rec.Parameters.GetValueOrDefault("DCavg") ?? rec.Parameters.GetValueOrDefault("DCcapability") ?? "";
                if (double.TryParse(dc, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dnum)) { dcSum += dnum; dcCount++; }
            }
            if (d.OverrideParams != null && d.OverrideParams.TryGetValue("PFHd", out var ov) && double.TryParse(ov, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var onum))
                pfhd += onum;
            if (d.OverrideParams != null && d.OverrideParams.TryGetValue("DCavg", out var odc) && double.TryParse(odc, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var odnum)) { dcSum += odnum; dcCount++; }
        }
        double baseDc = dcCount > 0 ? (dcSum / Math.Max(1, dcCount)) : (testEquip ? 0.6 : 0.0);
        if (method.Equals("simplified", StringComparison.OrdinalIgnoreCase)) baseDc = Math.Min(0.9, baseDc);
        double seriesFactor = Math.Pow(0.95, Math.Max(0, seriesI - 1)) * Math.Pow(0.95, Math.Max(0, seriesL - 1)) * Math.Pow(0.95, Math.Max(0, seriesO - 1));
        double monitorBoostI = monI == "diagnostics" ? 0.1 : monI == "test" ? 0.2 : 0.0;
        double monitorBoostL = monL == "diagnostics" ? 0.1 : monL == "test" ? 0.2 : 0.0;
        double monitorBoostO = monO == "diagnostics" ? 0.1 : monO == "test" ? 0.2 : 0.0;
        double boost = (monitorBoostI + monitorBoostL + monitorBoostO) / 3.0;
        double dcavg = Math.Max(0, Math.Min(1, (baseDc + boost) * seriesFactor));
        var warnings = new List<string>();
        if (f.Standard.Equals("IEC62061", StringComparison.OrdinalIgnoreCase) || f.Standard.Equals("both", StringComparison.OrdinalIgnoreCase))
        {
            if (pfhd <= 0) warnings.Add("PFHd 未提供，请在设备或覆盖参数中填写 PFHd");
        }
        if (!redundant && cat == "Cat3") warnings.Add("建议类别为 Cat3，但未检测到冗余通道");
        if (!testEquip && cat == "Cat2") warnings.Add("建议类别为 Cat2，但未勾选测试设备");
        if ((monI == "none" || monL == "none" || monO == "none") && dcavg < 0.6) warnings.Add("通道未启用监测，DCavg 估算较低");
        if (!string.IsNullOrWhiteSpace(drI) || !string.IsNullOrWhiteSpace(drL) || !string.IsNullOrWhiteSpace(drO)) warnings.Add("需求率已设置，请在SRS中确认测试与维护策略");
        return new { deviceCount = i + l + o, redundant, categorySuggestion = cat, pfhdSum = pfhd, dcavgEst = dcavg, method, warnings };
    }
}
