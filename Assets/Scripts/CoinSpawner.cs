using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton responsável por gerar moedas aleatoriamente na arena (plano X/Z) ao longo do tempo.
/// </summary>
public class CoinSpawner : MonoBehaviour
{
    public static CoinSpawner Instance { get; private set; }

    [Header("Config de spawn")]
    public GameObject coinPrefab;
    [Tooltip("Raio da arena (no plano X/Z) onde as moedas podem aparecer")]
    public float raioArena = 8f;
    [Tooltip("Altura (Y) em que as moedas aparecem, deve ser a altura da plataforma")]
    public float alturaSpawn = 0.5f;
    [Tooltip("Intervalo entre spawns, em segundos")]
    public float intervaloSpawn = 3f;
    [Tooltip("Número máximo de moedas na arena ao mesmo tempo")]
    public int maxMoedasSimultaneas = 3;

    private readonly List<Coin> moedasAtivas = new List<Coin>();
    private float timer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= intervaloSpawn && moedasAtivas.Count < maxMoedasSimultaneas)
        {
            timer = 0f;
            SpawnMoeda();
        }
    }

    private void SpawnMoeda()
    {
        Vector2 circulo = Random.insideUnitCircle * raioArena;
        Vector3 posAleatoria = new Vector3(circulo.x, alturaSpawn, circulo.y);
        GameObject obj = Instantiate(coinPrefab, posAleatoria, Quaternion.identity);
        Coin coin = obj.GetComponent<Coin>();
        if (coin != null) moedasAtivas.Add(coin);
    }

    public void NotificarMoedaColetada(Coin coin)
    {
        moedasAtivas.Remove(coin);
    }

    /// <summary>Chamado pelo GameManager ao iniciar/reiniciar um round.</summary>
    public void LimparTodasAsMoedas()
    {
        foreach (var coin in moedasAtivas)
        {
            if (coin != null) Destroy(coin.gameObject);
        }
        moedasAtivas.Clear();
        timer = 0f;
    }
}