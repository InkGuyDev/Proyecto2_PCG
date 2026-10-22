using System.Collections.Generic;
using UnityEngine;

public class RandomStartState : MonoBehaviour
{
    [Header("Configuración de Máscaras")]
    [Tooltip("Más máscaras = áreas protegidas más pequeñas. Menos máscaras = áreas más grandes.")]
    [Min(1)]
    [SerializeField] private int maskCount = 3;

    [Tooltip("Porcentaje aproximado del tablero que quedará protegido por máscaras (0.1 = 10% a 0.4 = 40%)")]
    [Range(0.05f, 0.45f)]
    [SerializeField] private float totalMaskCoverage = 0.20f;

    public int MaskCount
    {
        get => maskCount;
        set => maskCount = Mathf.Max(1, value);
    }

    public float TotalMaskCoverage
    {
        get => totalMaskCoverage;
        set => totalMaskCoverage = Mathf.Clamp(value, 0.05f, 0.45f);
    }

    // Genera el estado inicial del tablero y la matriz de máscaras.
    public List<List<char>> GenerateInitialBoard(int width, int height, int mineCount, out bool[,] maskGrid)
    {
        maskGrid = new bool[width, height];
        List<List<char>> board = new List<List<char>>();

        // 1. Inicializar el tablero vacío ('E' = Empty)
        for (int x = 0; x < width; x++)
        {
            List<char> column = new List<char>();
            for (int y = 0; y < height; y++)
            {
                column.Add('E');
                maskGrid[x, y] = false;
            }
            board.Add(column);
        }

        // 2. Generar las máscaras (zonas seguras / bloqueadas que garantizan solubilidad inicial)
        GenerateMasks(width, height, maskGrid);

        // 3. Recolectar todas las posiciones modificables (donde maskGrid es false)
        List<Vector2Int> modifiablePositions = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!maskGrid[x, y])
                {
                    modifiablePositions.Add(new Vector2Int(x, y));
                }
            }
        }

        // Asegurar que no intentemos poner más minas que espacios disponibles
        int safeMineCount = Mathf.Min(mineCount, modifiablePositions.Count);

        // 4. Distribuir las minas aleatoriamente en las zonas modificables
        for (int i = 0; i < safeMineCount; i++)
        {
            int randomIndex = Random.Range(0, modifiablePositions.Count);
            Vector2Int pos = modifiablePositions[randomIndex];

            board[pos.x][pos.y] = 'M'; // 'M' = Mina
            modifiablePositions.RemoveAt(randomIndex); // Evitar solapamiento
        }

        return board;
    }

    private void GenerateMasks(int width, int height, bool[,] maskGrid)
    {
        int totalCells = width * height;
        int targetProtectedCells = Mathf.RoundToInt(totalCells * totalMaskCoverage);

        // A mayor cantidad de máscaras, menor tamaño tendrá cada una
        int cellsPerMask = Mathf.Max(1, targetProtectedCells / maskCount);
        int maskRadius = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(cellsPerMask) / 2f));

        for (int m = 0; m < maskCount; m++)
        {
            int centerX = Random.Range(0, width);
            int centerY = Random.Range(0, height);

            for (int dx = -maskRadius; dx <= maskRadius; dx++)
            {
                for (int dy = -maskRadius; dy <= maskRadius; dy++)
                {
                    int nx = centerX + dx;
                    int ny = centerY + dy;

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        maskGrid[nx, ny] = true;
                    }
                }
            }
        }
    }
}