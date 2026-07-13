using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Fica na cena de GUI (carregada de forma aditiva junto com a Gameplay).
/// Não referencia o GameManager/BolinhaController diretamente na lógica deles —
/// só se inscreve nos eventos (Observer pattern) e atualiza a tela.
/// </summary>
public class GameplayUIController : MonoBehaviour
{
    [Header("Placar")]
    public TMP_Text placarP1;
    public TMP_Text placarP2;

    [Header("Barras de cooldown da ação (Image com Fill Amount)")]
    public Image barraCooldownP1;
    public Image barraCooldownP2;

    [Header("Aviso de round (opcional)")]
    public TMP_Text avisoRound;
    public float duracaoAviso = 1.5f;

    private float _timerAviso;

    void OnEnable()
    {
        // GameManager já deve existir (cena de Gameplay carrega antes/junto)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlacarAtualizado += AtualizarPlacar;
            GameManager.Instance.OnRoundTerminou += MostrarAvisoRound;

            GameManager.Instance.bolinhaP1.OnCooldownProgressChanged += AtualizarCooldownP1;
            GameManager.Instance.bolinhaP2.OnCooldownProgressChanged += AtualizarCooldownP2;
        }

        if (avisoRound != null) avisoRound.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlacarAtualizado -= AtualizarPlacar;
            GameManager.Instance.OnRoundTerminou -= MostrarAvisoRound;

            GameManager.Instance.bolinhaP1.OnCooldownProgressChanged -= AtualizarCooldownP1;
            GameManager.Instance.bolinhaP2.OnCooldownProgressChanged -= AtualizarCooldownP2;
        }
    }

    void Update()
    {
        if (_timerAviso > 0f)
        {
            _timerAviso -= Time.deltaTime;
            if (_timerAviso <= 0f && avisoRound != null)
            {
                avisoRound.gameObject.SetActive(false);
            }
        }
    }

    private void AtualizarPlacar(int roundsP1, int roundsP2)
    {
        if (placarP1 != null) placarP1.text = $"P1 - {roundsP1}pts";
        if (placarP2 != null) placarP2.text = $"P2 - {roundsP2}pts";
    }

    private void AtualizarCooldownP1(float progresso)
    {
        if (barraCooldownP1 != null) barraCooldownP1.fillAmount = progresso;
    }

    private void AtualizarCooldownP2(float progresso)
    {
        if (barraCooldownP2 != null) barraCooldownP2.fillAmount = progresso;
    }

    private void MostrarAvisoRound(PlayerIndex vencedorDoRound)
    {
        if (avisoRound == null) return;
        avisoRound.text = vencedorDoRound == PlayerIndex.Player1
            ? "Jogador 1 venceu o round!"
            : "Jogador 2 venceu o round!";
        avisoRound.gameObject.SetActive(true);
        _timerAviso = duracaoAviso;
    }
}