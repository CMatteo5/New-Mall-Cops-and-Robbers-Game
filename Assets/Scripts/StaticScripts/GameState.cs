public static class GameState
{
    public static bool ShopOpen { get; private set; }

    public static void SetShopOpen(bool value)
    {
        ShopOpen = value;
    }
}