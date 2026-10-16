using System.Collections.Generic;
using UnityEngine;

public class HillClimbing : MonoBehaviour
{
    [Header("Preferencia de Densidad")]
    [Tooltip("True = Mayor densidad (minas agrupadas). False = Menor densidad (minas dispersas/separadas).")]
    [SerializeField] private bool preferHighDensity = true;

    [Header("Parámetros de Hill Climbing")]
    [Min(1)]
    [SerializeField] private int maxIterations = 200;

    [Tooltip("Cantidad de movimientos vecinos que explora en cada paso para subir la colina")]
    [Min(1)]
    [SerializeField] private int neighborsPerStep = 15;

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;

    public bool PreferHighDensity
    {
        get => preferHighDensity;
        set => preferHighDensity = value;
    }

    /// <summary>
    /// Ejecuta Hill Climbing sobre un tablero existente respetando las máscaras.
    /// </summary>
    public List<List<char>> Optimize(List<List<char>> initialBoard, bool[,] maskGrid)
    {
        BoardCandidate current = new BoardCandidate(initialBoard);
        EvaluateBoard(current, maskGrid, preferHighDensity);

        float initialFitness = current.fitness;

        for (int iter = 0; iter < maxIterations; iter++)
        {
            BoardCandidate bestNeighbor = null;

            // Generar vecinos moviendo una mina a una casilla vacía (solo en zonas sin máscara)
            for (int n = 0; n < neighborsPerStep; n++)
            {
                BoardCandidate neighbor = GenerateNeighbor(current, maskGrid);
                EvaluateBoard(neighbor, maskGrid, preferHighDensity);

                if (bestNeighbor == null || neighbor.fitness > bestNeighbor.fitness)
                {
                    bestNeighbor = neighbor;
                }
            }

            // Si el mejor vecino supera al estado actual, avanzamos ("subimos la colina")
            if (bestNeighbor != null && bestNeighbor.fitness > current.fitness)
            {
                current = bestNeighbor;
            }
            else
            {
                // Si no hubo mejora tras explorar vecinos, llegamos a un óptimo local
                if (logProgress)
                {
                    Debug.Log($"[HillClimbing] Óptimo local alcanzado en iteración {iter}. Fitness Inicial: {initialFitness:F2} -> Final: {current.fitness:F2}");
                }
                break;
            }
        }

        if (logProgress)
        {
            Debug.Log($"[HillClimbing] Finalizado. Fitness Inicial: {initialFitness:F2} -> Final: {current.fitness:F2}");
        }

        return current.board;
    }

    /// <summary>
    /// Genera un tablero vecino moviendo una mina no bloqueada a una casilla vacía no bloqueada.
    /// </summary>
    private BoardCandidate GenerateNeighbor(BoardCandidate current, bool[,] maskGrid)
    {
        BoardCandidate neighbor = current.Clone();

        List<Vector2Int> movableMines = new List<Vector2Int>();
        List<Vector2Int> availableEmpty = new List<Vector2Int>();

        for (int x = 0; x < neighbor.width; x++)
        {
            for (int y = 0; y < neighbor.height; y++)
            {
                // Ignorar casillas protegidas por la máscara
                if (maskGrid[x, y]) continue;

                if (neighbor.board[x][y] == 'M')
                    movableMines.Add(new Vector2Int(x, y));
                else if (neighbor.board[x][y] == 'E')
                    availableEmpty.Add(new Vector2Int(x, y));
            }
        }

        if (movableMines.Count > 0 && availableEmpty.Count > 0)
        {
            Vector2Int minePos = movableMines[Random.Range(0, movableMines.Count)];
            Vector2Int emptyPos = availableEmpty[Random.Range(0, availableEmpty.Count)];

            // Intercambiar la mina a la nueva posición vacía
            neighbor.board[minePos.x][minePos.y] = 'E';
            neighbor.board[emptyPos.x][emptyPos.y] = 'M';
        }

        return neighbor;
    }

    /// <summary>
    /// Evalúa el tablero asignando un valor a cada casilla según el mapa de calor de densidad.
    /// Este método es público/estático para que EvolutionaryStrategy también pueda usarlo.
    /// </summary>
    public static float EvaluateBoard(BoardCandidate candidate, bool[,] maskGrid, bool highDensity)
    {
        float totalScore = 0f;
        int width = candidate.width;
        int height = candidate.height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Ignoramos o damos peso neutro a las casillas dentro de la máscara
                if (maskGrid != null && maskGrid[x, y])
                    continue;

                int adjacentMines = CountAdjacentMines(candidate.board, x, y, width, height);
                bool isMine = candidate.board[x][y] == 'M';

                if (highDensity)
                {
                    // MAYOR DENSIDAD:
                    // Las casillas con minas alcanzan un valor mayor si están rodeadas de minas (0 a 8).
                    // Las casillas vacías tienen un valor menor si tienen minas cerca (premiando grandes zonas despejadas).
                    if (isMine)
                        totalScore += adjacentMines * 2.0f;
                    else
                        totalScore += (8 - adjacentMines) * 0.5f;
                }
                else
                {
                    // MENOR DENSIDAD (Dispersión):
                    // Al revés: las minas tienen menor valor si se amontonan y mayor si están separadas.
                    // Las casillas vacías alcanzan un valor mayor al estar rodeadas de pocas minas distribuidas.
                    if (isMine)
                        totalScore += (8 - adjacentMines) * 2.0f;
                    else
                        totalScore += adjacentMines * 0.5f;
                }
            }
        }

        candidate.fitness = totalScore;
        return totalScore;
    }

    private static int CountAdjacentMines(List<List<char>> board, int cx, int cy, int width, int height)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = cx + dx;
                int ny = cy + dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (board[nx][ny] == 'M')
                        count++;
                }
            }
        }
        return count;
    }
}