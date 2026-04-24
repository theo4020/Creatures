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
    public int        populationSize  = 20;
    public float      spacing         = 3f;      // Espacement entre créatures

    [Header("Épisode")]
    public float episodeDuration = 10f;          // Secondes par génération

    [Header("Réseau de neurones")]
    public int hiddenUnits = 32;                 // Neurones par couche cachée
    public int hiddenLayers = 2;                 // Nombre de couches cachées

    [Header("Algorithme génétique")]
    [Range(0f, 1f)] public float mutationRate     = 0.1f;
    [Range(0f, 1f)] public float mutationStrength = 0.2f;
    [Range(0f, 1f)] public float eliteRatio       = 0.2f;  // % élites conservées

    // État
    private List<CreatureAgent> _population = new();
    private int   _generation    = 0;
    private float _bestFitness   = 0f;
    private float _prevBestFitness = 0f;
    private float _prevBestDist    = 0f;
    private float _prevAvgFitness  = 0f;

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
            agent.Brain  = new NeuralNetwork(layers);

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

        float bestFitness = sorted[0].Fitness;
        float bestDist    = _population.Max(a => a.MaxDist);
        float avgFitness  = _population.Average(a => a.Fitness);

        if (bestFitness > _bestFitness) _bestFitness = bestFitness;

        // Sauvegarde pour affichage génération suivante
        _prevBestFitness = bestFitness;
        _prevBestDist    = bestDist;
        _prevAvgFitness  = avgFitness;

        Debug.Log($"[GA] Gen {_generation} | BestFitness={bestFitness:F2} | BestDist={bestDist:F2}m | Avg={avgFitness:F2} | AllTimeBest={_bestFitness:F2}");
    }

    // ─────────────────────────────────────────────
    // ALGORITHME GÉNÉTIQUE
    // ─────────────────────────────────────────────
    private void EvolvePopulation()
    {
        var sorted    = _population.OrderByDescending(a => a.Fitness).ToList();
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
            NeuralNetwork child   = NeuralNetwork.Crossover(parentA, parentB);
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
        var style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        var styleGood = new GUIStyle(style);
        styleGood.normal.textColor = Color.green;
        var styleBad = new GUIStyle(style);
        styleBad.normal.textColor = new Color(1f, 0.4f, 0f);

        GUI.Label(new Rect(10, 10, 350, 25), $"Génération    : {_generation}", style);
        GUI.Label(new Rect(10, 35, 350, 25), $"All-time best : {_bestFitness:F2}", styleGood);

        // Séparateur
        GUI.Label(new Rect(10, 65, 350, 20), "── Génération précédente ──", style);
        GUI.Label(new Rect(10, 85,  350, 25), $"  Meilleure fitness : {_prevBestFitness:F2}", style);
        GUI.Label(new Rect(10, 108, 350, 25), $"  Meilleure dist    : {_prevBestDist:F2} m", style);
        GUI.Label(new Rect(10, 131, 350, 25), $"  Fitness moyenne   : {_prevAvgFitness:F2}", style);

        // Vivantes
        if (_population.Count > 0)
        {
            int alive = _population.Count(a => a.IsAlive);
            GUI.Label(new Rect(10, 160, 350, 25), $"Vivantes : {alive} / {populationSize}", alive > 0 ? style : styleBad);
        }

        // Boutons vitesse
        GUI.Label(new Rect(10, 192, 300, 25), $"Vitesse : x{Time.timeScale:F0}", style);
        if (GUI.Button(new Rect(10,  217, 50, 28), "x1"))  Time.timeScale = 1f;
        if (GUI.Button(new Rect(65,  217, 50, 28), "x2"))  Time.timeScale = 2f;
        if (GUI.Button(new Rect(120, 217, 50, 28), "x5"))  Time.timeScale = 5f;
        if (GUI.Button(new Rect(175, 217, 50, 28), "x10")) Time.timeScale = 10f;
        if (GUI.Button(new Rect(230, 217, 50, 28), "x20")) Time.timeScale = 20f;
    }
}