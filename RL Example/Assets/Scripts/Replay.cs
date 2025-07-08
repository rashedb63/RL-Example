using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Replay : MonoBehaviour
{
    public GameObject grid;
    private List<State> states;
    public GameObject SmartAgent;
    // Start is called before the first frame update
    void Start()
    {
        BuildStateList();
        StartCoroutine(RandomlySpawnSmartAgent());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator RandomlySpawnSmartAgent()
    {
        GameObject agent = null;
        bool done = true;
        while (true)
        {
            yield return null;
            int index = UnityEngine.Random.Range(0, states.Count);
            // Instantiate & init
            if (done)
            {
                agent = Instantiate(
                SmartAgent,
                states[index].GetPosition().position,
                Quaternion.identity
                );
                done = false;
            }

            var mover = agent.GetComponent<MoveSmartAgent>();
            mover.OnAgentComplete += () =>
            {
                done = true;
            };
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
        foreach (Transform row in grid.transform)
        {
            foreach (Transform column in row)
            {
                pos = column.position;
                state = new State(column.name, column);
                states.Add(state);
            }
        }
        // Debug.Log("Total States = " + states.Count);
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
