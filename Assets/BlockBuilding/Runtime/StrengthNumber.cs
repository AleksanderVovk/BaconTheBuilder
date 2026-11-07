using System;
using System.Globalization;
using BigInteger = System.Numerics.BigInteger;

public static class StrengthNumber
{
    public static bool TryParse(string text, out BigInteger value)
    {
        return BigInteger.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value.Sign >= 0;
    }

    public static string Serialize(BigInteger value)
    {
        return BigInteger.Max(BigInteger.Zero, value).ToString(CultureInfo.InvariantCulture);
    }

    public static string Format(BigInteger value)
    {
        if (value < 1000000000000L)
            return value.ToString("N0", CultureInfo.InvariantCulture);
        string digits = Serialize(value);
        string fraction = digits.Substring(1, 2).TrimEnd('0');
        return digits.Substring(0, 1) + (fraction.Length > 0
            ? "." + fraction
            : "") + "e" + (digits.Length - 1).ToString(CultureInfo.InvariantCulture);
    }

    public static int LegacyValue(BigInteger value)
    {
        return (int)BigInteger.Min(int.MaxValue, BigInteger.Max(BigInteger.Zero, value));
    }

    public static BigInteger SquareRoot(BigInteger value)
    {
        if (value.Sign <= 0)
            return BigInteger.Zero;
        // Newton iteration depends on the number of digits, not on player level.
        BigInteger root = BigInteger.One << ((value.ToByteArray().Length * 8 + 1) / 2);
        while (true)
        {
            BigInteger next = (root + value / root) >> 1;
            if (next >= root)
                return root;
            root = next;
        }
    }
}
