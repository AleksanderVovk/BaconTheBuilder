using UnityEngine;

public class LangLIst : MonoBehaviour
{
    public static LangLIst instance;

    public LanguageDirectory power;
    public LanguageDirectory power_b;

    public static readonly LanguageDirectory NotEnoughFunds =
    new LanguageDirectory(
        "Недостаточно средств!",
        "Insufficient funds!",
        "Yetersiz bakiye!"
    );

    #region pickaxe
    public static LanguageDirectory Bought =
       new LanguageDirectory(
           "Куплено",
           "Bought",
           "Satin alindi"
       );

    public static LanguageDirectory Equipped =
        new LanguageDirectory(
            "Экипировано",
            "Equipped",
            "Kusanildi"
        );

    public static LanguageDirectory Buy =
        new LanguageDirectory(
            "Купить",
            "Buy",
            "Satin al"
        );

    public static LanguageDirectory RequiredWins(long wins)
    {
        return new LanguageDirectory(
            $"Требуется {wins} побед",
            $"Requires {wins} wins",
            $"{wins} galibiyet gerekli"
        );
    }
    #endregion

    private void Awake()
    {
        instance = this;
    }
}
