using System.Collections.Generic;
using UnityEngine;

public class EvolutionaryStrategy : MonoBehaviour
{
    [Header("(mu + lambda) Evolution Strategy")]
    [Tooltip("mu = cantidad de padres que sobreviven en cada generación.")]
    [Min(1)]
    [SerializeField] private int mu = 15;

    [Tooltip("lambda = cantidad de hijos generados por mutación en cada generación.")]
    [Min(1)]
    [SerializeField] private int lambda = 45;

    [Min(1)]
    [SerializeField] private int generations = 80;

    [Tooltip("Probabilidad de que cada mina (fuera de la máscara) cambie de posición al mutar.")]
    [Range(0f, 1f)]
    [SerializeField] private float mutationRate = 0.20f;

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;

    public int Mu
    {
        get => mu;
        set => mu = Mathf.Max(1, value);
    }

    public int Lambda
    {
        get => lambda;
        set => lambda = Mathf.Max(1, value);
    }

    public int Generations
    {
        get => generations;
        set => generations = Mathf.Max(1, value);
    }

    public float MutationRate
    {
        get => mutationRate;
        set => mutationRate = Mathf.Clamp01(value);
    }

    public float LastBestFitness { get; private set; }

    // Ejecuta la Estrategia Evolutiva (mu + lambda) sobre el tablero inicial respetando la máscara.
    public List<List<char>> Generate(
        List<List<char>> initialBoard,
        bool[,] maskGrid,
        bool preferHighDensity)
    {
        if (initialBoard == null || initialBoard.Count == 0)
            return null;

        System.Random random = new System.Random();

        // 1. Crear "mu" padres iniciales a partir del estado base
        List<BoardCandidate> parents = CreateInitialParents(mu, initialBoard, maskGrid, random);

        // 2. Evaluar los padres iniciales
        EvaluatePopulation(parents, maskGrid, preferHighDensity);

        // 3. Ciclo evolutivo por "generations"
        for (int gen = 0; gen < generations; gen++)
        {
            List<BoardCandidate> offspring = new List<BoardCandidate>(lambda);

            // Generar lambda hijos
            for (int i = 0; i < lambda; i++)
            {
                BoardCandidate selectedParent = parents[random.Next(0, parents.Count)];
                BoardCandidate child = selectedParent.Clone();

                Mutate(child, maskGrid, random);
                HillClimbing.EvaluateBoard(child, maskGrid, preferHighDensity);

                offspring.Add(child);
            }

            // Combinar padres + hijos (mu + lambda)
            List<BoardCandidate> combined = new List<BoardCandidate>(mu + lambda);
            combined.AddRange(parents);
            combined.AddRange(offspring);

            // Ordenar de mayor a menor fitness
            combined.Sort((a, b) => b.fitness.CompareTo(a.fitness));

            // Conservar únicamente los mejores "mu" individuos como nuevos padres
            parents = combined.GetRange(0, mu);

            if (logProgress)
            {
                Debug.Log($"[EvolutionaryStrategy] Gen {gen + 1}/{generations} - Best Fitness: {parents[0].fitness:F2}");
            }
        }

        LastBestFitness = parents[0].fitness;

        // Retornar el tablero del mejor individuo final
        return parents[0].board;
    }

    
    // Crea los "mu" padres iniciales manteniendo las máscaras intactas y el mismo número de minas.
    private List<BoardCandidate> CreateInitialParents(
        int parentCount,
        List<List<char>> baseBoard,
        bool[,] maskGrid,
        System.Random random)
    {
        List<BoardCandidate> parents = new List<BoardCandidate>(parentCount);

        // El primer padre es exactamente el tablero inicial de RandomStartState
        BoardCandidate firstParent = new BoardCandidate(baseBoard);
        parents.Add(firstParent);

        // Los demás padres son clones con variaciones iniciales para dar diversidad a la población
        for (int i = 1; i < parentCount; i++)
        {
            BoardCandidate candidate = firstParent.Clone();
            Mutate(candidate, maskGrid, random);
            parents.Add(candidate);
        }

        return parents;
    }

    // Muta un tablero moviendo minas ubicadas en zonas NO bloqueadas hacia casillas vacías NO bloqueadas.
    private void Mutate(
        BoardCandidate candidate,
        bool[,] maskGrid,
        System.Random random)
    {
        List<Vector2Int> movableMines = new List<Vector2Int>();
        List<Vector2Int> availableEmpty = new List<Vector2Int>();

        for (int x = 0; x < candidate.width; x++)
        {
            for (int y = 0; y < candidate.height; y++)
            {
                // Si la casilla está protegida por la máscara, no se puede tocar
                if (maskGrid != null && maskGrid[x, y])
                    continue;

                if (candidate.board[x][y] == 'M')
                    movableMines.Add(new Vector2Int(x, y));
                else if (candidate.board[x][y] == 'E')
                    availableEmpty.Add(new Vector2Int(x, y));
            }
        }

        if (movableMines.Count == 0 || availableEmpty.Count == 0)
            return;

        bool mutated = false;

        // Recorremos las minas modificables y aplicamos mutationRate
        for (int i = 0; i < movableMines.Count; i++)
        {
            if (random.NextDouble() < mutationRate && availableEmpty.Count > 0)
            {
                Vector2Int minePos = movableMines[i];
                int randomEmptyIdx = random.Next(0, availableEmpty.Count);
                Vector2Int newPos = availableEmpty[randomEmptyIdx];

                // Mover la mina a la casilla vacía
                candidate.board[minePos.x][minePos.y] = 'E';
                candidate.board[newPos.x][newPos.y] = 'M';

                // Actualizar la lista de vacías disponibles para no solapar minas en esta misma mutación
                availableEmpty[randomEmptyIdx] = minePos;
                mutated = true;
            }
        }

        // Asegurar que cada hijo tenga al menos una variación respecto a su padre
        if (!mutated && availableEmpty.Count > 0)
        {
            int randomMineIdx = random.Next(0, movableMines.Count);
            int randomEmptyIdx = random.Next(0, availableEmpty.Count);

            Vector2Int minePos = movableMines[randomMineIdx];
            Vector2Int newPos = availableEmpty[randomEmptyIdx];

            candidate.board[minePos.x][minePos.y] = 'E';
            candidate.board[newPos.x][newPos.y] = 'M';
        }
    }

    private void EvaluatePopulation(
        List<BoardCandidate> population,
        bool[,] maskGrid,
        bool preferHighDensity)
    {
        foreach (BoardCandidate candidate in population)
        {
            HillClimbing.EvaluateBoard(candidate, maskGrid, preferHighDensity);
        }
    }
}