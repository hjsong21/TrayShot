using System;
using System.Collections.Generic;
using System.Globalization;
using Sukurini.Models;

namespace Sukurini.Gallery;

/// <summary>
/// Represents a group of screenshots taken on the same calendar date.
/// </summary>
public class DateGroup
{
    public DateTime Date { get; }
    public string Label { get; }
    public List<Screenshot> Items { get; }

    public DateGroup(DateTime date, DateTime today, List<Screenshot> items)
    {
        Date = date;
        Items = items;
        Label = BuildLabel(date, today);
    }

    private static string BuildLabel(DateTime date, DateTime today)
    {
        if (date == today)
            return "오늘";
        if (date == today.AddDays(-1))
            return "어제";

        // Use Korean locale for day-of-week display
        var culture = new CultureInfo("ko-KR");
        string dayOfWeek = culture.DateTimeFormat.GetDayName(date.DayOfWeek);

        // Current year: show "M월 d일 요일", prior year: show "yyyy년 M월 d일"
        if (date.Year == today.Year)
            return $"{date.Month}월 {date.Day}일 {dayOfWeek}";
        else
            return $"{date.Year}년 {date.Month}월 {date.Day}일";
    }
}
