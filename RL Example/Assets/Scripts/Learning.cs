using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.UIElements;
using System.Linq;
using static Learning;
using System.Security.Cryptography.X509Certificates;


public class Learning : MonoBehaviour
{
    public GameObject grid;
    public GameObject goal;
    private List<State> states;
    public GameObject SuccessEffect;
    public GameObject CollisionEffect;
    public GameObject TargetEffect;
    public GameObject AgentPrefab;
    public GameObject PositiveReward;
    public GameObject NegativeReward;
    public GameObject NeutralReward;
    public float AgentSpeed;
    public float DelayBetweenAttempts;
    public float DiscountFactor;
    public float ExploitationRate;
    private readonly List<Reward> rTable = new();
    private List<Reward> qTable = new();
    public int action;
    public string ResourcesFolderPath;
    void Start()
    {
        BuildStateList();
        switch(action)
        {
            case 0: // Define the rewards table
                StartCoroutine(GenerateRewardsTable());
                break;
            case 1: // Use the rewards table to define the Q-Learning table
                LoadRewardsTable();
                InitializeQTable();
                StartCoroutine(CalculateQTable());
                break;
            default:
                break;
        }
    }

    void Update()
    {

    }

    IEnumerator GenerateRewardsTable()
    {
        yield return null;

        for (int i = 0; i < states.Count; i++)
        {
            State spawn = states[i];

            for (int j = 0; j < states.Count; j++)
            {
                if (i == j)
                {
                    if (states[i].GetName() == "Goal")
                    {
                        rTable.Add(new Reward(states[i], states[j], 100));
                        continue;
                    }
                    else
                    {
                        rTable.Add(new Reward(states[i], states[j], 0));
                        continue;
                    }
                }

                State target = states[j];

                // Instantiate & init
                GameObject go = Instantiate(
                    AgentPrefab,
                    spawn.GetPosition().position,
                    Quaternion.identity
                );

                var mover = go.GetComponent<MoveAgent>();
                mover.Initialize(states[i], states[j],
                    target.GetPosition(),
                    SuccessEffect,
                    CollisionEffect,
                    TargetEffect,
                    null,
                    AgentSpeed
                );

                bool done = false;
                // Wait for completion
                mover.OnAgentComplete += (s, e, r) =>
                {
                    rTable.Add(new Reward(s, e, r)); // Record Transition
                    done = true;
                };
                yield return new WaitUntil(() => done);

                // Pause so the special effects can play
                yield return new WaitForSeconds(DelayBetweenAttempts);
            }
        }

        ExportRTableToCSV(rTable);
        
        // Debug.Log("Exploration finished!");
    }
    IEnumerator CalculateQTable()
    {
        double epsilon = UnityEngine.Random.value;
        bool firstAttempt = true;

        while (true)
        {
            yield return null;

            bool done = false;

            State spawn = null;
            State target = null;

            if(firstAttempt)
            {
                int randomPairIndex = GetRandomIndexFromPositiveRewards(rTable);
                spawn = rTable[randomPairIndex].start;
                target = rTable[randomPairIndex].end;

                // Instantiate & init
                GameObject agent = Instantiate(
                    AgentPrefab,
                    spawn.GetPosition().position,
                    Quaternion.identity
                );

                var mover = agent.GetComponent<MoveAgent>();
                mover.Initialize(spawn, target,
                    target.GetPosition(),
                    SuccessEffect,
                    CollisionEffect,
                    TargetEffect,
                    null,
                    AgentSpeed
                );

                // Wait for completion
                mover.OnAgentComplete += (s, e, r) =>
                {
                    var stateAction = qTable.FirstOrDefault(r => r.start.GetName() == spawn.GetName() && r.end.GetName() == target.GetName());
                    if (stateAction != null)
                    {
                        // Q(S, A) = R(S, A) + Gamma * MAX[Q(Si, Ai)]
                        stateAction.reward = rTable[randomPairIndex].reward + (DiscountFactor * qTable
                                                                                                .Where(r => r.start.GetName() == target.GetName())
                                                                                                .Max(r => r.reward));
                        if (stateAction.reward > 0)
                        {
                            GameObject positiveReward = Instantiate(
                                PositiveReward,
                                target.GetPosition().position,
                                Quaternion.Euler(90f, 0f, 0f)
                            );
                            TextMesh tm = positiveReward.GetComponent<TextMesh>();
                            if (tm != null)
                            {
                                tm.text = "+" + Mathf.Round(stateAction.reward).ToString();
                                // Debug.Log("TextMesh found!");
                            }
                            else
                            {
                                // Debug.LogError("No TextMesh found!");
                            }
                            // Debug.Log("State-Action Pair (" + rTable[randomPairIndex].start.GetName() + ", " + rTable[randomPairIndex].end.GetName() + ") updated its reward to " + stateAction.reward);
                        }
                        else if (r < 0) // If the agent collides with the wall, penalize the agent and remove this action-state (might have been missed while determining possible paths)
                        {
                            GameObject positiveReward = Instantiate(
                                NegativeReward,
                                target.GetPosition().position,
                                Quaternion.Euler(90f, 0f, 0f)
                            );
                            // Debug.Log("State-Action Pair (" + rTable[randomPairIndex].start.GetName() + ", " + rTable[randomPairIndex].end.GetName() + ") updated its reward to -1");
                            rTable[randomPairIndex].reward = -1;
                            rTable.RemoveAll(r => r.reward == -1f);
                        }
                        else
                        {
                            GameObject positiveReward = Instantiate(
                                NeutralReward,
                                target.GetPosition().position,
                                Quaternion.Euler(90f, 0f, 0f)
                            );
                        }
                    }
                    //rTable.Add(new Reward(s, e, r)); // Record Transition
                    done = true;
                };
                firstAttempt = false;
            }
            else if (epsilon <= ExploitationRate)
            {
                int? index = GetRandomIndexFromPositiveRewards(qTable);

                if (index.HasValue)
                {
                    // Step 1: Get the selected Reward's start state
                    State selectedStart = qTable[index.Value].start;

                    // Step 2: Find all Reward objects whose `.end` matches this start
                    var candidates = rTable
                        .Select((r, i) => new { Reward = r, Index = i })
                        .Where(x => x.Reward.end.GetName() == selectedStart.GetName() && x.Reward.reward >= 0)
                        .ToList();

                    // Step 3: Randomly pick one from candidates
                    if (candidates.Count > 0)
                    {
                        int rand = Random.Range(0, candidates.Count); // UnityEngine.Random
                        int randomPairIndex = candidates[rand].Index;
                        // Convert to Q Table here

                        Reward selectedReward = rTable[randomPairIndex];
                        spawn = selectedReward.start;
                        target = selectedReward.end;
                        // Debug.Log($"Chained reward: {selectedReward.reward} from {selectedReward.start} → {selectedReward.end}");

                        // Instantiate & init
                        GameObject agent = Instantiate(
                            AgentPrefab,
                            spawn.GetPosition().position,
                            Quaternion.identity
                        );

                        var mover = agent.GetComponent<MoveAgent>();
                        mover.Initialize(spawn, target,
                            target.GetPosition(),
                            SuccessEffect,
                            CollisionEffect,
                            TargetEffect,
                            null,
                            AgentSpeed
                        );

                        // Wait for completion
                        mover.OnAgentComplete += (s, e, r) =>
                        {
                            var stateAction = qTable.FirstOrDefault(r => r.start.GetName() == spawn.GetName() && r.end.GetName() == target.GetName());
                            if (stateAction != null)
                            {
                                // Q(S, A) = R(S, A) + Gamma * MAX[Q(Si, Ai)]
                                stateAction.reward = rTable[randomPairIndex].reward + (DiscountFactor * qTable
                                                                                                        .Where(r => r.start.GetName() == target.GetName())
                                                                                                        .Max(r => r.reward));
                                if (stateAction.reward > 0)
                                {
                                    GameObject positiveReward = Instantiate(
                                        PositiveReward,
                                        target.GetPosition().position,
                                        Quaternion.Euler(90f, 0f, 0f)
                                    );
                                    // Debug.Log("State-Action Pair (" + rTable[randomPairIndex].start.GetName() + ", " + rTable[randomPairIndex].end.GetName() + ") updated its reward to " + stateAction.reward);
                                    TextMesh tm = positiveReward.GetComponent<TextMesh>();
                                    if (tm != null)
                                    {
                                        tm.text = "+" + Mathf.Round(stateAction.reward).ToString();
                                        // Debug.Log("TextMesh found!");
                                    }
                                    else
                                    {
                                        // Debug.LogError("No TextMesh found!");
                                    }
                                }
                                else if (r < 0) // If the agent collides with the wall, penalize the agent and remove this action-state (might have been missed while determining possible paths)
                                {
                                    GameObject positiveReward = Instantiate(
                                        NegativeReward,
                                        target.GetPosition().position,
                                        Quaternion.Euler(90f, 0f, 0f)
                                    );
                                    // Debug.Log("State-Action Pair (" + rTable[randomPairIndex].start.GetName() + ", " + rTable[randomPairIndex].end.GetName() + ") updated its reward to -1");
                                    rTable[randomPairIndex].reward = -1;
                                    rTable.RemoveAll(r => r.reward == -1f);
                                }
                                else
                                {
                                    GameObject positiveReward = Instantiate(
                                        NeutralReward,
                                        target.GetPosition().position,
                                        Quaternion.Euler(90f, 0f, 0f)
                                    );
                                }
                            }
                            //rTable.Add(new Reward(s, e, r)); // Record Transition
                            done = true;
                        };
                    }
                    else
                    {
                        // Debug.Log("No backward-linked rewards found.");
                    }
                }
                else
                {
                    // Debug.Log("No positive top-3 reward selected.");
                }
            }
            else
            {
                // Randomly pick a state-action pair
                // In our case, its moving from state to another

                int randomPairIndex = Random.Range(0, rTable.Count);

                spawn = rTable[randomPairIndex].start;
                target = rTable[randomPairIndex].end;

                // Instantiate & init
                GameObject agent = Instantiate(
                    AgentPrefab,
                    spawn.GetPosition().position,
                    Quaternion.identity
                );

                var mover = agent.GetComponent<MoveAgent>();
                mover.Initialize(spawn, target,
                    target.GetPosition(),
                    SuccessEffect,
                    CollisionEffect,
                    TargetEffect,
                    null,
                    AgentSpeed
                );

                // Wait for completion
                mover.OnAgentComplete += (s, e, r) =>
                {
                    var stateAction = qTable.FirstOrDefault(r => r.start.GetName() == spawn.GetName() && r.end.GetName() == target.GetName());
                    if (stateAction != null)
                    {
                        // Q(S, A) = R(S, A) + Gamma * MAX[Q(Si, Ai)]
                        stateAction.reward = rTable[randomPairIndex].reward + (DiscountFactor * qTable
                                                                                                .Where(r => r.start.GetName() == target.GetName())
                                                                                                .Max(r => r.reward));
                        if (stateAction.reward > 0)
                        {
                            GameObject positiveReward = Instantiate(
                                PositiveReward,
                                target.GetPosition().position,
                                Quaternion.Euler(90f, 0f, 0f)
                            );
                            TextMesh tm = positiveReward.GetComponent<TextMesh>();
                            if (tm != null)
                            {
                                tm.text = "+" + Mathf.Round(stateAction.reward).ToString();
                                // Debug.Log("TextMesh found!");
                            }
                            else
                            {
                                // Debug.LogError("No TextMesh found!");
                            }
                            // Debug.Log("State-Action Pair (" + rTable[randomPairIndex].start.GetName() + ", " + rTable[randomPairIndex].end.GetName() + ") updated its reward to " + stateAction.reward);
                        }
                        else if (r < 0) // If the agent collides with the wall, penalize the agent and remove this action-state (might have been missed while determining possible paths)
                        {
                            GameObject positiveReward = Instantiate(
                                NegativeReward,
                                target.GetPosition().position,
                                Quaternion.Euler(90f, 0f, 0f)
                            );
                            // Debug.Log("State-Action Pair (" + rTable[randomPairIndex].start.GetName() + ", " + rTable[randomPairIndex].end.GetName() + ") updated its reward to -1");
                            rTable[randomPairIndex].reward = -1;
                            rTable.RemoveAll(r => r.reward == -1f);
                        }
                        else
                        {
                            GameObject positiveReward = Instantiate(
                                NeutralReward,
                                target.GetPosition().position,
                                Quaternion.Euler(90f, 0f, 0f)
                            );
                        }
                    }
                    //rTable.Add(new Reward(s, e, r)); // Record Transition
                    done = true;
                };
            }

            yield return new WaitUntil(() => done);

            // Pause so the special effects can play
            yield return new WaitForSeconds(DelayBetweenAttempts);

            epsilon = UnityEngine.Random.value;

            ExportQTableToCSV(qTable);
        }
    }
    private void BuildStateList()
    {
        Vector3 pos;
        if (grid == null)
        {
            // Debug.LogError("Grid root not assigned.");
            return;
        }
        states = new List<State>();
        State state = null;
        foreach (Transform row in grid.transform) // Rows
        {
            foreach (Transform column in row) // Columns
            {
                pos = column.position;
                state = new State(column.name, column);
                states.Add(state);
                //// Debug.Log($"{row.name}/{column.name} → Position: {pos}");
            }
        }
        // Lastly, don't forget the goal state
        state = new State("Goal", goal.transform);
        states.Add(state);
        // Debug.Log("Total States = " + states.Count);
    }
    private void LoadRewardsTable()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("RStates");
        if (csvFile == null)
        {
            // Debug.LogError($"RState file not found.");
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

                        rTable.Add(new Reward(states.FirstOrDefault(s => s.GetName() == start),
                            states.FirstOrDefault(s => s.GetName() == end),
                            reward));
                    }
                    else
                    {
                        // Debug.LogWarning($"Invalid reward value: {tokens[2]}");
                    }
                }
            }
        }
        // Debug.Log("Rewards table has been successfully loaded (total records is " + rTable.Count + ").");
    }
    private void InitializeQTable()
    {
        qTable = new List<Reward>();
        Reward reward = null;
        for(int i = 0; i < rTable.Count; i++)
        {
            reward = new Reward(rTable[i].start, rTable[i].end, 0);
            qTable.Add(reward);
        }
        rTable.RemoveAll(r => r.reward == -1f);
        // Debug.Log("Q-Table has been successfully initialized (total records is " + qTable.Count + ")");
    }
    private void ExportQTableToCSV(List<Reward> rewards)
    {
        string filename = ResourcesFolderPath + @"\QStates.csv";
        try
        {
            using (StreamWriter writer = new StreamWriter(filename, false)) // false = overwrite
            {
                // Write CSV header
                writer.WriteLine("Start,End,reward");

                // Write each reward
                foreach (var r in rewards)
                {
                    string line = $"{r.start.GetName()},{r.end.GetName()},{r.reward}";
                    writer.WriteLine(line);
                }
            }
        }
        catch (IOException ex)
        {
            // Debug.LogError("Failed to export rewards: " + ex.Message);
        }
    }
    private void ExportRTableToCSV(List<Reward> states)
    {
        string filename = ResourcesFolderPath + @"\RStates.csv";
        string path = Path.Combine(Application.persistentDataPath, filename);

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Start,End,Reward");

        foreach (Reward record in states)
        {
            string line = $"{record.start.GetName()},{record.end.GetName()},{record.reward}";
            csv.AppendLine(line);
        }

        File.WriteAllText(path, csv.ToString());

        // Debug.Log($"Exported {states.Count} transitions to CSV: {path}");
    }
    private static int GetRandomIndexFromPositiveRewards(List<Reward> rewards)
    {
        // Step 1: Filter for positive rewards and track original indices
        var positiveRewards = rewards
            .Select((r, index) => new { Reward = r, Index = index })
            .Where(x => x.Reward.reward > 0)
            .OrderByDescending(x => x.Reward.reward)
            .ToList();

        // Step 2: Return null if there are fewer than 1 valid options
        //if (positiveRewards.Count == 0)
        //    return null;

        // Step 3: Randomly pick one
        int randomPick = Random.Range(0, positiveRewards.Count);
        return positiveRewards[randomPick].Index;
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