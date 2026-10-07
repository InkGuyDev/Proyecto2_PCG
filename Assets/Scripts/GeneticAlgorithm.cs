using System;
using System.Collections.Generic;
using UnityEngine;

public class GeneticAlgorithm : MonoBehaviour
{
    [Header("Genetic Algorithm")]

    [SerializeField] private int gridWidth = 10;   // Ancho del mapa en Tiles
    [SerializeField] private int gridHeight = 10;  // Alto del mapa en Tiles
    [SerializeField] private int lightsLength = 6; // Cantidad de luces

    /*
     * populationSize = cantidad de individuos de la población.
     *
     * Si populationSize = 50 y teamSize = 6:
     *
     * population = [
     *   [25, 131, 94, 212, 135, 445],   // individuo 0
     *   [6, 150, 197, 376, 59, 248],     // individuo 1
     *   [143, 448, 130, 214, 94, 445],   // individuo 2
     *   ...
     *   [...]                              // individuo 49
     * ]
     *
     * population.Count = 50
     * population[i].genes.Length = 6
     *
     * Cada gen es el índice de un Pokémon dentro del dataset.
     */
    [Min(2)]
    [SerializeField] private int populationSize = 50;

    /*
     * generations = cantidad de veces que repetimos el ciclo evolutivo.
     *
     * generations = 80
     *
     * Generation 0  -> población inicial
     * Generation 1  -> nueva población
     * ...
     * Generation 80 -> población final
     */
    [Min(1)]
    [SerializeField] private int generations = 80;

    /*
     * crossoverRate = probabilidad de cruzar dos padres.
     *
     * crossoverRate = 0.80  -> aproximadamente 80%.
     */
    [Range(0f, 1f)]
    [SerializeField] private float crossoverRate = 0.80f;

    /*
     * mutationRate = probabilidad de reemplazar cada gen.
     *
     * mutationRate = 0.12
     *
     * Cada una de las 6 posiciones del equipo tiene 12%
     * de probabilidad de ser reemplazada por otro Pokémon.
     */
    [Range(0f, 1f)]
    [SerializeField] private float mutationRate = 0.12f;

    /*
     * tournamentSize = cantidad de individuos que compiten
     * para seleccionar un padre.
     *
     * tournamentSize = 3
     *
     * candidatos:
     * A -> fitness 0.72
     * B -> fitness 0.91
     * C -> fitness 0.84
     *
     * seleccionado -> B
     */
    [Min(2)]
    [SerializeField] private int tournamentSize = 3;

    /*
     * elitism = mejores individuos que pasan directamente
     * a la siguiente generación.
     *
     * elitism = 2
     *
     * nextPopulation comienza con:
     * [bestIndividual, secondBestIndividual]
     */
    [Min(0)]
    [SerializeField] private int elitism = 2;

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;

    /*
     * ============================================================
     * ALGORITMO GENÉTICO
     * ============================================================
     *
     * PSEUDOCÓDIGO GENERAL
     * ------------------------------------------------------------
     * 1: create random population
     * 2: evaluate every individual
     *
     * 3: repeat for N generations:
     * 4:      sort population by fitness
     * 5:      create empty nextPopulation
     * 6:      copy elite individuals
     *
     * 7:      while nextPopulation is not full:
     * 8:          parentA = tournament selection
     * 9:          parentB = tournament selection
     *
     * 10:         if random < crossoverRate:
     * 11:             child = crossover(parentA, parentB)
     * 12:         else:
     * 13:             child = copy(parentA)
     *
     * 14:         mutate(child)
     * 15:         evaluate(child)
     * 16:         add child to nextPopulation
     *
     * 17:     population = nextPopulation
     *
     * 18: select generated content from final population
     *
     * EJEMPLO DE UNA GENERACIÓN
     * ------------------------------------------------------------
     * populationSize = 4
     *
     * A = [25, 131, 94, 212, 135, 445] fitness 0.92
     * B = [6, 150, 197, 376, 59, 248]   fitness 0.87
     * C = [1, 4, 7, 25, 39, 52]         fitness 0.63
     * D = [10, 11, 12, 13, 14, 15]      fitness 0.55
     *
     * elitism = 1
     *
     * nextPopulation empieza como [A]
     * y luego se completa con hijos hasta volver a tener 4 individuos.
     */

    //[System.Serializable]
    public class Vector2IntCandidate
    {
        public Vector2Int[] genes;
        public float fitness;

        public Vector2IntCandidate(int length)
        {
            genes = new Vector2Int[length];
            fitness = 0f;
        }

