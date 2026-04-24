using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Gère la population de créatures.
/// Si creaturePrefab est assigné → l'utilise.
/// Sinon → génère des créatures aléatoires via Creature.CreateFromStart().
/// </summary>
public class TrainingManager : MonoBehaviour
{
    [Header("Population")]
    public GameObject creaturePrefab;   // Laisser vide pour génération aléatoire
    public int        populationSize  = 20;
    public float      spacing         = 5f;
    [Tooltip("Hauteur de spawn en mètres. 0 = calculée automatiquement depuis les colliders.")]
    public float      spawnHeight     = 0f;

    [Header("Épisode")]
    public float episodeDuration = 10f;

    [Header("Réseau de neurones")]
    public int hiddenUnits  = 32;
    public int hiddenLayers = 2;

    [Header("Algorithme génétique")]
    [Range(0f, 1f)] public float mutationRate     = 0.1f;
    [Range(0f, 1f)] public float mutationStrength = 0.2f;
    [Range(0f, 1f)] public float eliteRatio       = 0.2f;

    // État
    private List<CreatureAgent> _population     = new();
    private int   _generation      = 0;
    private float _bestFitness     = 0f;
    private float _prevBestFitness = 0f;
    private float _prevBestDist    = 0f;
    private float _prevAvgFitness  = 0f;
    private int   _stagnationCount = 0;

    // ─────────────────────────────────────────────
    // DÉMARRAGE
    // ─────────────────────────────────────────────
    private void Start()
    {
        StartCoroutine(InitAndTrain());
    }

    private IEnumerator InitAndTrain()
    {
        SpawnPopulation();
        yield return null; // attend une frame que Unity initialise tous les composants
        foreach (var agent in _population)
        {
            agent.Initialize();
            if (spawnHeight > 0f) agent.SetSpawnHeight(spawnHeight);
            agent.Brain = new NeuralNetwork(BuildLayerSizes(agent));
        }
        if (_population.Count > 0)
            Debug.Log($"[TrainingManager] Réseau : {string.Join("→", BuildLayerSizes(_population[0]))}");
        StartCoroutine(TrainingLoop());
    }

    // ─────────────────────────────────────────────
    // SPAWN
    // ─────────────────────────────────────────────
    private void SpawnPopulation()
    {
        // ── Crée UN seul modèle aléatoire, puis le clone pour toute la population ──
        GameObject template = null;

        if (creaturePrefab != null)
        {
            template = creaturePrefab;
        }
        else
        {
            // Génère un corps aléatoire template (hors scène, position 0)
            template = new GameObject("CreatureTemplate");
            template.transform.position = Vector3.zero;
            var creature = template.AddComponent<Creature>();
            CreateRandom(creature);
            template.SetActive(false); // cache le template
        }

        for (int i = 0; i < populationSize; i++)
        {
            Vector3 pos = new Vector3(
                i * spacing - populationSize * spacing * 0.5f, 2f, 0
            );

            // Clone le template
            var go = Instantiate(template, pos, Quaternion.identity);
            go.name = $"Creature_{i}";
            go.SetActive(true);

            var agent = go.GetComponent<CreatureAgent>();
            if (agent == null) agent = go.AddComponent<CreatureAgent>();

            agent.Brain = new NeuralNetwork(new int[] { 6, hiddenUnits, 1 }); // temporaire
            _population.Add(agent);
        }

        // Détruit le template si généré aléatoirement
        if (creaturePrefab == null)
            Destroy(template);
    }

    // ─────────────────────────────────────────────
    // CRÉATION ALÉATOIRE
    // ─────────────────────────────────────────────
    private void CreateRandom(Creature creature)
    {
        LimbData bodyData = new LimbData(true, Vector3.zero);
        creature.body = Limb.Create(bodyData);
        creature.body.transform.SetParent(creature.transform, false);
        creature.body.nbAttachedLimb = Random.Range(2, 6);

        for (int i = 0; i < creature.body.nbAttachedLimb; i++)
        {
            LimbData limbData = new LimbData(false, Vector3.zero);
            Limb limb = Limb.Create(limbData, creature.body);
            limb.nbAttachedLimb = Random.Range(0, 3);

            for (int j = 0; j < limb.nbAttachedLimb; j++)
            {
                LimbData subData = new LimbData(false, Vector3.zero);
                Limb sub = Limb.Create(subData, limb);
                limb.limbs.Add(sub);
            }
            creature.body.limbs.Add(limb);
        }
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

            foreach (var agent in _population)
                agent.StartEpisode();

            yield return new WaitForSeconds(episodeDuration);

            foreach (var agent in _population)
                agent.IsAlive = false;

            EvaluateGeneration();
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

        // Détecte stagnation
        if (bestFitness <= _bestFitness + 0.01f)
            _stagnationCount++;
        else
            _stagnationCount = 0;

        if (bestFitness > _bestFitness) _bestFitness = bestFitness;

        _prevBestFitness = bestFitness;
        _prevBestDist    = bestDist;
        _prevAvgFitness  = avgFitness;

        Debug.Log($"[GA] Gen {_generation} | BestFitness={bestFitness:F2} | BestDist={bestDist:F2}m | Avg={avgFitness:F2} | Stagnation={_stagnationCount}");
    }

