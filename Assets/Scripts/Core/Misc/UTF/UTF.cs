using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using YG;

[System.Serializable]
public class LanguageDirectory
{
    public string ru;
    public string en;
    public string tr;

    public LanguageDirectory(string _ru, string _en, string _tr)
    {
        ru = _ru;
        en = _en;
        tr = _tr;
    }
}

public class UTF : MonoBehaviour
{
    public static UTF manager;

    private void Awake()
    {
        manager = this;
    }

    public static string ReadLanguageDirectory(LanguageDirectory languageDirectory) => manager._ReadLanguageDirectory(languageDirectory);
    public string _ReadLanguageDirectory(LanguageDirectory languageDirectory)
    {
        switch (YG2.envir.language)
        {
            case "ru":
                return languageDirectory.ru;

            case "en":
                return languageDirectory.en;

            case "tr":
                return languageDirectory.tr;
        }

        return null;
    }

    private static readonly SortedDictionary<int, string> abbrevations = new SortedDictionary<int, string>
    {
        {1000,"K"},
        {1000000, "M" },
        {1000000000, "B" }
    };

    public static string AbbreviateNumber(float number)
    {
        for (int i = abbrevations.Count - 1; i >= 0; i--)
        {
            KeyValuePair<int, string> pair = abbrevations.ElementAt(i);
            if (Mathf.Abs(number) >= pair.Key)
            {
                int roundedNumber = Mathf.FloorToInt(number / pair.Key);
                float decimalNumber = (number / pair.Key) - roundedNumber;
                string formattedNumber = string.Format("{0:0.##}", decimalNumber).Substring(1); // ��������� ��� ����� ����� �������
                return roundedNumber.ToString() + formattedNumber + pair.Value;
            }
        }
        return string.Format("{0:0.##}", number);
    }

    public string TextToSprite(string input)
    {
        var output = input;

        Regex pattern = new Regex(@"\\u[a-zA-Z0-9]*\\u[a-zA-Z0-9]*");

        while (output.Contains(@"\u"))
        {
            output = pattern.Replace(output,
                m => {
                    var pair = m.Value;
                    var first = pair.Substring(0, 6);
                    var second = pair.Substring(6, 6);
                    var firstInt = Convert.ToInt32(first.Substring(2), 16);
                    var secondInt = Convert.ToInt32(second.Substring(2), 16);
                    var codePoint = (firstInt - 0xD800) * 0x400 + (secondInt - 0xDC00) + 0x10000;
                    return @"\U" + codePoint.ToString("X8");
                },
                1
            );
        }

        return output;
    }

}