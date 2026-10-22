using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI : MonoBehaviour
{
    [Header("Referencias a los Scripts del Proyecto")]
    [SerializeField] private GeneratorManager generatorManager;
    [SerializeField] private RandomStartState randomStartState;
    [SerializeField] private EvolutionaryStrategy evolutionaryStrategy;
    [SerializeField] private HillClimbing hillClimbing;

    [Header("1. Controles de GeneratorManager")]
    [Tooltip("Dropdown con 4 opciones: 0=Easy (10x10), 1=Medium (20x30), 2=Difficult (30x60), 3=Variable")]
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Slider minePercentageSlider;
    [SerializeField] private TMP_Text minePercentageText;
    [SerializeField] private Toggle useESToggle;
    [SerializeField] private Toggle useHCToggle;
    [SerializeField] private Toggle highDensityToggle;
    [SerializeField] private TMP_Text densityLabelText;

    [Header("2. Controles de RandomStartState (Máscaras)")]
    [SerializeField] private Slider maskCountSlider;
    [SerializeField] private TMP_Text maskCountText;
    [SerializeField] private Slider maskCoverageSlider;
    [SerializeField] private TMP_Text maskCoverageText;

    [Header("3. Controles de EvolutionaryStrategy (mu + lambda)")]
    [SerializeField] private Slider muSlider;
    [SerializeField] private TMP_Text muText;
    [SerializeField] private Slider lambdaSlider;
    [SerializeField] private TMP_Text lambdaText;
    [SerializeField] private Slider generationsSlider;
    [SerializeField] private TMP_Text generationsText;
    [SerializeField] private Slider mutationRateSlider;
    [SerializeField] private TMP_Text mutationRateText;

    [Header("4. Controles de HillClimbing")]
    [SerializeField] private Slider maxIterationsSlider;
    [SerializeField] private TMP_Text maxIterationsValueText;
    [SerializeField] private Slider neighborsSlider;
    [SerializeField] private TMP_Text neighborsValueText;

    [Header("5. Botones de Ejecución y Comparación")]
    [Tooltip("Genera un nuevo estado inicial aleatorio y nuevas máscaras")]
    [SerializeField] private Button generateNewInstanceButton;
    [Tooltip("Re-ejecuta los algoritmos activos sobre el mismo estado inicial guardado")]
    [SerializeField] private Button runSameInstanceButton;
    [SerializeField] private TMP_Text metricsComparisonText;

    private string previousRunSummary = "Sin ejecución previa en esta instancia.";
    private int runCounterOnSameInstance = 0;

    [Header("6. Sistema de Paginación de Paneles")]
    [Tooltip("Arrastra aquí los paneles en orden (Ej: 0=Manager, 1=RandomStart, 2=ES, 3=HillClimbing)")]
    [SerializeField] private GameObject[] pages;
    [Tooltip("Nombres opcionales para mostrar en el encabezado de cada página")]
    [SerializeField]
    private string[] pageTitles = new string[]
    {
        "1/4: General y Dificultad",
        "2/4: Estado Inicial y Máscaras",
        "3/4: Evolutionary Strategy (? + ?)",
        "4/4: Hill Climbing"
    };
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TMP_Text pageTitleText;

    private int currentPageIndex = 0;

    // =========================================================
    // 2. LLAMA A InitializePagination() DENTRO DE Start()
    // =========================================================
    void Start()
    {
        InitializeManagerControls();
        InitializeRandomStartControls();
        InitializeEvolutionaryControls();
        InitializeHillClimbingControls();
        InitializeButtons();

        InitializePagination(); // <-- Nueva llamada para activar la primera página

        runCounterOnSameInstance = 1;
        UpdateMetricsDisplay(isNewInstance: true);
    }

    // Permite cambiar de página también con las flechas del teclado (Izquierda / Derecha)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousPage();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextPage();
        }
    }

    // =========================================================
    // INICIALIZACIÓN DE CONTROLES
    // =========================================================

    private void InitializeManagerControls()
    {
        if (generatorManager == null) return;

        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Fácil (10x10)",
                "Medio (20x30)",
                "Difícil (30x60)",
                "Variable"
            });
            difficultyDropdown.value = (int)generatorManager.SelectedDifficulty;
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }

        if (minePercentageSlider != null)
        {
            minePercentageSlider.wholeNumbers = false;
            minePercentageSlider.minValue = 0.05f;
            minePercentageSlider.maxValue = 0.50f;
            minePercentageSlider.value = generatorManager.MinePercentage;
            UpdateMinePercentageText(generatorManager.MinePercentage);
            minePercentageSlider.onValueChanged.AddListener(OnMinePercentageChanged);
        }

        if (useESToggle != null)
        {
            useESToggle.isOn = generatorManager.UseEvolutionaryStrategy;
            useESToggle.onValueChanged.AddListener(val => generatorManager.UseEvolutionaryStrategy = val);
        }

        if (useHCToggle != null)
        {
            useHCToggle.isOn = generatorManager.UseHillClimbing;
            useHCToggle.onValueChanged.AddListener(val => generatorManager.UseHillClimbing = val);
        }

        if (highDensityToggle != null)
        {
            highDensityToggle.isOn = generatorManager.PreferHighDensity;
            UpdateDensityText(highDensityToggle.isOn);
            highDensityToggle.onValueChanged.AddListener(OnDensityToggleChanged);
        }
    }

    private void InitializeRandomStartControls()
    {
        if (randomStartState == null) return;

        if (maskCountSlider != null)
        {
            maskCountSlider.wholeNumbers = true;
            maskCountSlider.minValue = 1;
            maskCountSlider.maxValue = 15;
            maskCountSlider.value = randomStartState.MaskCount;
            UpdateMaskCountText(randomStartState.MaskCount);
            maskCountSlider.onValueChanged.AddListener(OnMaskCountChanged);
        }

        if (maskCoverageSlider != null)
        {
            maskCoverageSlider.wholeNumbers = false;
            maskCoverageSlider.minValue = 0.05f;
            maskCoverageSlider.maxValue = 0.45f;
            maskCoverageSlider.value = randomStartState.TotalMaskCoverage;
            UpdateMaskCoverageText(randomStartState.TotalMaskCoverage);
            maskCoverageSlider.onValueChanged.AddListener(OnMaskCoverageChanged);
        }
    }

    private void InitializeEvolutionaryControls()
    {
        if (evolutionaryStrategy == null) return;

        if (muSlider != null)
        {
            muSlider.wholeNumbers = true;
            muSlider.minValue = 1;
            muSlider.maxValue = 50;
            muSlider.value = evolutionaryStrategy.Mu;
            UpdateMuText(evolutionaryStrategy.Mu);
            muSlider.onValueChanged.AddListener(OnMuChanged);
        }

        if (lambdaSlider != null)
        {
            lambdaSlider.wholeNumbers = true;
            lambdaSlider.minValue = 1;
            lambdaSlider.maxValue = 150;
            lambdaSlider.value = evolutionaryStrategy.Lambda;
            UpdateLambdaText(evolutionaryStrategy.Lambda);
            lambdaSlider.onValueChanged.AddListener(OnLambdaChanged);
        }

        if (generationsSlider != null)
        {
            generationsSlider.wholeNumbers = true;
            generationsSlider.minValue = 1;
            generationsSlider.maxValue = 300;
            generationsSlider.value = evolutionaryStrategy.Generations;
            UpdateGenerationsText(evolutionaryStrategy.Generations);
            generationsSlider.onValueChanged.AddListener(OnGenerationsChanged);
        }

        if (mutationRateSlider != null)
        {
            mutationRateSlider.wholeNumbers = false;
            mutationRateSlider.minValue = 0.01f;
            mutationRateSlider.maxValue = 1.0f;
            mutationRateSlider.value = evolutionaryStrategy.MutationRate;
            UpdateMutationRateText(evolutionaryStrategy.MutationRate);
            mutationRateSlider.onValueChanged.AddListener(OnMutationRateChanged);
        }
    }

    private void InitializeHillClimbingControls()
    {
        if (hillClimbing == null) return;

        if (maxIterationsSlider != null)
        {
            maxIterationsSlider.wholeNumbers = true;
            maxIterationsSlider.minValue = 10;
            maxIterationsSlider.maxValue = 1000;
            maxIterationsSlider.value = hillClimbing.MaxIterations;
            UpdateIterationsText(hillClimbing.MaxIterations);
            maxIterationsSlider.onValueChanged.AddListener(OnMaxIterationsChanged);
        }

        if (neighborsSlider != null)
        {
            neighborsSlider.wholeNumbers = true;
            neighborsSlider.minValue = 1;
            neighborsSlider.maxValue = 100;
            neighborsSlider.value = hillClimbing.NeighborsPerStep;
            UpdateNeighborsText(hillClimbing.NeighborsPerStep);
            neighborsSlider.onValueChanged.AddListener(OnNeighborsChanged);
        }
    }

    private void InitializeButtons()
    {
        if (generateNewInstanceButton != null)
            generateNewInstanceButton.onClick.AddListener(OnGenerateNewInstanceClicked);

        if (runSameInstanceButton != null)
            runSameInstanceButton.onClick.AddListener(OnRunSameInstanceClicked);
    }

    // =========================================================
    // EVENTOS DE LA INTERFAZ
    // =========================================================

    public void OnDifficultyChanged(int index)
    {
        generatorManager.SelectedDifficulty = (GeneratorManager.DifficultyLevel)index;
    }

    public void OnMinePercentageChanged(float value)
    {
        generatorManager.MinePercentage = value;
        UpdateMinePercentageText(value);
    }

    public void OnDensityToggleChanged(bool isHighDensity)
    {
        generatorManager.PreferHighDensity = isHighDensity;
        if (hillClimbing != null)
            hillClimbing.PreferHighDensity = isHighDensity;
        UpdateDensityText(isHighDensity);
    }

    public void OnMaskCountChanged(float value)
    {
        int intVal = Mathf.RoundToInt(value);
        randomStartState.MaskCount = intVal;
        UpdateMaskCountText(intVal);
    }

    public void OnMaskCoverageChanged(float value)
    {
        randomStartState.TotalMaskCoverage = value;
        UpdateMaskCoverageText(value);
    }

    public void OnMuChanged(float value)
    {
        int intVal = Mathf.RoundToInt(value);
        evolutionaryStrategy.Mu = intVal;
        UpdateMuText(intVal);
    }

    public void OnLambdaChanged(float value)
    {
        int intVal = Mathf.RoundToInt(value);
        evolutionaryStrategy.Lambda = intVal;
        UpdateLambdaText(intVal);
    }

    public void OnGenerationsChanged(float value)
    {
        int intVal = Mathf.RoundToInt(value);
        evolutionaryStrategy.Generations = intVal;
        UpdateGenerationsText(intVal);
    }

    public void OnMutationRateChanged(float value)
    {
        evolutionaryStrategy.MutationRate = value;
        UpdateMutationRateText(value);
    }

    public void OnMaxIterationsChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value);
        hillClimbing.MaxIterations = intValue;
        UpdateIterationsText(intValue);
    }

    public void OnNeighborsChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value);
        hillClimbing.NeighborsPerStep = intValue;
        UpdateNeighborsText(intValue);
    }

    public void OnGenerateNewInstanceClicked()
    {
        runCounterOnSameInstance = 1;
        generatorManager.NewDificult();
        UpdateMetricsDisplay(isNewInstance: true);
    }

    public void OnRunSameInstanceClicked()
    {
        runCounterOnSameInstance++;
        generatorManager.RunOnSameInitialState();
        UpdateMetricsDisplay(isNewInstance: false);
    }

    // =========================================================
    // ACTUALIZACIÓN DE TEXTOS Y REPORTE COMPARATIVO
    // =========================================================

    private void UpdateMetricsDisplay(bool isNewInstance)
    {
        if (metricsComparisonText == null || generatorManager == null) return;

        string densityMode = generatorManager.PreferHighDensity ? "Alta" : "Baja";
        string activeAlgos = $"ES:{(generatorManager.UseEvolutionaryStrategy ? "ON" : "OFF")} | HC:{(generatorManager.UseHillClimbing ? "ON" : "OFF")}";

        string hcStats = "";
        if (generatorManager.UseHillClimbing && hillClimbing != null)
        {
            float initFit = hillClimbing.LastInitialFitness;
            float finalFit = hillClimbing.LastFinalFitness;
            float delta = finalFit - initFit;
            hcStats = $"• HC Fitness: {initFit:F1} -> <b>{finalFit:F1}</b> (+{delta:F1}) | Iter: {hillClimbing.LastIterationsExecuted}/{hillClimbing.MaxIterations} ({hillClimbing.LastExecutionTimeMs:F1} ms)";
        }
        else if (generatorManager.UseEvolutionaryStrategy && evolutionaryStrategy != null)
        {
            hcStats = $"• ES Best Fitness: <b>{evolutionaryStrategy.LastBestFitness:F1}</b> (Gen: {evolutionaryStrategy.Generations}, ?:{evolutionaryStrategy.Mu}, ?:{evolutionaryStrategy.Lambda})";
        }
        else
        {
            hcStats = "• Solo Estado Inicial (Algoritmos desactivados)";
        }

        string currentSummary =
            $"<b>Config #{runCounterOnSameInstance}</b> [{activeAlgos} | Dens: {densityMode}]\n" +
            $"{hcStats}";

        if (isNewInstance)
        {
            metricsComparisonText.text =
                $"<b>[NUEVA INSTANCIA ({generatorManager.SelectedDifficulty})]</b>\n" +
                $"{currentSummary}\n" +
                $"<i>Ajusta parámetros y pulsa 'Probar en Misma Instancia'.</i>";
        }
        else
        {
            metricsComparisonText.text =
                $"<b>[COMPARACIÓN EN MISMA INSTANCIA]</b>\n" +
                $"<color=#BBBBBB>{previousRunSummary}</color>\n" +
                $"----------------------------------\n" +
                $"{currentSummary}";
        }

        previousRunSummary = currentSummary;
    }

    private void UpdateMinePercentageText(float val)
    {
        if (minePercentageText != null)
            minePercentageText.text = $"Minas: {Mathf.RoundToInt(val * 100f)}%";
    }

    private void UpdateDensityText(bool isHighDensity)
    {
        if (densityLabelText != null)
            densityLabelText.text = isHighDensity ? "Densidad: Alta (Agrupar)" : "Densidad: Baja (Dispersar)";
    }

    private void UpdateMaskCountText(int val)
    {
        if (maskCountText != null)
            maskCountText.text = $"Cant. Máscaras: {val}";
    }

    private void UpdateMaskCoverageText(float val)
    {
        if (maskCoverageText != null)
            maskCoverageText.text = $"Cobertura Máscaras: {Mathf.RoundToInt(val * 100f)}%";
    }

    private void UpdateMuText(int val)
    {
        if (muText != null)
            muText.text = $"Padres (?): {val}";
    }

    private void UpdateLambdaText(int val)
    {
        if (lambdaText != null)
            lambdaText.text = $"Hijos (?): {val}";
    }

    private void UpdateGenerationsText(int val)
    {
        if (generationsText != null)
            generationsText.text = $"Generaciones: {val}";
    }

    private void UpdateMutationRateText(float val)
    {
        if (mutationRateText != null)
            mutationRateText.text = $"Mutación: {Mathf.RoundToInt(val * 100f)}%";
    }

    private void UpdateIterationsText(int value)
    {
        if (maxIterationsValueText != null)
            maxIterationsValueText.text = $"Max Iteraciones HC: {value}";
    }

    private void UpdateNeighborsText(int value)
    {
        if (neighborsValueText != null)
            neighborsValueText.text = $"Vecinos por paso HC: {value}";
    }

    // =========================================================
    // 3. AGREGA ESTOS MÉTODOS AL FINAL DE HillClimbingUI.cs
    // =========================================================
    private void InitializePagination()
    {
        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(PreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);

        ShowPage(0);
    }

    public void NextPage()
    {
        if (pages == null || pages.Length == 0) return;

        // Avanza de forma circular (si llega a la última, vuelve a la primera)
        currentPageIndex = (currentPageIndex + 1) % pages.Length;
        ShowPage(currentPageIndex);
    }

    public void PreviousPage()
    {
        if (pages == null || pages.Length == 0) return;

        // Retrocede de forma circular (si está en la 0, salta a la última)
        currentPageIndex--;
        if (currentPageIndex < 0)
            currentPageIndex = pages.Length - 1;

        ShowPage(currentPageIndex);
    }

    private void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0) return;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == index);
            }
        }

        if (pageTitleText != null)
        {
            if (pageTitles != null && index < pageTitles.Length && !string.IsNullOrEmpty(pageTitles[index]))
                pageTitleText.text = pageTitles[index];
            else
                pageTitleText.text = $"Página {index + 1} / {pages.Length}";
        }
    }
}