        public Vector2IntCandidate Clone()
        {
            Vector2IntCandidate clone = new Vector2IntCandidate(genes.Length);
            Array.Copy(this.genes, clone.genes, genes.Length);
            clone.fitness = this.fitness;
            return clone;
        }
    }


    public List<Vector2Int> Generate()
    {
        //variable aleatoria 
        System.Random random = new System.Random();

        // creamos la poblacion mediante su funcion correspondiente
        // ver CreateInitialPopulation para los detalles
        List<Vector2IntCandidate> population = CreateInitialPopulation(lightsLength, gridWidth, gridHeight, random);

        // evaluamos la poblacion mediante su funcion correspondiente
        EvaluatePopulation(population);

        // en el inspector se pueden modificar la cantidad de generaciones que habran
        // segun la cantidad que se ingresen es la cantidad de iteraciones que haremos
        for (int gen = 0; gen < generations; gen++)
        {
            // mediante sort ordenamos la poblacion, mediante la expresion lambda (=>) le estamos
            //indicando en base a que queremos que la ordene, le decimos que tome dos equipos (a y b)
            //y que los compare en base a su fitness. Al final, deben quedar ordenados desde el que 
            //tenga menor fitness hasta el que tenga más.
            population.Sort((a, b) => b.fitness.CompareTo(a.fitness));

            //logProgress es una casilla que podemos activar desde el inspector
            //si esta activa mostrara el proceso de evolución atraves de la consola
            //if (logProgress)
            //{
            //    Debug.Log($"Generacion: {gen}, Fitness: {population[0].fitness}");
            //}

            // creamos la siguiente poblacion
            List<Vector2IntCandidate> nextPopulation = new List<Vector2IntCandidate>(populationSize);

            //ahora copiaremos los elementos elite (es decir los mejores valorados) 
            //utilizamos el método Clone() implementado en la clase PokemonTeamCandidate
            //en PokemonTeamController para realizar la copia
            for (int i = 0; i < elitism && i < population.Count; i++)
            {
                nextPopulation.Add(population[i].Clone()); //agregamos los elementos elite a la nueva poblacion
            }

            // mientras la poblacion no este llena
            while (nextPopulation.Count < populationSize)
            {
                // se seleccionan dos padres aleatorios evaluandolos con TournamentSelection
                // ver los detalles en la funcion correspondiente
                Vector2IntCandidate parentA = TournamentSelection(population, random);
                Vector2IntCandidate parentB = TournamentSelection(population, random);

                Vector2IntCandidate child;

                // usamos la variable aleatoria y si es menor a la probabilidad de cruce entre
                // los padres
                if (random.NextDouble() < crossoverRate)
                {
                    // los padres se cruzaran entre si mediante la funcion Crossover
                    // y de ahi saldra el hijo
                    child = Crossover(parentA, parentB, random);
                }
                else //si no
                {
                    // el hijo sera un clon directo del padre A
                    child = parentA.Clone();
                }

                // independiente de la forma en la que haya salido el hijo ahora será modificado
                // mediante la funcion mutate
                Mutate(child, gridWidth, gridHeight, random);

                // se evaluan las métricas (powerScore, diversityScore, fitness)
                EvaluateCandidate(child);

                // añadimos el hijo a la nueva poblacion
                nextPopulation.Add(child);
            }

            // ahora la poblacion que acabamos de crear sera la nuestra "poblacion actual"
            // para calcular la siguiente (si es que hay siguiente, el ciclo puede acabar)
            population = nextPopulation;
        }

        // de lo que generamos haremos la seleccion mediante el metodo correspondiente de la clase
        //PokemonTeamFitness del archivo PokemonTeamController usando la poblacion ultima poblacion
        //que generamos (a priori la mejor), en base a los paremetros de configuracion
        population.Sort((a, b) => b.fitness.CompareTo(a.fitness));
        return new List<Vector2Int>(population[0].genes);
    }

