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
    [Tooltip("Componente vertical extra aplicada no empurrão, pra dar uma leve 'jogada pra cima'")]
    public float componenteVerticalEmpurrao = 2f;

    // --- Eventos (Observer pattern) para a UI escutar sem acoplamento direto ---
    public event Action<float> OnCooldownProgressChanged; // 0 = acabou de usar, 1 = pronto
    public event Action<int> OnMoedaColetada; // total de moedas

    // Referência à outra bolinha, atribuída pelo GameManager ao iniciar o round
    [HideInInspector] public BolinhaController alvoInimigo;

    private Rigidbody rb;
    private PlayerControls controls;
    private Vector2 inputMove; // x = eixo X do mundo, y = eixo Z do mundo

    private float velocidadeAtual;
    private float forcaAtual;
    private int moedasColetadas;

    private float cooldownTimer;
    private bool podeUsarAcao = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.freezeRotation = true; // controle direto, sem a bolinha tombar/rolar de forma imprevisível
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        if (playerIndex == PlayerIndex.Player1)
        {
            controls.Player1.Enable();
            controls.Player1.Move.performed += OnMovePerformed;
            controls.Player1.Move.canceled += OnMoveCanceled;
            controls.Player1.Ability.performed += OnAbilityPerformed;
        }
        else
        {
            controls.Player2.Enable();
            controls.Player2.Move.performed += OnMovePerformed;
            controls.Player2.Move.canceled += OnMoveCanceled;
            controls.Player2.Ability.performed += OnAbilityPerformed;
        }
    }

    void OnDisable()
    {
        if (playerIndex == PlayerIndex.Player1)
        {
            controls.Player1.Move.performed -= OnMovePerformed;
            controls.Player1.Move.canceled -= OnMoveCanceled;
            controls.Player1.Ability.performed -= OnAbilityPerformed;
            controls.Player1.Disable();
        }
        else
        {
            controls.Player2.Move.performed -= OnMovePerformed;
            controls.Player2.Move.canceled -= OnMoveCanceled;
            controls.Player2.Ability.performed -= OnAbilityPerformed;
            controls.Player2.Disable();
        }
    }

    void Start()
    {
        InicializarComData();
    }

    public void InicializarComData()
    {
        if (data == null) return;

        velocidadeAtual = data.velocidade;
        forcaAtual = data.forcaEmpurrao;
        rb.mass = data.massa;
        transform.localScale = Vector3.one * data.tamanho;
        moedasColetadas = 0;
        cooldownTimer = 0f;
        podeUsarAcao = true;

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
        Vector3 movimento = new Vector3(inputMove.x, 0f, inputMove.y) * velocidadeAtual;
        rb.linearVelocity = new Vector3(movimento.x, rb.linearVelocity.y, movimento.z);
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        inputMove = ctx.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        inputMove = Vector2.zero;
    }

    private void OnAbilityPerformed(InputAction.CallbackContext ctx)
    {
        if (!podeUsarAcao || alvoInimigo == null) return;
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
        float magnitude = forcaAtual * (distanciaReferenciaForca / distanciaClamp);

        Vector3 forcaFinal = direcao * magnitude + Vector3.up * componenteVerticalEmpurrao;
        alvoInimigo.ReceberEmpurrao(forcaFinal);

        podeUsarAcao = false;
        cooldownTimer = data.cooldownAcao;
        OnCooldownProgressChanged?.Invoke(0f);
    }

    private void ReceberEmpurrao(Vector3 forca)
    {
        rb.AddForce(forca, ForceMode.Impulse);
    }

    private void AtualizarCooldown()
    {
        if (podeUsarAcao) return;

        cooldownTimer -= Time.deltaTime;
        float progresso = 1f - Mathf.Clamp01(cooldownTimer / data.cooldownAcao);
        OnCooldownProgressChanged?.Invoke(progresso);

        if (cooldownTimer <= 0f)
        {
            podeUsarAcao = true;
            OnCooldownProgressChanged?.Invoke(1f);
        }
    }

    /// <summary>
    /// Chamado pela moeda (Coin.cs) quando essa bolinha coleta uma moeda.
    /// </summary>
    public void ColetarMoeda()
    {
        moedasColetadas++;
        rb.mass += data.massaGanhaPorMoeda;
        forcaAtual += data.forcaGanhaPorMoeda;
        velocidadeAtual = Mathf.Max(0.5f, velocidadeAtual - data.velocidadePerdidaPorMoeda);

        OnMoedaColetada?.Invoke(moedasColetadas);
    }
}