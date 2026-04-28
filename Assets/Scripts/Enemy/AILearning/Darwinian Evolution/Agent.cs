using UnityEngine;

public class Agent : MonoBehaviour
{
    public bool useComplexAI = true;

    public DNA dna;
    public float fitness;

    private Transform player;
    private float speed;
    private float turnSpeed;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (dna == null)
            dna = new DNA(3); // 3 genes

        MapGenes();
    }

    void Update()
    {
        if (useComplexAI)
            ComplexMovement();
        else
            BasicMovement();
    }

    void MapGenes()
    {
        speed = Mathf.Lerp(1f, 5f, (dna.genes[0] + 1) / 2f);
        turnSpeed = Mathf.Lerp(50f, 200f, (dna.genes[1] + 1) / 2f);
    }

    void BasicMovement()
    {
        transform.Translate(Vector3.forward * 2f * Time.deltaTime);
        transform.Rotate(Vector3.up * Random.Range(-100f, 100f) * Time.deltaTime);
    }

    void ComplexMovement()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);

        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    public void CalculateFitness()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        fitness = 1f / (distance + 1f);
    }
}