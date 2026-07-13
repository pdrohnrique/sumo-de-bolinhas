using UnityEngine;
using TMPro;

/// <summary>
/// Cena de Seleção de Bolinhas. Cada jogador navega pela lista de BolinhaData
/// disponíveis (Move esquerda/direita) e confirma com o botão de Ability.
/// Quando os dois confirmam, inicia a partida via GameSession.
/// </summary>
public class BolinhaSelectionManager : MonoBehaviour
{
    [Header("Bolinhas disponíveis (arraste os assets BolinhaData aqui)")]
    public BolinhaData[] bolinhasDisponiveis;

    [Header("UI Jogador 1")]
    public TMP_Text nomeBolinhaP1;
    public TMP_Text statusP1;
    public UnityEngine.UI.Image previewCorP1;
    public GameObject indicadorProntoP1;

    [Header("UI Jogador 2")]
    public TMP_Text nomeBolinhaP2;
    public TMP_Text statusP2;
    public UnityEngine.UI.Image previewCorP2;
    public GameObject indicadorProntoP2;

    [Header("Cenas de destino")]
    public string nomeCenaGameplay = "Gameplay";
    public string nomeCenaGUI = "GUI";

    private int _indexP1;
    private int _indexP2 = 1; // começa em bolinhas diferentes por padrão
    private bool _confirmadoP1;
    private bool _confirmadoP2;

    private PlayerControls _controls;

    void Awake()
    {
        _controls = new PlayerControls();
    }

    void OnEnable()
    {
        _controls.Player1.Enable();
        _controls.Player1.Move.performed += OnMoveP1;
        _controls.Player1.Ability.performed += OnConfirmP1;

        _controls.Player2.Enable();
        _controls.Player2.Move.performed += OnMoveP2;
        _controls.Player2.Ability.performed += OnConfirmP2;
    }

    void OnDisable()
    {
        _controls.Player1.Move.performed -= OnMoveP1;
        _controls.Player1.Ability.performed -= OnConfirmP1;
        _controls.Player1.Disable();

        _controls.Player2.Move.performed -= OnMoveP2;
        _controls.Player2.Ability.performed -= OnConfirmP2;
        _controls.Player2.Disable();
    }

    void Start()
    {
        AtualizarUI();
    }

    private void OnMoveP1(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (_confirmadoP1) return;
        float x = ctx.ReadValue<Vector2>().x;
        if (x > 0.5f) MudarIndex(ref _indexP1, 1);
        else if (x < -0.5f) MudarIndex(ref _indexP1, -1);
        AtualizarUI();
    }

    private void OnMoveP2(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (_confirmadoP2) return;
        float x = ctx.ReadValue<Vector2>().x;
        if (x > 0.5f) MudarIndex(ref _indexP2, 1);
        else if (x < -0.5f) MudarIndex(ref _indexP2, -1);
        AtualizarUI();
    }

    private void OnConfirmP1(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        _confirmadoP1 = !_confirmadoP1;
        AtualizarUI();
        TentarIniciarPartida();
    }

    private void OnConfirmP2(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        _confirmadoP2 = !_confirmadoP2;
        AtualizarUI();
        TentarIniciarPartida();
    }

    private void MudarIndex(ref int index, int direcao)
    {
        if (bolinhasDisponiveis.Length == 0) return;
        index = (index + direcao + bolinhasDisponiveis.Length) % bolinhasDisponiveis.Length;
    }

    private void AtualizarUI()
    {
        if (bolinhasDisponiveis.Length == 0) return;

        BolinhaData dataP1 = bolinhasDisponiveis[_indexP1];
        BolinhaData dataP2 = bolinhasDisponiveis[_indexP2];

        if (nomeBolinhaP1 != null) nomeBolinhaP1.text = dataP1.nomeBolinha;
        if (statusP1 != null) statusP1.text = FormatarStatus(dataP1);
        if (previewCorP1 != null) previewCorP1.color = dataP1.corJogador1;
        if (indicadorProntoP1 != null) indicadorProntoP1.SetActive(_confirmadoP1);

        if (nomeBolinhaP2 != null) nomeBolinhaP2.text = dataP2.nomeBolinha;
        if (statusP2 != null) statusP2.text = FormatarStatus(dataP2);
        if (previewCorP2 != null) previewCorP2.color = dataP2.corJogador2;
        if (indicadorProntoP2 != null) indicadorProntoP2.SetActive(_confirmadoP2);
    }

    private string FormatarStatus(BolinhaData d)
    {
        return $"Velocidade: {d.velocidade}\nForça: {d.forcaEmpurrao}\nMassa: {d.massa}\nTamanho: {d.tamanho}";
    }

    private void TentarIniciarPartida()
    {
        if (!_confirmadoP1 || !_confirmadoP2) return;

        GarantirGameSessionExiste();

        GameSession.Instance.DefinirEscolhas(bolinhasDisponiveis[_indexP1], bolinhasDisponiveis[_indexP2]);
        GameSession.Instance.IniciarPartida(nomeCenaGameplay, nomeCenaGUI);
    }

    /// <summary>
    /// Proteção: se essa cena for testada diretamente (sem passar pela _Boot),
    /// o GameSession ainda não existiria. Isso cria um na hora, evitando NullReferenceException.
    /// </summary>
    private void GarantirGameSessionExiste()
    {
        if (GameSession.Instance != null) return;

        GameObject obj = new GameObject("GameSession");
        obj.AddComponent<GameSession>();
    }
}