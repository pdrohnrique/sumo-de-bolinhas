using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton que persiste entre cenas (DontDestroyOnLoad).
/// Guarda qual bolinha cada jogador escolheu (vindo da tela de Seleção)
/// e o resultado da partida (para a tela de Vitória ler).
/// Coloque esse script num GameObject vazio na cena _Boot.
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public BolinhaData BolinhaP1 { get; private set; }
    public BolinhaData BolinhaP2 { get; private set; }

    public int RoundsGanhosP1 { get; private set; }
    public int RoundsGanhosP2 { get; private set; }
    private const int RoundsParaVencer = 2;

    public PlayerIndex? VencedorDaPartida { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void DefinirEscolhas(BolinhaData p1, BolinhaData p2)
    {
        BolinhaP1 = p1;
        BolinhaP2 = p2;
        RoundsGanhosP1 = 0;
        RoundsGanhosP2 = 0;
        VencedorDaPartida = null;
    }

    public void IniciarPartida(string cenaGameplay, string cenaGUI)
    {
        SceneManager.LoadScene(cenaGameplay);
        SceneManager.LoadScene(cenaGUI, LoadSceneMode.Additive);
    }

    /// <summary>Chamado pelo GameManager quando um round termina.</summary>
    public bool RegistrarVitoriaDeRound(PlayerIndex vencedor)
    {
        if (vencedor == PlayerIndex.Player1) RoundsGanhosP1++;
        else RoundsGanhosP2++;

        bool partidaAcabou = RoundsGanhosP1 >= RoundsParaVencer || RoundsGanhosP2 >= RoundsParaVencer;
        if (partidaAcabou)
        {
            VencedorDaPartida = RoundsGanhosP1 > RoundsGanhosP2 ? PlayerIndex.Player1 : PlayerIndex.Player2;
        }
        return partidaAcabou;
    }

    public void IrParaTelaDeVitoria(string cenaVitoria)
    {
        SceneManager.LoadScene(cenaVitoria);
    }

    public void VoltarParaSelecao(string cenaSelecao)
    {
        Instance = null; // permite recriar sessão limpa
        Destroy(gameObject);
        SceneManager.LoadScene(cenaSelecao);
    }
}