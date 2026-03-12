namespace DartsTournament.Api.DTOs;

public record CreatePlayerRequest(string FirstName, string LastName, string? Nickname);
public record UpdatePlayerRequest(string FirstName, string LastName, string? Nickname);
public record PlayerResponse(int Id, string FirstName, string LastName, string? Nickname, DateTime CreatedAt);
