using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EvolutionManager : MonoBehaviour
{
    public GameObject agentPrefab;
    public int populationSize = 20;
    public float trialTime = 15f;
    public float mutationRate = 0.05f;
    public Transform spawnCenter;
    public Transform spawnedAgentsParent;

    private List<AgentGAController> population = new List<AgentGAController>();
    private int generation = 1;
    private float timer;
    private bool isComplexActive = true;
    public bool useComplexAI = true;

    void Start()
    {
        SpawnPopulation();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= trialTime)
        {
            timer = 0f;
            Evolve();
        }
    }

    public void ToggleAIMode()
    {
        isComplexActive = !isComplexActive;

        foreach (var agent in population)
        {
            if (agent != null)
                agent.useComplexAI = isComplexActive;
        }
    }

    void Evolve()
    {
        population = population
            .Where(a => a != null)
            .OrderByDescending(a => a.GetFitness())
            .ToList();

        int survivorsCount = Mathf.Max(1, populationSize / 2);
        List<DNA> survivorDNAs = population
            .Take(survivorsCount)
            .Select(a => a.dna)
            .ToList();

        foreach (var agent in population)
        {
            if (agent != null)
                Destroy(agent.gameObject);
        }

        population.Clear();

        for (int i = 0; i < populationSize; i++)
        {
            DNA pA = survivorDNAs[Random.Range(0, survivorDNAs.Count)];
            DNA pB = survivorDNAs[Random.Range(0, survivorDNAs.Count)];
            DNA childDNA = pA.Crossover(pB);
            childDNA.Mutate(mutationRate);
            SpawnAgent(childDNA);
        }

        generation++;
    }

    void SpawnAgent(DNA customDNA)
    {
        Vector2 pos = (spawnCenter != null) ? (Vector2)spawnCenter.position : Vector2.zero;
        pos += Random.insideUnitCircle * 5f;

        GameObject obj = Instantiate(
            agentPrefab,
            pos,
            Quaternion.identity,
            spawnedAgentsParent
        );

        AgentGAController controller = obj.GetComponent<AgentGAController>();

        if (controller != null)
        {
            controller.dna = customDNA;
            controller.useComplexAI = isComplexActive;
            population.Add(controller);
        }
        else
        {
            Debug.LogError("Spawned agent prefab does not have an AgentGAController component.");
        }
    }

    void SpawnPopulation()
    {
        for (int i = 0; i < populationSize; i++)
        {
            SpawnAgent(new DNA(3));
        }
    }

    void OnDisable()
    {
        foreach (var agent in population)
        {
            if (agent != null)
                Destroy(agent.gameObject);
        }

        population.Clear();
        timer = 0f;
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 250, 70), "Evolutionary Sandbox");
        GUI.Label(new Rect(20, 30, 200, 20), "Generation: " + generation);
        GUI.Label(new Rect(20, 50, 200, 20), "Mode: " + (isComplexActive ? "Complex (DNA)" : "Basic (Random)"));
    }
}