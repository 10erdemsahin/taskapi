namespace AdventureGame
{
    /// <summary>
    /// NPC'nin prompt'una eklenecek güncel oyun durumu (ör. "Yankı Taşı: 3/5").
    /// GameManager uygular; NPC GameManager'ı doğrudan tanımaz (DIP).
    /// </summary>
    public interface INpcContextProvider
    {
        /// <summary>Boş dönebilir. Her LLM isteğinden hemen önce çağrılır.</summary>
        string GetNpcContext();
    }
}
