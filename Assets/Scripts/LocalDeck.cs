public class LocalDeck
{
    private System.Action callback;

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
