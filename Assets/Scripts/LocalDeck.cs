public class LocalDeck
{
#pragma warning disable IDE0044 // Add readonly modifier
    private System.Action callback;
#pragma warning restore IDE0044 // Add readonly modifier

    public Card[] value;
    public Card[] Value
    {
        get
        {
            return value;
        }
        set
        {
            this.value = value;
            callback();
        }
    }

    public LocalDeck(Card[] deck, System.Action callback)
    {
        value = deck;
        this.callback = callback;
    }
}