    /*
     * ============================================================
     * CREAR POBLACIÓN INICIAL
     * ============================================================
     *
     * populationSize = 50
     * teamSize = 6
     * datasetSize = 1032
     *
     * Un individuo posible:
     * [25, 131, 94, 212, 135, 445]
     *
     * Cada valor debe estar entre 0 y datasetSize - 1.
     *
     * Resultado:
     * population.Count = 50
     * population[i].genes.Length = 6
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: create empty population
     * 2: repeat populationSize times:
     * 3:      create candidate with teamSize genes
     * 4:      for each gene:
     * 5:          assign random dataset index
     * 6:      add candidate to population
     * 7: return population
     */
    private List<Vector2IntCandidate> CreateInitialPopulation(
        int lenght, int width, int height, System.Random random)
    {
        
        List<Vector2IntCandidate> population = new List<Vector2IntCandidate>(populationSize);

       
        for (int i = 0; i < populationSize; i++)
        {
            // Instanciamos el candidato pasándole al constructor config.teamSize, según la definición correcta en el controlador
            Vector2IntCandidate candidate = new Vector2IntCandidate(lenght);

            
            for (int j = 0; j < candidate.genes.Length; j++)
            {

                candidate.genes[j] = new Vector2Int(random.Next(0, width), random.Next(0, height));
            }

            
            population.Add(candidate);
        }

        
        return population;
    }

    /*
     * ============================================================
     * TOURNAMENT SELECTION
     * ============================================================
     *
     * tournamentSize = 3
     *
     * candidate A -> fitness 0.62
     * candidate B -> fitness 0.91
     * candidate C -> fitness 0.78
     *
     * ganador -> candidate B
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: best = null
     * 2: repeat tournamentSize times:
     * 3:      choose random candidate from population
     * 4:      if best is null OR candidate fitness > best fitness:
     * 5:          best = candidate
     * 6: return best
     */
    private Vector2IntCandidate TournamentSelection(
        List<Vector2IntCandidate> population,
        System.Random random)
    {
       
        Vector2IntCandidate best = null;

        
        for (int i = 0; i < tournamentSize; i++)
        {
          
            Vector2IntCandidate candidate = population[random.Next(population.Count)];

           
            if (best == null || candidate.fitness > best.fitness)
            {
                
                best = candidate;
            }
        }

       
        return best;
    }

    /*
     * ============================================================
     * CROSSOVER
     * ============================================================
     *
     * parentA:
     * [25, 131, 94 | 212, 135, 445]
     *
     * parentB:
     * [6, 150, 197 | 376, 59, 248]
     *
     * crossoverPoint = 3
     *
     * child:
     * [25, 131, 94 | 376, 59, 248]
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: create empty child
     * 2: choose crossoverPoint between 1 and geneCount - 1
     * 3: for each gene position:
     * 4:      if position < crossoverPoint:
     * 5:          copy gene from parentA
     * 6:      else:
     * 7:          copy gene from parentB
     * 8: return child
     */
    private Vector2IntCandidate Crossover(
        Vector2IntCandidate parentA,
        Vector2IntCandidate parentB,
        System.Random random)
    {
        // se usa el tamaño del arreglo de genes del padre A para el constructor
        int geneCount = parentA.genes.Length;
        Vector2IntCandidate child = new Vector2IntCandidate(geneCount);

        
        int crossoverPoint = random.Next(1, geneCount);

       
        for (int i = 0; i < geneCount; i++)
        {
            
            if (i < crossoverPoint)
            {
                
                child.genes[i] = parentA.genes[i];
            }
            else
            {
                
                child.genes[i] = parentB.genes[i];
            }
        }

        
        return child;
    }

    /*
     * ============================================================
     * MUTACIÓN
     * ============================================================
     *
     * mutationRate = 0.12
     *
     * Antes:
     * [25, 131, 94, 212, 135, 445]
     *
     * Si muta la posición 3:
     * [25, 131, 94, 700, 135, 445]
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: for each gene:
     * 2:      generate random value between 0 and 1
     * 3:      if random value < mutationRate:
     * 4:          replace gene with random dataset index
     */
    private void Mutate(
        Vector2IntCandidate candidate,
        int width, int height, System.Random random )
    {
        
        for (int i = 0; i < candidate.genes.Length; i++)
        {
           
            if (random.NextDouble() < mutationRate)
            {

                candidate.genes[i] = new Vector2Int(random.Next(0, width), random.Next(0, height));
            }
        }
    }

    private void EvaluatePopulation(
        List<Vector2IntCandidate> population)
    {
        foreach (Vector2IntCandidate candidate in population)
        {
            EvaluateCandidate(candidate);
        }
    }

    private void EvaluateCandidate(Vector2IntCandidate candidate)
    {
        float score = 0f;

        HashSet<Vector2Int> uniquePositions = new HashSet<Vector2Int>();
        foreach (Vector2Int pos in candidate.genes)
        {
            uniquePositions.Add(pos);
        }

        score += uniquePositions.Count * 10f;

        candidate.fitness = score;
    }
}
