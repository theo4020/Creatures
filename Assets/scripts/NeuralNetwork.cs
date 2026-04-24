using System;
using UnityEngine;

/// <summary>
/// Réseau de neurones feedforward simple.
/// Les poids sont stockés à plat pour faciliter le GA (croisement/mutation).
/// </summary>
[Serializable]
public class NeuralNetwork
{
    public float Fitness;

    private int[] _layers;    // ex: {14, 32, 32, 4}
    private float[][] _neurons;   // valeurs des neurones par couche
    private float[][] _biases;    // biais par couche (sauf entrée)
    private float[][][] _weights; // poids [couche][neurone][entrée]

    // ─────────────────────────────────────────────
    // CONSTRUCTEUR
    // ─────────────────────────────────────────────
    public NeuralNetwork(int[] layers)
    {
        _layers = (int[])layers.Clone();
        InitNeurons();
        InitWeightsRandom();
    }

    // Constructeur de copie
    public NeuralNetwork(NeuralNetwork other)
    {
        _layers = (int[])other._layers.Clone();
        Fitness = 0f;
        InitNeurons();
        CopyWeightsFrom(other);
    }

    // ─────────────────────────────────────────────
    // INITIALISATION
    // ─────────────────────────────────────────────
    private void InitNeurons()
    {
        _neurons = new float[_layers.Length][];
        for (int i = 0; i < _layers.Length; i++)
            _neurons[i] = new float[_layers[i]];
    }

    private void InitWeightsRandom()
    {
        _biases = new float[_layers.Length - 1][];
        _weights = new float[_layers.Length - 1][][];

        for (int i = 1; i < _layers.Length; i++)
        {
            int layer = i - 1;
            _biases[layer] = new float[_layers[i]];
            _weights[layer] = new float[_layers[i]][];

            for (int n = 0; n < _layers[i]; n++)
            {
                _biases[layer][n] = RandomGaussian();
                _weights[layer][n] = new float[_layers[i - 1]];

                for (int w = 0; w < _layers[i - 1]; w++)
                    _weights[layer][n][w] = RandomGaussian();
            }
        }
    }

    // ─────────────────────────────────────────────
    // PROPAGATION AVANT
    // ─────────────────────────────────────────────
    public float[] Activate(float[] inputs)
    {
        // Couche d'entrée
        for (int i = 0; i < _layers[0]; i++)
            _neurons[0][i] = inputs[i];

        // Couches cachées + sortie
        for (int layer = 1; layer < _layers.Length; layer++)
        {
            for (int n = 0; n < _layers[layer]; n++)
            {
                float sum = _biases[layer - 1][n];
                for (int w = 0; w < _layers[layer - 1]; w++)
                    sum += _neurons[layer - 1][w] * _weights[layer - 1][n][w];

                // Tanh sur toutes les couches sauf la sortie (qui reste dans [-1,1] naturellement)
                _neurons[layer][n] = (float)Math.Tanh(sum);
            }
        }

        return (float[])_neurons[_neurons.Length - 1].Clone();
    }

    // ─────────────────────────────────────────────
    // GA : MUTATION
    // ─────────────────────────────────────────────
    public void Mutate(float rate = 0.1f, float strength = 0.2f)
    {
        for (int layer = 0; layer < _weights.Length; layer++)
        {
            for (int n = 0; n < _weights[layer].Length; n++)
            {
                // Muter le biais
                if (UnityEngine.Random.value < rate)
                    _biases[layer][n] += RandomGaussian() * strength;

                // Muter les poids
                for (int w = 0; w < _weights[layer][n].Length; w++)
                    if (UnityEngine.Random.value < rate)
                        _weights[layer][n][w] += RandomGaussian() * strength;
            }
        }
    }

    // ─────────────────────────────────────────────
    // GA : CROISEMENT UNIFORME
    // ─────────────────────────────────────────────
    public static NeuralNetwork Crossover(NeuralNetwork parentA, NeuralNetwork parentB)
    {
        NeuralNetwork child = new NeuralNetwork(parentA);

        for (int layer = 0; layer < child._weights.Length; layer++)
        {
            for (int n = 0; n < child._weights[layer].Length; n++)
            {
                if (UnityEngine.Random.value > 0.5f)
                    child._biases[layer][n] = parentB._biases[layer][n];

                for (int w = 0; w < child._weights[layer][n].Length; w++)
                    if (UnityEngine.Random.value > 0.5f)
                        child._weights[layer][n][w] = parentB._weights[layer][n][w];
            }
        }

        return child;
    }

    // ─────────────────────────────────────────────
    // UTILITAIRES
    // ─────────────────────────────────────────────
    private void CopyWeightsFrom(NeuralNetwork other)
    {
        _biases = new float[other._biases.Length][];
        _weights = new float[other._weights.Length][][];

        for (int layer = 0; layer < other._weights.Length; layer++)
        {
            _biases[layer] = (float[])other._biases[layer].Clone();
            _weights[layer] = new float[other._weights[layer].Length][];

            for (int n = 0; n < other._weights[layer].Length; n++)
                _weights[layer][n] = (float[])other._weights[layer][n].Clone();
        }
    }

    private static float RandomGaussian()
    {
        // Box-Muller transform
        float u1 = 1f - UnityEngine.Random.value;
        float u2 = 1f - UnityEngine.Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
    }
}