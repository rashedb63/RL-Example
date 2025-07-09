# RL-Example: Q-Learning Maze Simulation in Unity

This project is a classical maze simulation demonstrating how **Q-Learning** works in a visual, interactive manner using Unity 3D. It is primarily designed for **educational purposes**, helping learners grasp the fundamentals of Reinforcement Learning by visualizing the agent's learning and decision-making process.

---

## Project Overview

- Implements a simple **maze environment**.
- Uses **Q-Learning** to train an agent to navigate the maze.
- Includes two main GameObjects:
  - **Learning**: Responsible for identifying walls and generating the Q-table based on user-set actions.
  - **Replay**: Reads and replays the learned behavior using the generated Q-table.

---

## Requirements

To run this project, you’ll need:

- [Unity 3D](https://unity.com/)
- [Visual Studio](https://visualstudio.microsoft.com/) (for inspecting and modifying C# scripts)

All required assets are included within the repository.

---

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/rashedb63/RL-Example.git

2. Open the project in Unity 3D.
3. Load the main scene, which includes:<br/>
   3.1. Learning GameObject: Set the Action parameter to:<br/> 
   0 – Identify maze walls<br/>
   1 – Generate Q-table<br/>
   3.2. Replay GameObject: Uses the generated Q-table to perform the learned navigation.<br/>
4. Play the scene and observe the training and performance in action.

## Recommended Usage:
1. Adjust the training time for better agent performance.
2. Watch how the agent evolves its behavior as training progresses.
3. Replay mode shows the agent's learned navigation path.

## Contributing:
Contributions are highly encouraged! Whether you're fixing bugs, improving the UI, or enhancing the learning logic — all help is welcome.
Simply fork the repo, make your changes, and open a pull request.

## License:
This project is open to the public without any restrictions. Use, modify, and share freely!

## Contact
For suggestions or collaboration ideas, feel free to reach out or open an issue.
