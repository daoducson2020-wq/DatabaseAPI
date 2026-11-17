using MySqlConnector;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

string connectionString =
"Server=127.0.0.1;Port=3306;User ID=root;Password=root;Database=inventory_battle" ?? throw new InvalidOperationException("Missing MySQL connection string.");
var leaderboard = new List<RecordEntry>
{
    new("Harry Potter", 350, 10),
    new("Ron Weasly", 150, 7),
    new("Hermione Granger", 975, 14)
};

app.MapGet("/api/leaderboard", () =>
{
    var players = new List<object>();
    using var connection = new MySqlConnection(connectionString);
    connection.Open();

    string sql = """
        select player_name, inventory_value, floors_climbed
        from leaderboard
        order by floors_climbed desc, inventory_value desc
        limit 5;
        """;

    using var command = new MySqlCommand(sql, connection);
    using var reader = command.ExecuteReader();

    while (reader.Read())
    {
        players.Add(new
        {
            playerName = reader.GetString(0),
            inventoryValue = reader.GetInt32(1),
            floorsClimbed = reader.GetInt32(2),
        });
    }

    return Results.Ok(new { players });
});

app.MapPost("/api/records", (RecordEntry record) =>
{
    if (string.IsNullOrWhiteSpace(record.PlayerName) || record.InventoryValue < 0 || record.FloorsClimbed < 0)
    {
        return Results.BadRequest(new { error = "Invalid record data" });
    }

    using var connection = new MySqlConnection(connectionString);
    connection.Open();
    string sql = """
        INSERT INTO leaderboard (player_name, inventory_value, floors_climbed)
        VALUES (@playerName, @inventoryValue, @floorsClimbed)
        """;

    using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@playerName", record.PlayerName.Trim());
    command.Parameters.AddWithValue("@inventoryValue", record.InventoryValue);
    command.Parameters.AddWithValue("@floorsClimbed", record.FloorsClimbed);
    command.ExecuteNonQueryAsync();

    return Results.Json(new { message = "Record saved" }, statusCode: 201);
});
app.Run("http://localhost:9999");
record RecordEntry(string PlayerName, int InventoryValue, int FloorsClimbed);
