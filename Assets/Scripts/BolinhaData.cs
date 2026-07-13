using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NovaBolinha", menuName = "SumoBolinhas/BolinhaData")]
public class BolinhaData : ScriptableObject
{
    [FormerlySerializedAs("NomeBolinha")] [Header("Identidade")]
    public string nomeBolinha;
    public Sprite sprite;

    [Header("Cores (para os 2 jogadores caso escolham a mesma bolinha)")]
    public Color corJogador1 = Color.white;
    public Color corJogador2 = Color.red;

    [Header("Status base")]
    [Tooltip("Velocidade de movimento")]
    public float velocidade = 5f;

    [Tooltip("Tamanho/escala da bolinha (afeta raio de colisão via Transform.localScale)")]
    public float tamanho = 1f;

    [Tooltip("Massa inicial (quanto maior, mais difícil de ser empurrada)")]
    public float massa = 1f;

    [Tooltip("Força base aplicada na bolinha inimiga ao usar a ação")]
    public float forcaEmpurrao = 10f;

    [Tooltip("Tempo (segundos) para a barra de habilidade encher de novo após usar")]
    public float cooldownAcao = 2f;

    [Header("Efeito por moeda coletada (balanceamento)")]
    [Tooltip("Quanto a massa aumenta por moeda coletada (fica mais difícil de jogar longe)")]
    public float massaGanhaPorMoeda = 0.15f;

    [Tooltip("Quanto a força de empurrão aumenta por moeda coletada")]
    public float forcaGanhaPorMoeda = 1.5f;

    [Tooltip("Quanto a velocidade É REDUZIDA por moeda coletada")]
    public float velocidadePerdidaPorMoeda = 0.2f;
    
    [Header("Movimento físico")] 
    [Tooltip("Força máxima aplicada pelo sistema de movimentação (limita aceleração)")]
    public float forcaMovimentoMaxima = 60f;
    
    [Tooltip("Multiplicador de responsividade do movimento (1 = padrão; >1 mais responsivo)")]
    public float multiplicadorRespostaMovimento = 1f;
}