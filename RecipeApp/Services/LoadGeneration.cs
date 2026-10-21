namespace RecipeApp.Services;

/// <summary>Ignore results from an older page visit or account.</summary>
public sealed class LoadGeneration
{
    private long generation;
    public record Ticket(long Generation,string Account);
    public Ticket Begin(string account)=>new(++generation,account);
    public bool IsCurrent(Ticket ticket,string currentAccount)=>ticket.Generation==generation&&ticket.Account==currentAccount;
    public void Invalidate()=>generation++;
}
