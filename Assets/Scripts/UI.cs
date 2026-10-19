using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI : MonoBehaviour
{
    [Header("Referencias a los Scripts")]
    [SerializeField] private HillClimbing hillClimbing;
    [SerializeField] private GeneratorManager generatorManager;

    [Header("Parámetro 1: Preferencia de Densidad")]
    [SerializeField] private Toggle highDensityToggle;
    [SerializeField] private TMP_Text densityLabelText;

    [Header("Parámetro 2: Máximo de Iteraciones")]
    [SerializeField] private Slider maxIterationsSlider;
    [SerializeField] private TMP_Text maxIterationsValueText;

    [Header("Parámetro 3: Vecinos por Paso (Neighbors Per Step)")]
    [SerializeField] private Slider neighborsSlider;
    [SerializeField] private TMP_Text neighborsValueText;

    [Header("Botones de Acción")]
    [Tooltip("Crea un nuevo tablero aleatorio y nuevas máscaras desde cero")]
    [SerializeField] private Button generateNewInstanceButton;
    [Tooltip("Aplica la nueva configuración sobre exactamente el mismo tablero inicial actual")]
    [SerializeField] private Button runSameInstanceButton;

    [Header("Panel de Métricas y Comparación")]
    [SerializeField] private TMP_Text metricsComparisonText;

    private string previousRunSummary = "Sin ejecución previa en esta instancia.";
    private int runCounterOnSameInstance = 0;

    void Start()
    {
        if (hillClimbing == null || generatorManager == null)
        {
            Debug.LogError("[HillClimbingUI] Faltan referencias a HillClimbing o GeneratorManager.");
            return;
        }

        if (highDensityToggle != null)
        {
            highDensityToggle.isOn = hillClimbing.PreferHighDensity;
            UpdateDensityText(highDensityToggle.isOn);
            highDensityToggle.onValueChanged.AddListener(OnDensityToggleChanged);
        }

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

        if (generateNewInstanceButton != null)
        {
            generateNewInstanceButton.onClick.AddListener(OnGenerateNewInstanceClicked);
        }

        if (runSameInstanceButton != null)
        {
            runSameInstanceButton.onClick.AddListener(OnRunSameInstanceClicked);
        }

        // Mostrar métricas del primer tablero generado en Start()
        runCounterOnSameInstance = 1;
        UpdateMetricsDisplay(isNewInstance: true);
    }

    public void OnDensityToggleChanged(bool isHighDensity)
    {
        hillClimbing.PreferHighDensity = isHighDensity;
        generatorManager.PreferHighDensity = isHighDensity;
        UpdateDensityText(isHighDensity);
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

    /// <summary>
    /// Botón 1: Genera una nueva condición inicial aleatoria desde cero.
    /// </summary>
    public void OnGenerateNewInstanceClicked()
    {
        runCounterOnSameInstance = 1;
        generatorManager.NewDificult();
        UpdateMetricsDisplay(isNewInstance: true);
    }

    /// <summary>
    /// Botón 2: Mantiene la misma condición inicial y ejecuta los nuevos parámetros para comparar.
    /// </summary>
    public void OnRunSameInstanceClicked()
    {
        runCounterOnSameInstance++;
        generatorManager.RunOnSameInitialState();
        UpdateMetricsDisplay(isNewInstance: false);
    }

    private void UpdateMetricsDisplay(bool isNewInstance)
    {
        if (metricsComparisonText == null) return;

        float initFit = hillClimbing.LastInitialFitness;
        float finalFit = hillClimbing.LastFinalFitness;
        float delta = finalFit - initFit;
        int iters = hillClimbing.LastIterationsExecuted;
        float timeMs = hillClimbing.LastExecutionTimeMs;
        string densityMode = hillClimbing.PreferHighDensity ? "Alta" : "Baja";

        string currentSummary =
            $"<b>Config #{runCounterOnSameInstance}</b> (Dens: {densityMode} | MaxIter: {hillClimbing.MaxIterations} | Vecinos: {hillClimbing.NeighborsPerStep})\n" +
            $"• Fitness Inicial: {initFit:F1} -> <b>Final: {finalFit:F1}</b> (Mejora: +{delta:F1})\n" +
            $"• Iteraciones realizadas: {iters} / {hillClimbing.MaxIterations} ({timeMs:F2} ms)";

        if (isNewInstance)
        {
            metricsComparisonText.text =
                $"<b>[NUEVA CONDICIÓN INICIAL]</b>\n" +
                $"{currentSummary}\n\n" +
                $"<i>Modifica los sliders y presiona 'Probar en Misma Instancia' para comparar.</i>";
        }
        else
        {
            metricsComparisonText.text =
                $"<b>[COMPARACIÓN EN MISMA CONDICIÓN INICIAL]</b>\n" +
                $"<color=#CCCCCC>{previousRunSummary}</color>\n" +
                $"----------------------------------------\n" +
                $"{currentSummary}";
        }

        previousRunSummary = currentSummary;
    }

    private void UpdateDensityText(bool isHighDensity)
    {
        if (densityLabelText != null)
        {
            densityLabelText.text = isHighDensity
                ? "Densidad: Alta (Agrupar)"
                : "Densidad: Baja (Dispersar)";
        }
    }

    private void UpdateIterationsText(int value)
    {
        if (maxIterationsValueText != null)
            maxIterationsValueText.text = $"Max Iteraciones: {value}";
    }

    private void UpdateNeighborsText(int value)
    {
        if (neighborsValueText != null)
            neighborsValueText.text = $"Vecinos por paso: {value}";
    }
}