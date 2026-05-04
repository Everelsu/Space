using Npgsql;

public class DatabaseService
{
    private const string ConnString =
        "Host=aws-1-ap-northeast-2.pooler.supabase.com;" +
        "Port=6543;" +
        "Database=postgres;" +
        "Username=postgres.ngvycmxfxipceazxgxnz;" +
        "Password=d2KkcSEzqzx3yaqv;" +
        "SSL Mode=Require;" +
        "Trust Server Certificate=true;";

    public static NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(ConnString);
    }
}