    // ─────────────────────────────────────────────
    // ALGORITHME GÉNÉTIQUE
    // ─────────────────────────────────────────────
    private void EvolvePopulation()
    {
        var sorted     = _population.OrderByDescending(a => a.Fitness).ToList();
        int eliteCount = Mathf.Max(1, Mathf.RoundToInt(populationSize * eliteRatio));

        // Mutation adaptative : si stagnation > 10 gens → augmente mutation
        float rate     = mutationRate;
        float strength = mutationStrength;
        if (_stagnationCount > 10)
        {
            rate     = Mathf.Min(0.5f, rate     * 2f);
            strength = Mathf.Min(1.0f, strength * 2f);
            Debug.Log($"[GA] Stagnation détectée — mutation boostée : rate={rate:F2} strength={strength:F2}");
        }

        List<NeuralNetwork> newBrains = new();

        // Élites conservées
        for (int i = 0; i < eliteCount; i++)
            newBrains.Add(new NeuralNetwork(sorted[i].Brain));

        // Croisement + mutation
        while (newBrains.Count < populationSize)
        {
            NeuralNetwork pA    = sorted[Random.Range(0, eliteCount)].Brain;
            NeuralNetwork pB    = sorted[Random.Range(0, eliteCount)].Brain;
            NeuralNetwork child = NeuralNetwork.Crossover(pA, pB);
            child.Mutate(rate, strength);
            newBrains.Add(child);
        }

        for (int i = 0; i < populationSize; i++)
            _population[i].Brain = newBrains[i];
    }

    // ─────────────────────────────────────────────
    // UTILITAIRES
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
    // GUI
    // ─────────────────────────────────────────────
    private void OnGUI()
    {
        var style     = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        var styleGood = new GUIStyle(style) { normal = { textColor = Color.green } };
        var styleBad  = new GUIStyle(style) { normal = { textColor = new Color(1f, 0.4f, 0f) } };

        GUI.Label(new Rect(10, 10, 350, 25), $"Génération    : {_generation}", style);
        GUI.Label(new Rect(10, 35, 350, 25), $"All-time best : {_bestFitness:F2}", styleGood);
        if (_stagnationCount > 5)
            GUI.Label(new Rect(10, 55, 350, 20), $"⚠ Stagnation : {_stagnationCount} gens", styleBad);

        GUI.Label(new Rect(10, 78, 350, 20),  "── Génération précédente ──", style);
        GUI.Label(new Rect(10, 98, 350, 25),  $"  Meilleure fitness : {_prevBestFitness:F2}", style);
        GUI.Label(new Rect(10, 121, 350, 25), $"  Meilleure dist    : {_prevBestDist:F2} m",  style);
        GUI.Label(new Rect(10, 144, 350, 25), $"  Fitness moyenne   : {_prevAvgFitness:F2}",  style);

        if (_population.Count > 0)
        {
            int alive = _population.Count(a => a.IsAlive);
            GUI.Label(new Rect(10, 172, 350, 25), $"Vivantes : {alive} / {populationSize}",
                alive > 0 ? style : styleBad);
        }

        GUI.Label(new Rect(10, 200, 300, 25), $"Vitesse : x{Time.timeScale:F0}", style);
        if (GUI.Button(new Rect(10,  225, 50, 28), "x1"))  Time.timeScale = 1f;
        if (GUI.Button(new Rect(65,  225, 50, 28), "x2"))  Time.timeScale = 2f;
        if (GUI.Button(new Rect(120, 225, 50, 28), "x5"))  Time.timeScale = 5f;
        if (GUI.Button(new Rect(175, 225, 50, 28), "x10")) Time.timeScale = 10f;
        if (GUI.Button(new Rect(230, 225, 50, 28), "x20")) Time.timeScale = 20f;
    }
}