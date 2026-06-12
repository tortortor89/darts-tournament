using System.Text.Json;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Tests;

/// <summary>
/// Vérifie que les requêtes JSON du frontend se désérialisent correctement
/// (mêmes options que ASP.NET Core : camelCase, insensible à la casse)
/// </summary>
public class MatchSessionDtoBindingTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void StartRequest_DoubleOutFalse_EstBienLu()
    {
        var json = """{"legsToWin":3,"startingPlayerId":3,"trackDoubles":false,"gameMode":301,"doubleOut":false}""";

        var request = JsonSerializer.Deserialize<StartMatchSessionRequest>(json, WebOptions)!;

        Assert.Equal(GameMode.ThreeOhOne, request.GameMode);
        Assert.False(request.DoubleOut);
        Assert.False(request.TrackDoubles);
    }

    [Fact]
    public void StartRequest_SansDoubleOut_ParDefautTrue()
    {
        var json = """{"legsToWin":3,"startingPlayerId":3,"gameMode":501}""";

        var request = JsonSerializer.Deserialize<StartMatchSessionRequest>(json, WebOptions)!;

        Assert.Equal(GameMode.FiveOhOne, request.GameMode);
        Assert.True(request.DoubleOut);
    }
}
