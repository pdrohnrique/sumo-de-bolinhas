using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton da cena de Gameplay (3D). Gerencia rounds, detecta bolinha caindo/saindo
/// da arena, e avisa a UI via eventos (Observer) — a UI só se inscreve nos eventos,
/// sem referência direta a esse script.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referências da cena")]
    public BolinhaController bolinhaP1;
    public BolinhaController bolinhaP2;
    public Transform posicaoInicialP1;
    public Transform posicaoInicialP2;

    [Header("Config da arena")]
    [Tooltip("Distância horizontal (X/Z) do centro a partir da qual a bolinha é considerada fora")]
    public float raioLimiteArena = 10f;
    [Tooltip("Altura Y abaixo da qual a bolinha é considerada 'caiu da plataforma'")]
    public float alturaQuedaLimite = -3f;

    [Tooltip("Nome da cena de vitória, para o SceneManager")]
    public string nomeCenaVitoria = "Vitoria";

    // --- Eventos para a UI (Observer) ---
    public event Action<int, int> OnPlacarAtualizado; // (roundsP1, roundsP2)
    public event Action<PlayerIndex> OnRoundTerminou;
    public event Action<PlayerIndex> OnPartidaTerminou;

    private bool _rodadaEmAndamento = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (GameSession.Instance != null)
        {
            bolinhaP1.data = GameSession.Instance.BolinhaP1;
            bolinhaP2.data = GameSession.Instance.BolinhaP2;
        }

        bolinhaP1.alvoInimigo = bolinhaP2;
        bolinhaP2.alvoInimigo = bolinhaP1;

        IniciarRound();
    }

    void Update()
    {
        if (!_rodadaEmAndamento) return;

        if (EstaForaDaArena(bolinhaP1.transform.position))
        {
            TerminarRound(PlayerIndex.Player2); // P1 saiu/caiu, P2 vence o round
        }
        else if (EstaForaDaArena(bolinhaP2.transform.position))
        {
            TerminarRound(PlayerIndex.Player1);
        }
    }

    private bool EstaForaDaArena(Vector3 posicao)
    {
        float distanciaHorizontal = new Vector2(posicao.x, posicao.z).magnitude;
        return distanciaHorizontal > raioLimiteArena || posicao.y < alturaQuedaLimite;
    }

    private void IniciarRound()
    {
        _rodadaEmAndamento = true;

        bolinhaP1.transform.position = posicaoInicialP1.position;
        bolinhaP2.transform.position = posicaoInicialP2.position;
        bolinhaP1.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        bolinhaP2.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        bolinhaP1.InicializarComData();
        bolinhaP2.InicializarComData();

        CoinSpawner.Instance?.LimparTodasAsMoedas();

        if (GameSession.Instance != null)
        {
            OnPlacarAtualizado?.Invoke(GameSession.Instance.RoundsGanhosP1, GameSession.Instance.RoundsGanhosP2);
        }
    }

    private void TerminarRound(PlayerIndex vencedorDoRound)
    {
        _rodadaEmAndamento = false;
        OnRoundTerminou?.Invoke(vencedorDoRound);

        bool partidaAcabou = GameSession.Instance != null &&
            GameSession.Instance.RegistrarVitoriaDeRound(vencedorDoRound);

        if (GameSession.Instance != null)
        {
            OnPlacarAtualizado?.Invoke(GameSession.Instance.RoundsGanhosP1, GameSession.Instance.RoundsGanhosP2);
        }

        if (partidaAcabou)
        {
            OnPartidaTerminou?.Invoke(vencedorDoRound);
            StartCoroutine(IrParaVitoriaComDelay());
        }
        else
        {
            StartCoroutine(ProximoRoundComDelay());
        }
    }

    private IEnumerator ProximoRoundComDelay()
    {
        yield return new WaitForSeconds(2f);
        IniciarRound();
    }

    private IEnumerator IrParaVitoriaComDelay()
    {
        yield return new WaitForSeconds(2f);
        GameSession.Instance.IrParaTelaDeVitoria(nomeCenaVitoria);
    }
}