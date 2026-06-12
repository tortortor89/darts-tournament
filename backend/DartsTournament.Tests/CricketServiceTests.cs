using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;
using DartsTournament.Api.Services;

namespace DartsTournament.Tests;

public class CricketServiceTests
{
    private const int Player1 = 1;
    private const int Player2 = 2;

    private readonly CricketService _service = new();

    private CricketGameState NewState() => _service.InitializeState(Player1, Player2);

    private void CloseAllTargets(CricketGameState state, int playerId, int opponentId)
    {
        var hits = new List<CricketHit>
        {
            new(15, 3), new(16, 3), new(17, 3), new(18, 3), new(19, 3), new(20, 3), new(25, 3)
        };
        _service.ProcessTurn(state, playerId, opponentId, hits);
    }

    // --- ProcessTurn : fermeture et scoring ---

    [Fact]
    public void ProcessTurn_TroisMarques_FermeLaCibleSansMarquerDePoints()
    {
        var state = NewState();

        var results = _service.ProcessTurn(state, Player1, Player2, new List<CricketHit> { new(20, 3) });

        Assert.True(results.Single().ClosedTarget);
        Assert.Equal(0, results.Single().PointsScored);
        Assert.Equal(3, state.PlayerStates[Player1].TargetHits[20]);
    }

    [Fact]
    public void ProcessTurn_MarquesEnExces_MarqueDesPointsSiAdversaireOuvert()
    {
        var state = NewState();

        // 5 marques sur le 20 : fermé + 2 marques en excès = 40 points
        var results = _service.ProcessTurn(state, Player1, Player2, new List<CricketHit> { new(20, 5) });

        Assert.Equal(40, results.Single().PointsScored);
        Assert.Equal(40, state.PlayerStates[Player1].Score);
    }

    [Fact]
    public void ProcessTurn_MarquesEnExces_NeMarquePasSiAdversaireFerme()
    {
        var state = NewState();
        _service.ProcessTurn(state, Player2, Player1, new List<CricketHit> { new(20, 3) });

        var results = _service.ProcessTurn(state, Player1, Player2, new List<CricketHit> { new(20, 5) });

        Assert.Equal(0, results.Single().PointsScored);
        Assert.Equal(0, state.PlayerStates[Player1].Score);
    }

    [Fact]
    public void ProcessTurn_ExcesSurLeBull_Marque25ParMarque()
    {
        var state = NewState();

        // 5 marques sur le Bull : fermé + 2 en excès = 50 points
        var results = _service.ProcessTurn(state, Player1, Player2, new List<CricketHit> { new(25, 5) });

        Assert.Equal(50, results.Single().PointsScored);
    }

    [Fact]
    public void ProcessTurn_EtatCumuleEntreLesVisites()
    {
        var state = NewState();

        _service.ProcessTurn(state, Player1, Player2, new List<CricketHit> { new(20, 2) });
        var results = _service.ProcessTurn(state, Player1, Player2, new List<CricketHit> { new(20, 2) });

        // 2 + 2 marques : fermé à la 4e, 1 en excès = 20 points
        Assert.True(results.Single().ClosedTarget);
        Assert.Equal(20, results.Single().PointsScored);
    }

    // --- HasPlayerWonLeg ---

    [Fact]
    public void HasPlayerWonLeg_ToutFermeScoreEgal_Gagne()
    {
        var state = NewState();
        CloseAllTargets(state, Player1, Player2);

        // Scores égaux (0 - 0)
        Assert.True(_service.HasPlayerWonLeg(state, Player1, Player2));
    }

    [Fact]
    public void HasPlayerWonLeg_ToutFermeScoreInferieur_NeGagnePas()
    {
        var state = NewState();
        // L'adversaire marque 40 points sur le 20 avant que Player1 ne le ferme
        _service.ProcessTurn(state, Player2, Player1, new List<CricketHit> { new(20, 5) });
        CloseAllTargets(state, Player1, Player2);

        Assert.False(_service.HasPlayerWonLeg(state, Player1, Player2));
    }

    [Fact]
    public void HasPlayerWonLeg_CiblesNonFermees_NeGagnePas()
    {
        var state = NewState();
        _service.ProcessTurn(state, Player1, Player2, new List<CricketHit> { new(20, 3), new(19, 3) });

        Assert.False(_service.HasPlayerWonLeg(state, Player1, Player2));
    }

    // --- ValidateTurn : faisabilité en 3 fléchettes ---

    [Fact]
    public void ValidateTurn_TroisTriples_Valide()
    {
        _service.ValidateTurn(new List<CricketHit> { new(20, 3), new(19, 3), new(18, 3) });
        _service.ValidateTurn(new List<CricketHit> { new(20, 9) });
    }

    [Fact]
    public void ValidateTurn_SixMarquesSurLeBull_Valide()
    {
        // 3 double-bulls
        _service.ValidateTurn(new List<CricketHit> { new(25, 6) });
    }

    [Fact]
    public void ValidateTurn_SeptMarquesSurLeBull_Rejete()
    {
        // Le Bull ne peut pas être triplé : 7 marques exigeraient 4 fléchettes
        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateTurn(new List<CricketHit> { new(25, 7) }));
    }

    [Fact]
    public void ValidateTurn_ComboImpossibleEnTroisFlechettes_Rejete()
    {
        // 4 + 4 + 1 marques = 2 + 2 + 1 = 5 fléchettes minimum
        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateTurn(new List<CricketHit> { new(20, 4), new(19, 4), new(18, 1) }));
    }

    [Fact]
    public void ValidateTurn_QuatreCibles_Rejete()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateTurn(new List<CricketHit> { new(20, 1), new(19, 1), new(18, 1), new(17, 1) }));
    }

    [Fact]
    public void ValidateTurn_CibleInvalide_Rejete()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateTurn(new List<CricketHit> { new(14, 1) }));
    }

    [Fact]
    public void ValidateTurn_VisiteVide_Valide()
    {
        // 3 fléchettes ratées
        _service.ValidateTurn(new List<CricketHit>());
    }
}
