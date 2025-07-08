using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class MoveSmartAgent : MonoBehaviour
{
    public GameObject grid;
    public GameObject goal;
    public GameObject TargetEffect;
    private GameObject Target;
    public GameObject CollisionEffect;
    public GameObject CollisionSoundEffect;
    public GameObject CelebrationEffect;
    private List<State> states;
    public float Speed = 1f;
    private string startStateName = "";
    private readonly List<Reward> qTable = new();
    private List<Reward> targetStates = new();
    private State chosenEndState = new();
    public event Action OnAgentComplete;

    void Start()
    {
        PositionAgentToClosestCell();
        LoadQTable();
        targetStates = qTable
                            .Where(r => r.start != null && r.start.GetName() == startStateName)
                            .ToList();

        chosenEndState = null;

        if (targetStates.Count > 0) // Target states with equal rewards (choose randomly)
        {
            // Step 2: Find the max reward
            float maxReward = targetStates.Max(r => r.reward);

            // Step 3: Filter transitions that have the max reward
            var bestTransitions = targetStates
                .Where(r => r.reward == maxReward)
                .ToList();

            // Step 4: Pick one randomly
            int randomIndex = UnityEngine.Random.Range(0, bestTransitions.Count);
            chosenEndState = bestTransitions[randomIndex].end;
            Target = Instantiate(TargetEffect, chosenEndState.GetPosition().position, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (this.transform.position != chosenEndState.GetPosition().position)
        {
            this.transform.position = Vector3.MoveTowards(
                transform.position,
                chosenEndState.GetPosition().position,
                Speed * Time.deltaTime
            );
            // Debug.Log("Moving");
        }
        else
        {
            if(Target != null)
            {
                Destroy(Target);
            }
            // Debug.Log("Setting New Target");
            startStateName = chosenEndState.GetName();

            targetStates = qTable
                            .Where(r => r.start != null && r.start.GetName() == startStateName)
                            .ToList();

            chosenEndState = null;

            if (targetStates.Count > 0) // Target states with equal rewards (choose randomly)
            {
                // Step 2: Find the max reward
                float maxReward = targetStates.Max(r => r.reward);

                // Step 3: Filter transitions that have the max reward
                var bestTransitions = targetStates
                    .Where(r => r.reward == maxReward)
                    .ToList();

                // Step 4: Pick one randomly
                int randomIndex = UnityEngine.Random.Range(0, bestTransitions.Count);
                chosenEndState = bestTransitions[randomIndex].end;
                Target = Instantiate(TargetEffect, chosenEndState.GetPosition().position, Quaternion.identity);
            }
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Instantiate(CollisionEffect, this.transform.position, Quaternion.identity);
            Instantiate(CollisionSoundEffect, this.transform.position, Quaternion.identity);
            Destroy(Target);
            Destroy(this.gameObject);
            OnAgentComplete.Invoke();
            // Debug.Log("Failed");
        }
        else if (collision.gameObject.CompareTag("Goal"))
        {
            Instantiate(CelebrationEffect, new Vector3(0,1,0), Quaternion.identity);
            Destroy(Target);
            Destroy(this.gameObject);
            OnAgentComplete.Invoke();
            // Debug.Log("Goal");
        }
    }
    private void PositionAgentToClosestCell()
    {
        Vector3 pos;
        if (grid == null)
        {
            // Debug.LogError("Grid root not assigned.");
            return;
        }
        states = new List<State>();
        State state = null;
        Transform spawnPosition = null;
        float distance = 1000;
        foreach (Transform row in grid.transform) // Rows
        {
            foreach (Transform column in row) // Columns
            {
                pos = column.position;
                state = new State(column.name, column);
                states.Add(state);
                if (Vector3.Distance(this.transform.position, column.position) < distance)
                {
                    // Closest cell to the spawned agent
                    distance = Vector3.Distance(this.transform.position, column.position);
                    spawnPosition = column;
                    startStateName = column.name;
                }
                //// Debug.Log($"{row.name}/{column.name} → Position: {pos}");
            }
        }
        // Lastly, don't forget the goal state
        state = new State("Goal", goal.transform);
        states.Add(state);
        if (Vector3.Distance(this.transform.position, goal.transform.position) < distance)
        {
            distance = Vector3.Distance(this.transform.position, goal.transform.position);
            spawnPosition = goal.transform;
        }
        this.transform.position = spawnPosition.position;
        // Debug.Log("Agent Deployed");
    }
    private void LoadQTable()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("QStates");
        if (csvFile == null)
        {
            // Debug.LogError($"QState file not found.");
            return;
        }

        using (StringReader reader = new StringReader(csvFile.text))
        {
            bool headerSkipped = false;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!headerSkipped)
                {
                    headerSkipped = true; // Skip header line
                    continue;
                }

                string[] tokens = line.Split(',');
                if (tokens.Length == 3)
                {
                    string start = tokens[0].Trim();
                    string end = tokens[1].Trim();
                    if (float.TryParse(tokens[2].Trim(), out float reward))
                    {

                        State startState = states.FirstOrDefault(s => s.GetName().Trim() == start);
                        State endState = states.FirstOrDefault(s => s.GetName().Trim() == end);

                        if (startState == null || endState == null)
                        {
                            //// Debug.LogWarning($"Line {lineNum}: Failed to find state(s). Start: '{startName}', End: '{endName}'");
                            continue;
                        }

                        qTable.Add(new Reward(startState, endState, reward));
                    }
                    else
                    {
                        // Debug.LogWarning($"Invalid reward value: {tokens[2]}");
                    }
                }
            }
        }
        // Debug.Log("Rewards table has been successfully loaded (total records is " + qTable.Count + ").");
    }
    public class State
    {
        private string name;
        private Transform position;

        public State()
        {
            name = "";
            position = null;
        }

        public State(string name, Transform position)
        {
            this.name = name;
            this.position = position;
        }

        public string GetName()
        {
            return name;
        }

        public Transform GetPosition()
        {
            return this.position;
        }
    }
    public class Reward
    {
        public State start;
        public State end;
        public float reward;
        public Reward(State start, State end, float reward)
        {
            this.start = start;
            this.end = end;
            this.reward = reward;
        }
    }
}
