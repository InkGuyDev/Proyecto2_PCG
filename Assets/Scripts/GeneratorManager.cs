using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GeneratorManager : MonoBehaviour
{
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Difficult,
        Variable
    }

    [Header("Malla (para interpretar la matriz)")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap tilemap;

    [Header("Tiles para el Tilemap (Visualización)")]
    [Tooltip("Tile para representar una mina ('M')")]
    [SerializeField] private TileBase mineTile;
    [Tooltip("Tile para casilla sin minas alrededor ('0' o 'E')")]
    [SerializeField] private TileBase emptyTile;
    [Tooltip("Tile para distinguir visualmente las zonas de máscara vacías")]
    [SerializeField] private TileBase maskTile;
    [Tooltip("Arreglo de 8 Tiles para los números del 1 al 8 (índice 0 = número 1, ..., índice 7 = número 8)")]
    [SerializeField] private TileBase[] numberTiles = new TileBase[8];

    // Matriz de generación
    private List<List<char>> matriz = new List<List<char>>();
    private bool[,] currentMaskGrid;

    [Header("Valores de Dificultad (Dimensiones X e Y)")]
    [SerializeField] private DifficultyLevel selectedDifficulty = DifficultyLevel.Easy;
    [SerializeField] private Vector2Int DifEasy = new Vector2Int(10, 10);
    [SerializeField] private Vector2Int DifMedium = new Vector2Int(20, 30);
    [SerializeField] private Vector2Int DifDificult = new Vector2Int(30, 60);
    [SerializeField] private Vector2Int DifVariable = new Vector2Int(5, 5);

    [Header("Configuración de Minas y Densidad")]
    [Tooltip("Porcentaje del tablero que tendrá minas (ej. 0.18 = 18% de las casillas)")]
    [Range(0.05f, 0.50f)]
    [SerializeField] private float minePercentage = 0.18f;

    [Tooltip("True = Mayor densidad (agrupar minas). False = Menor densidad (separar minas).")]
    [SerializeField] private bool preferHighDensity = true;

    [Header("Algoritmos Activos")]
    [SerializeField] private bool useEvolutionaryStrategy = true;
    [SerializeField] private bool useHillClimbing = true;

    [Header("Códigos / Referencias")]
    [SerializeField] private RandomStartState randomStartState;
    [SerializeField] private EvolutionaryStrategy evolutionaryStrategy;
    [SerializeField] private HillClimbing hillClimbing;

    private List<List<char>> initialBoardSnapshot = new List<List<char>>();

    public bool PreferHighDensity
    {
        get => preferHighDensity;
        set
        {
            preferHighDensity = value;
            if (hillClimbing != null)
                hillClimbing.PreferHighDensity = value;
        }
    }

    void Start()
    {
        if (grid == null)
            grid = GetComponent<Grid>();

        DifEasy = new Vector2Int(10, 10);
        DifMedium = new Vector2Int(20, 30);
        DifDificult = new Vector2Int(30, 60);
        if (DifVariable.x <= 0 || DifVariable.y <= 0)
            DifVariable = new Vector2Int(5, 5);

        // Generar e interpretar el tablero al iniciar
        NewDificult();
    }

    void Update()
    {
        // Presiona Espacio en modo Play para regenerar el tablero y probar variaciones rápido
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NewDificult();
        }
    }

    // Configura las dimensiones según la dificultad seleccionada, ejecuta los algoritmos y pinta el Tilemap.
    [ContextMenu("Generar Nuevo Tablero (NewDificult)")]
    public void NewDificult()
    {
        Vector2Int dimensions = GetCurrentDimensions();
        int width = dimensions.x;
        int height = dimensions.y;

        int totalCells = width * height;
        int mineCount = Mathf.Max(1, Mathf.RoundToInt(totalCells * minePercentage));

        if (randomStartState == null)
        {
            Debug.LogError("[GeneratorManager] Falta asignar la referencia a RandomStartState.");
            return;
        }

        // 1. Generar un NUEVO estado inicial aleatorio y máscaras de protección
        List<List<char>> rawInitial = randomStartState.GenerateInitialBoard(width, height, mineCount, out currentMaskGrid);

        // Guardar una copia intacta de esta condición inicial para poder compararla después
        initialBoardSnapshot = CloneBoard(rawInitial);

        // 2. Ejecutar los algoritmos sobre una copia de la condición inicial
        RunAlgorithmsOnSnapshot();
    }

    // Re-ejecuta los algoritmos sobre EXACTAMENTE el mismo tablero inicial y máscaras guardados.
    // Ideal para comparar dos o más configuraciones distintas sobre la misma instancia.
    [ContextMenu("Re-evaluar Misma Condición Inicial")]
    public void RunOnSameInitialState()
    {
        if (initialBoardSnapshot == null || initialBoardSnapshot.Count == 0)
        {
            NewDificult();
            return;
        }

        RunAlgorithmsOnSnapshot();
    }

    private void RunAlgorithmsOnSnapshot()
    {
        // Partimos siempre desde un clon intacto del estado inicial guardado
        matriz = CloneBoard(initialBoardSnapshot);

        // Ejecutar Evolutionary Strategy (mu + lambda) si está activo
        if (useEvolutionaryStrategy && evolutionaryStrategy != null)
        {
            matriz = evolutionaryStrategy.Generate(matriz, currentMaskGrid, preferHighDensity);
        }

        // Ejecutar Hill Climbing para ajuste local con el mapa de calor si está activo
        if (useHillClimbing && hillClimbing != null)
        {
            hillClimbing.PreferHighDensity = preferHighDensity;
            matriz = hillClimbing.Optimize(matriz, currentMaskGrid);
        }

        // Calcular los números de pistas (0 a 8) y pintar en el Tilemap
        CalculateMinesweeperNumbers(matriz);
        Interpretation();
    }

    private List<List<char>> CloneBoard(List<List<char>> source)
    {
        List<List<char>> copy = new List<List<char>>(source.Count);
        for (int x = 0; x < source.Count; x++)
        {
            copy.Add(new List<char>(source[x]));
        }
        return copy;
    }

    // Permite cambiar la dificultad por código (por ejemplo, desde botones de la UI).
    public void SetDifficultyAndGenerate(int difficultyIndex)
    {
        selectedDifficulty = (DifficultyLevel)Mathf.Clamp(difficultyIndex, 0, 3);
        NewDificult();
    }

    private Vector2Int GetCurrentDimensions()
    {
        switch (selectedDifficulty)
        {
            case DifficultyLevel.Easy:
                return DifEasy;
            case DifficultyLevel.Medium:
                return DifMedium;
            case DifficultyLevel.Difficult:
                return DifDificult;
            case DifficultyLevel.Variable:
            default:
                return DifVariable;
        }
    }
    
    // Recorre la matriz y reemplaza las casillas 'E' (vacías) por el número de minas vecinas ('0' a '8').
    private void CalculateMinesweeperNumbers(List<List<char>> board)
    {
        int width = board.Count;
        int height = width > 0 ? board[0].Count : 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x][y] == 'M')
                    continue;

                int count = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            if (board[nx][ny] == 'M')
                                count++;
                        }
                    }
                }

                // Convierte el conteo (0-8) en su carácter correspondiente ('0'-'8')
                board[x][y] = (char)('0' + count);
            }
        }
    }

    // Interpreta la matriz de caracteres y coloca los Tiles correspondientes en el Tilemap.
    public void Interpretation()
    {
        if (tilemap == null)
        {
            Debug.LogWarning("[GeneratorManager] No se ha asignado un Tilemap para interpretar la matriz.");
            return;
        }

        tilemap.ClearAllTiles();

        int width = matriz.Count;
        int height = width > 0 ? matriz[0].Count : 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                char cellValue = matriz[x][y];
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                TileBase tileToPlace = null;

                if (cellValue == 'M')
                {
                    tileToPlace = mineTile;
                }
                else if (cellValue == '0' || cellValue == 'E')
                {
                    // Si asignaste un tile especial para visualizar la máscara, lo usa en casillas vacías protegidas
                    if (maskTile != null && currentMaskGrid != null && currentMaskGrid[x, y])
                        tileToPlace = maskTile;
                    else
                        tileToPlace = emptyTile;
                }
                else if (cellValue >= '1' && cellValue <= '8')
                {
                    int numberIndex = (cellValue - '1'); // '1' -> índice 0, '8' -> índice 7
                    if (numberTiles != null && numberIndex < numberTiles.Length && numberTiles[numberIndex] != null)
                    {
                        tileToPlace = numberTiles[numberIndex];
                    }
                    else
                    {
                        tileToPlace = emptyTile;
                    }
                }

                if (tileToPlace != null)
                {
                    tilemap.SetTile(tilePosition, tileToPlace);
                }
            }
        }

        Debug.Log($"[GeneratorManager] Tablero interpretado en Tilemap ({width}x{height}). Densidad alta: {preferHighDensity}");
    }
}