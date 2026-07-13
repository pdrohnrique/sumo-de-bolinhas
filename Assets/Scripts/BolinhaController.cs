using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerIndex { Player1 = 0, Player2 = 1 }

/// <summary>
/// MonoBehaviour que controla o comportamento de uma bolinha em jogo (3D).
/// Recebe um asset de BolinhaData para definir seu funcionamento (status).
/// Usa o Input System (Action Maps "Player1"/"Player2") via C# events.
/// Movimento no plano X/Z (visão de cima), gravidade real no eixo Y.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BolinhaController : MonoBehaviour
{
    [Header("Config")]
    public PlayerIndex playerIndex;
    public BolinhaData data; // asset ScriptableObject atribuído na tela de seleção

    [Header("Ajustes da ação de empurrão")]
    [Tooltip("Distância máxima em que a ação tem efeito no inimigo")]
    public float alcanceMaximoAcao = 6f;
    [Tooltip("Distância mínima usada no cálculo, evita força infinita quando colado")]
    public float distanciaMinima = 0.5f;
    [Tooltip("Distância na qual a bolinha ainda recebe a força CHEIA (controla a intensidade geral do empurrão). Aumente pra sentir o empurrão mais forte à distância.")]
    public float distanciaReferenciaForca = 3f; 

    // --- Eventos (Observer pattern) para a UI escutar sem acoplamento direto ---
    public event Action<float> OnCooldownProgressChanged; // 0 = acabou de usar, 1 = pronto
    public event Action<int> OnMoedaColetada; // total de moedas

    // Referência à outra bolinha, atribuída pelo GameManager ao iniciar o round
    [HideInInspector] public BolinhaController alvoInimigo;

    private Rigidbody _rb;
    private PlayerControls _controls;
    private Vector2 _inputMove; // x = eixo X do mundo, y = eixo Z do mundo

    private float _velocidadeAtual;
    private float _forcaAtual;
    private int _moedasColetadas;

    private float _cooldownTimer;
    private bool _podeUsarAcao = true;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = true;
        _rb.freezeRotation = true; // controle direto, sem a bolinha tombar/rolar de forma imprevisível
        _controls = new PlayerControls();
    }

    void OnEnable()
    {
        if (playerIndex == PlayerIndex.Player1)
        {
            _controls.Player1.Enable();
            _controls.Player1.Move.performed += OnMovePerformed;
            _controls.Player1.Move.canceled += OnMoveCanceled;
            _controls.Player1.Ability.performed += OnAbilityPerformed;
        }
        else
        {
            _controls.Player2.Enable();
            _controls.Player2.Move.performed += OnMovePerformed;
            _controls.Player2.Move.canceled += OnMoveCanceled;
            _controls.Player2.Ability.performed += OnAbilityPerformed;
        }
    }

    void OnDisable()
    {
        if (playerIndex == PlayerIndex.Player1)
        {
            _controls.Player1.Move.performed -= OnMovePerformed;
            _controls.Player1.Move.canceled -= OnMoveCanceled;
            _controls.Player1.Ability.performed -= OnAbilityPerformed;
            _controls.Player1.Disable();
        }
        else
        {
            _controls.Player2.Move.performed -= OnMovePerformed;
            _controls.Player2.Move.canceled -= OnMoveCanceled;
            _controls.Player2.Ability.performed -= OnAbilityPerformed;
            _controls.Player2.Disable();
        }
    }

    void Start()
    {
        InicializarComData();
    }

    public void InicializarComData()
    {
        if (data == null) return;

        _velocidadeAtual = data.velocidade;
        _forcaAtual = data.forcaEmpurrao;
        _rb.mass = data.massa;
        transform.localScale = Vector3.one * data.tamanho;
        _moedasColetadas = 0;
        _cooldownTimer = 0f;
        _podeUsarAcao = true;

        var component = GetComponent<Renderer>();
        if (component != null)
        {
            // Usa uma instância do material pra não alterar o asset original
            component.material.color = playerIndex == PlayerIndex.Player1 ? data.corJogador1 : data.corJogador2;
        }
    }

    void Update()
    {
        AtualizarCooldown();
    }

    void FixedUpdate()
    {
        // alvo de velocidade horizontal desejada (X/Z)
        Vector3 desiredVel = new Vector3(_inputMove.x, 0f, _inputMove.y) * _velocidadeAtual;
        Vector3 currentVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        Vector3 velDelta = desiredVel - currentVel;
        // força necessária para alcançar velDelta neste passo físico (F = m * dv / dt)
        Vector3 requiredForce = velDelta * _rb.mass / Time.fixedDeltaTime * data.multiplicadorRespostaMovimento;
        // limita para evitar picos enormes: usa o limite configurável em BolinhaData
        float maxForce = data.forcaMovimentoMaxima;
        if (requiredForce.magnitude > maxForce)
            requiredForce = requiredForce.normalized * maxForce;
        _rb.AddForce(requiredForce, ForceMode.Force);
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        _inputMove = ctx.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        _inputMove = Vector2.zero;
    }

    private void OnAbilityPerformed(InputAction.CallbackContext ctx)
    {
        if (!_podeUsarAcao || alvoInimigo == null) return;
        UsarAcaoDeForca();
    }

    private void UsarAcaoDeForca()
    {
        Vector3 delta = alvoInimigo.transform.position - transform.position;
        delta.y = 0f; // considera só a distância horizontal (X/Z)
        float distancia = delta.magnitude;

        if (distancia > alcanceMaximoAcao) return; // fora de alcance, não faz nada

        Vector3 direcao = delta.normalized;
        float distanciaClamp = Mathf.Max(distancia, distanciaMinima);

        // Força máxima quando a distância <= distanciaReferenciaForca, caindo conforme afasta.
        // Independente de alcanceMaximoAcao (que só controla o corte) e de distanciaMinima (só o piso).
        float magnitude = _forcaAtual * (distanciaReferenciaForca / distanciaClamp);

        Vector3 forcaFinal = direcao * magnitude;
        alvoInimigo.ReceberEmpurrao(forcaFinal);

        _podeUsarAcao = false;
        _cooldownTimer = data.cooldownAcao;
        OnCooldownProgressChanged?.Invoke(0f);
    }

    private void ReceberEmpurrao(Vector3 forca)
    {
        _rb.AddForce(forca, ForceMode.Impulse);
    }

    private void AtualizarCooldown()
    {
        if (_podeUsarAcao) return;

        _cooldownTimer -= Time.deltaTime;
        float progresso = 1f - Mathf.Clamp01(_cooldownTimer / data.cooldownAcao);
        OnCooldownProgressChanged?.Invoke(progresso);

        if (_cooldownTimer <= 0f)
        {
            _podeUsarAcao = true;
            OnCooldownProgressChanged?.Invoke(1f);
        }
    }

    /// <summary>
    /// Chamado pela moeda (Coin.cs) quando essa bolinha coleta uma moeda.
    /// </summary>
    public void ColetarMoeda()
    {
        _moedasColetadas++;
        _rb.mass += data.massaGanhaPorMoeda;
        _forcaAtual += data.forcaGanhaPorMoeda;
        _velocidadeAtual = Mathf.Max(0.5f, _velocidadeAtual - data.velocidadePerdidaPorMoeda);

        OnMoedaColetada?.Invoke(_moedasColetadas);
    }
}