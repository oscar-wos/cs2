namespace Sessions;

public interface IDatabaseQueries
{
    string CreateServers { get; }
    string CreateMaps { get; }
    string CreatePlayers { get; }
    string CreateSessions { get; }

    string SelectServer { get; }
    string InsertServer { get; }

    string SelectMap { get; }
    string InsertMap { get; }

    string SelectPlayer { get; }
    string InsertPlayer { get; }

    string InsertSession { get; }
    string UpdateSession { get; }
    string UpdateSeen { get; }
}

public abstract class Queries : IDatabaseQueries
{
    public abstract string CreateServers { get; }
    public abstract string CreateMaps { get; }
    public abstract string CreatePlayers { get; }
    public abstract string CreateSessions { get; }

    public abstract string SelectServer { get; }
    public abstract string InsertServer { get; }

    public abstract string SelectMap { get; }
    public abstract string InsertMap { get; }

    public abstract string SelectPlayer { get; }
    public abstract string InsertPlayer { get; }

    public abstract string InsertSession { get; }
    public abstract string UpdateSession { get; }
    public abstract string UpdateSeen { get; }

    public IEnumerable<string> GetCreateQueries()
    {
        yield return CreateServers;
        yield return CreateMaps;
        yield return CreatePlayers;
        yield return CreateSessions;
    }
}

public class PlayerSQL()
{
    public int Id { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    
    public SessionSQL? Session { get; set; }
}

public class ServerSQL()
{
    public int Id { get; set; }

    public MapSQL? Map { get; set; }
}

public class SessionSQL()
{
    public int Id { get; set; }
}

public class MapSQL()
{
    public short Id { get; set; }
}