using UnityEngine;

[System.Serializable]
public class DNA
{
    public float[] genes;

    public DNA(int size)
    {
        genes = new float[size];
        Randomize();
    }

    public void Randomize()
    {
        for (int i = 0; i < genes.Length; i++)
            genes[i] = Random.Range(-1f, 1f);
    }

    public DNA Crossover(DNA partner)
    {
        DNA child = new DNA(genes.Length);
        int midpoint = Random.Range(0, genes.Length);

        for (int i = 0; i < genes.Length; i++)
        {
            child.genes[i] = (i > midpoint) ? genes[i] : partner.genes[i];
        }
        return child;
    }

    public void Mutate(float rate)
    {
        for (int i = 0; i < genes.Length; i++)
        {
            if (Random.value < rate)
                genes[i] = Random.Range(-1f, 1f);
        }
    }
}