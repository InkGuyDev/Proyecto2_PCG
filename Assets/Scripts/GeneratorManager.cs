using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GeneratorManager : MonoBehaviour
{
    // Malla (para interpretar la matriz)
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap tilemap;

    //Matriz de generacion
    List<List<char>> matriz = new List<List<char>>();

    //Valores de dificultad
    [SerializeField] private Vector2Int DifEasy;
    [SerializeField] private Vector2Int DifMedium;
    [SerializeField] private Vector2Int DifDificult;
    [SerializeField] private Vector2Int DifVariable;

    //Codigos
    [SerializeField] RandomStartState randomStartState;
    [SerializeField] EvolutionaryStrategy evolutionaryStrategy;
    //[SerializeField] HillClimbing hillClimbing;


    // Start is called before the first frame update
    void Start()
    {
        grid = GetComponent<Grid>();

        DifEasy = new Vector2Int(10, 10);
        DifMedium = new Vector2Int(20, 30);
        DifDificult = new Vector2Int(30, 60);
        DifVariable = new Vector2Int(5, 5);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NewDificult()
    {

    }

    public void Interpretation()
    {

    }
}
