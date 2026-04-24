using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Gère la population de créatures, les épisodes et l'algorithme génétique.
///
/// SETUP DANS L'ÉDITEUR :
///   1. Créer un GameObject vide "TrainingManager", attacher ce script
///   2. Créer un Prefab de votre créature (avec CreatureAgent + ArticulationBodies)
///   3. Glisser le Prefab dans le champ "Creature Prefab"
///   4. Appuyer sur Play ▶
/// </summary>
public class TrainingManager : MonoBehaviour
{
    [Header("Population")]
    public GameObject creaturePrefab;
    public int populationSize = 20;
    public float spacing = 3f;      // Espacement entre créatures

    [Header("Épisode")]
    public float episodeDuration = 10f;          // Secondes par génération

    [Header("Réseau de neurones")]
    public int hiddenUnits = 32;                 // Neurones par couche cachée
    public int hiddenLayers = 2;                 // Nombre de couches cachées

    [Header("Algorithme génétique")]
    [Range(0f, 1f)] public float mutationRate = 0.1f;
    [Range(0f, 1f)] public float mutationStrength = 0.2f;
    [Range(0f, 1f)] public float eliteRatio = 0.2f;  // % élites conservées

    // État
    private List<CreatureAgent> _population = new();
    private int _generation = 0;
    private float _bestFitness = 0f;

    // ─────────────────────────────────────────────
    // DÉMARRAGE
    // ─────────────────────────────────────────────
    private void Start()
    {
        if (creaturePrefab == null)
        {
            Debug.LogError("[TrainingManager] Assigne le Creature Prefab dans l'inspecteur !");
            return;
        }

        SpawnPopulation();
        StartCoroutine(TrainingLoop());
    }

    // ─────────────────────────────────────────────
    // SPAWN INITIAL
    // ─────────────────────────────────────────────
    private void SpawnPopulation()
    {
        for (int i = 0; i < populationSize; i++)
        {
            // Place les créatures en ligne sur l'axe X
            Vector3 pos = new Vector3(i * spacing - (populationSize * spacing * 0.5f), 0, 0);
            GameObject go = Instantiate(creaturePrefab, pos, Quaternion.identity);
            go.name = $"Creature_{i}";

            CreatureAgent agent = go.GetComponent<CreatureAgent>();
            agent.Initialize();

            // Construit le réseau : [obs, hidden..., actions]
            int[] layers = BuildLayerSizes(agent);
            agent.Brain = new NeuralNetwork(layers);

            _population.Add(agent);
        }

        Debug.Log($"[TrainingManager] {populationSize} créatures spawned. Réseau : {string.Join("→", BuildLayerSizes(_population[0]))}");
    }

    // ─────────────────────────────────────────────
    // BOUCLE D'ENTRAÎNEMENT
    // ─────────────────────────────────────────────
    private IEnumerator TrainingLoop()
    {
        while (true)
        {
            _generation++;
            Debug.Log($"[GA] ── Génération {_generation} ──");

            // Démarre tous les épisodes
            foreach (var agent in _population)
                agent.StartEpisode();

            // Attend la fin de l'épisode
            yield return new WaitForSeconds(episodeDuration);

            // Stoppe toutes les créatures
            foreach (var agent in _population)
                agent.IsAlive = false;

            // Évalue et log
            EvaluateGeneration();

            // Fait évoluer la population
            EvolvePopulation();
        }
    }

    // ─────────────────────────────────────────────
    // ÉVALUATION
    // ─────────────────────────────────────────────
    private void EvaluateGeneration()
    {
        var sorted = _population.OrderByDescending(a => a.Fitness).ToList();

        float best = sorted[0].Fitness;
        float avg = _population.Average(a => a.Fitness);

        if (best > _bestFitness) _bestFitness = best;

        Debug.Log($"[GA] Gen {_generation} | Best: {best:F2}m | Avg: {avg:F2}m | AllTimeBest: {_bestFitness:F2}m");
    }

    // ─────────────────────────────────────────────
    // ALGORITHME GÉNÉTIQUE
    // ─────────────────────────────────────────────
    private void EvolvePopulation()
    {
        var sorted = _population.OrderByDescending(a => a.Fitness).ToList();
        int eliteCount = Mathf.Max(1, Mathf.RoundToInt(populationSize * eliteRatio));

        List<NeuralNetwork> newBrains = new();

        // 1. Élites → conservées telles quelles
        for (int i = 0; i < eliteCount; i++)
            newBrains.Add(new NeuralNetwork(sorted[i].Brain));

        // 2. Reste → croisement + mutation depuis les élites
        while (newBrains.Count < populationSize)
        {
            NeuralNetwork parentA = sorted[Random.Range(0, eliteCount)].Brain;
            NeuralNetwork parentB = sorted[Random.Range(0, eliteCount)].Brain;
            NeuralNetwork child = NeuralNetwork.Crossover(parentA, parentB);
            child.Mutate(mutationRate, mutationStrength);
            newBrains.Add(child);
        }

        // 3. Injecte les nouveaux cerveaux
        for (int i = 0; i < populationSize; i++)
            _population[i].Brain = newBrains[i];
    }

    // ─────────────────────────────────────────────
    // UTILITAIRE
    // ─────────────────────────────────────────────
    private int[] BuildLayerSizes(CreatureAgent agent)
    {
        var layers = new List<int> { agent.ObservationSize() };
        for (int i = 0; i < hiddenLayers; i++)
            layers.Add(hiddenUnits);
        layers.Add(agent.ActionSize());
        return layers.ToArray();
    }

    // ─────────────────────────────────────────────
    // AFFICHAGE GUI (dans la Game View)
    // ─────────────────────────────────────────────
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 25), $"Génération : {_generation}");
        GUI.Label(new Rect(10, 35, 300, 25), $"Meilleur (all time) : {_bestFitness:F2} m");

        if (_population.Count > 0)
        {
            int alive = _population.Count(a => a.IsAlive);
            GUI.Label(new Rect(10, 60, 300, 25), $"Vivantes : {alive} / {populationSize}");
        }
    }
}