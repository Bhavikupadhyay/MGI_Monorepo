using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

public class EconomyForecastPanel : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text earningsText;
    [SerializeField] private TMP_Text expensesText;
    [SerializeField] private TMP_Text netDeltaText;

    [Header("Linked UI References")]
    [SerializeField] private WeeklyForecastUI weeklyForecastUI;  // 👈 This is the important one

    private readonly EconomyForecastService _forecastService = new EconomyForecastService();

    private void OnEnable()
    {
        RefreshFromForecast();
    }

    public void RefreshFromForecast()
    {
        if (!_forecastService.TryGetSnapshot(out var snapshot))
        {
            Debug.LogWarning("[EconomyForecastPanel] Unable to load economy_forecast.json.");
            return;
        }

        // Update the UI Texts
        if (earningsText) earningsText.text = $"Earnings: {snapshot.earnings:N0}";
        if (expensesText) expensesText.text = $"Expenses: {snapshot.totalExpenses:N0}";
        if (netDeltaText) netDeltaText.text = $"Net Delta: {snapshot.netDelta:+#;-#;0}";

        // Update the top bar forecast
        if (weeklyForecastUI != null)
            weeklyForecastUI.SetForecast(snapshot.netDelta);
    }
}

[Serializable]
internal sealed class EconomyForecastSnapshot
{
    public int week;
    public int currentBalance;
    public int income;
    public int salary;
    public int bonus;
    public int operatingExpense;
    public int earnings;
    public int totalExpenses;
    public int netDelta;
    public int projectedBalance;
}

internal sealed class EconomyForecastService
{
    [Serializable]
    private sealed class EconomyForecastFile
    {
        public string player_id;
        public int current_balance;
        public int weeks;
        public int salary;
        public int income;
        public Dictionary<string, int> bonuses;
        public Dictionary<string, int> expenses;
        public List<ForecastWeek> forecast;
        public List<string> alerts;
    }

    [Serializable]
    private sealed class ForecastWeek
    {
        public int week;
        public int net_change;
        public int balance;
    }

    private const string EconomyFolderName = "Economy";
    private const string ForecastFileName = "economy_forecast.json";
    private const string StreamingAssetsFolderName = "StreamingAssets";

    public bool TryGetSnapshot(out EconomyForecastSnapshot snapshot)
    {
        snapshot = null;

        var path = Path.Combine(Application.dataPath, StreamingAssetsFolderName, EconomyFolderName, ForecastFileName);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            var forecastFile = JsonConvert.DeserializeObject<EconomyForecastFile>(json);
            if (forecastFile == null)
            {
                return false;
            }

            var selectedWeek = forecastFile.forecast?.OrderBy(entry => entry.week).FirstOrDefault();
            var weekNumber = selectedWeek?.week ?? 1;
            var weekKey = weekNumber.ToString();
            var bonus = ReadWeekValue(forecastFile.bonuses, weekKey);
            var operatingExpense = ReadWeekValue(forecastFile.expenses, weekKey);
            var earnings = forecastFile.income + bonus;
            var totalExpenses = forecastFile.salary + operatingExpense;

            snapshot = new EconomyForecastSnapshot
            {
                week = weekNumber,
                currentBalance = forecastFile.current_balance,
                income = forecastFile.income,
                salary = forecastFile.salary,
                bonus = bonus,
                operatingExpense = operatingExpense,
                earnings = earnings,
                totalExpenses = totalExpenses,
                netDelta = selectedWeek?.net_change ?? (earnings - totalExpenses),
                projectedBalance = selectedWeek?.balance ?? (forecastFile.current_balance + earnings - totalExpenses)
            };

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EconomyForecastService] Failed to read forecast JSON: {ex.Message}");
            return false;
        }
    }

    private static int ReadWeekValue(Dictionary<string, int> values, string weekKey)
    {
        if (values == null || string.IsNullOrWhiteSpace(weekKey))
        {
            return 0;
        }

        return values.TryGetValue(weekKey, out var value) ? value : 0;
    }
}
