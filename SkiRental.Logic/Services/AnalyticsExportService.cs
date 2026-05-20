using System.Text;
using SkiRental.Domain.Models;

namespace SkiRental.Logic.Services;

/// <summary>Exports utilization reports to CSV (Excel-compatible).</summary>
public sealed class AnalyticsExportService
{
    public byte[] ExportUtilizationToCsv(IReadOnlyList<UtilizationReport> reports)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SerialNumber,Description,Category,UtilizationPercent,RentedHours,Revenue,Status");
        foreach (var r in reports)
        {
            sb.AppendLine(string.Join(",",
                Escape(r.SerialNumber),
                Escape(r.EquipmentDescription),
                Escape(r.Category.ToString()),
                (r.UtilizationRate * 100).ToString("F2"),
                r.TotalRentedHours.ToString("F2"),
                r.Revenue.ToString("F2"),
                StatusLabel(r.UtilizationRate)));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string StatusLabel(double rate) =>
        rate < 0.30 ? "Niska aktywnosc" : rate < 0.60 ? "Przeciętna" : "Wysoka aktywnosc";

    private static string Escape(string? v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        if (v.Contains(',') || v.Contains('"'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }
}
