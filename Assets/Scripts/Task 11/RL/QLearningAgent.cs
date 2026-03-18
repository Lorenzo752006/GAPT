using UnityEngine;

/// <summary>
/// The Q-Learning Agent: explores the grid, takes actions, receives rewards,
/// and updates the Q-Table using the Bellman equation.
/// 
/// This is the core reinforcement learning algorithm. The agent does NOT know
/// the map layout in advance. It learns purely by trial and error:
///   1. Take an action (move in a direction).
///   2. Observe the result (new position, reward/punishment).
///   3. Update the Q-Table cell using the mathematical formula.
///   4. Repeat thousands of times until knowledge converges.
/// 
/// The Bellman Update Formula:
///   Q(s,a) = Q(s,a) + ? * [R + ? * max Q(s',a') - Q(s,a)]
/// 
/// Where:
///   s     = current state (grid position)
///   a     = action taken
///   R     = reward received
///   s'    = new state after action
///   ?     = learning rate (how fast the agent updates its knowledge)
///   ?     = discount factor (how much future rewards matter vs immediate)
/// </summary>
public class QLearningAgent
{
    private QTable qTable;
    private CellType[,] gridSnapshot;
    private int gridWidth;
    private int gridHeight;

    // Current agent position during simulation
    private int currentX;
    private int currentY;

    /// <summary>
    /// Learning rate (alpha): Controls how much new information overrides old.
    /// Range 0-1. Higher = learn faster but less stable.
    /// </summary>
    public float LearningRate { get; set; }

    /// <summary>
    /// Discount factor (gamma): How much future rewards are valued.
    /// Range 0-1. Higher = agent plans further ahead.
    /// </summary>
    public float DiscountFactor { get; set; }

    /// <summary>
    /// Exploration rate (epsilon): Probability of taking a random action
    /// instead of the best known action. Encourages discovery of new paths.
    /// Range 0-1. Decays over time during training.
    /// </summary>
    public float ExplorationRate { get; set; }

    /// <summary>
    /// Reward values for different outcomes.
    /// </summary>
    public float RewardGoal { get; set; }
    public float PenaltyWall { get; set; }
    public float PenaltyStep { get; set; }

    /// <summary>
    /// The goal position the agent is trying to reach.
    /// </summary>
    public Vector2Int GoalPosition { get; private set; }

    /// <summary>
    /// Current position of the agent in the simulation.
    /// </summary>
    public Vector2Int CurrentPosition => new Vector2Int(currentX, currentY);

    /// <summary>
    /// Access to the underlying Q-Table for visualization and runtime use.
    /// </summary>
    public QTable Table => qTable;

    public QLearningAgent(CellType[,] grid, int width, int height,
                          float learningRate = 0.1f,
                          float discountFactor = 0.95f,
                          float explorationRate = 1.0f,
                          float rewardGoal = 100f,
                          float penaltyWall = -10f,
                          float penaltyStep = -1f)
    {
        gridWidth = width;
        gridHeight = height;
        gridSnapshot = grid;

        qTable = new QTable(width, height);

        LearningRate = learningRate;
        DiscountFactor = discountFactor;
        ExplorationRate = explorationRate;
        RewardGoal = rewardGoal;
        PenaltyWall = penaltyWall;
        PenaltyStep = penaltyStep;
    }

    /// <summary>
    /// Sets the goal position the agent should learn to navigate to.
    /// </summary>
    public void SetGoal(int x, int y)
    {
        GoalPosition = new Vector2Int(x, y);
    }

    /// <summary>
    /// Sets the goal to the player's current grid position.
    /// </summary>
    public void SetGoal(Vector2Int pos)
    {
        GoalPosition = pos;
    }

    /// <summary>
    /// Places the agent at a specific grid position (for episode start).
    /// </summary>
    public void SetPosition(int x, int y)
    {
        currentX = x;
        currentY = y;
    }

    /// <summary>
    /// Places the agent at a random walkable position on the grid.
    /// </summary>
    public void SetRandomPosition()
    {
        int attempts = 0;
        while (attempts < 1000)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            if (gridSnapshot[x, y] == CellType.Floor)
            {
                currentX = x;
                currentY = y;
                return;
            }
            attempts++;
        }
        // Fallback: place at goal
        currentX = GoalPosition.x;
        currentY = GoalPosition.y;
    }

    /// <summary>
    /// Selects an action using epsilon-greedy policy.
    /// With probability epsilon, picks a random action (exploration).
    /// Otherwise, picks the best known action (exploitation).
    /// </summary>
    public int ChooseAction()
    {
        if (Random.value < ExplorationRate)
        {
            // Explore: random action
            return Random.Range(0, QTable.ActionCount);
        }
        else
        {
            // Exploit: best known action
            int state = qTable.PositionToState(currentX, currentY);
            return qTable.GetBestAction(state);
        }
    }

    /// <summary>
    /// Executes one step of the learning loop:
    ///   1. Take the given action.
    ///   2. Determine the reward.
    ///   3. Update the Q-Table using the Bellman equation.
    ///   4. Move to the new state (if valid).
    /// 
    /// Returns true if the agent reached the goal.
    /// </summary>
    public bool Step(int action)
    {
        int currentState = qTable.PositionToState(currentX, currentY);

        // Calculate the target position
        Vector2Int dir = QTable.ActionToDirection(action);
        int newX = currentX + dir.x;
        int newY = currentY + dir.y;

        float reward;
        int newState;
        bool reachedGoal = false;

        // Determine the outcome
        if (newX < 0 || newX >= gridWidth || newY < 0 || newY >= gridHeight ||
            gridSnapshot[newX, newY] == CellType.Wall)
        {
            // Hit a wall or out of bounds: punishment, stay in place
            reward = PenaltyWall;
            newState = currentState; // Don't move
        }
        else if (newX == GoalPosition.x && newY == GoalPosition.y)
        {
            // Reached the goal: big reward!
            reward = RewardGoal;
            newState = qTable.PositionToState(newX, newY);
            reachedGoal = true;
        }
        else
        {
            // Valid move, small step penalty to encourage efficiency
            reward = PenaltyStep;
            newState = qTable.PositionToState(newX, newY);
        }

        // ============================================================
        //  THE BELLMAN EQUATION — The heart of Q-Learning
        // ============================================================
        //  Q(s,a) = Q(s,a) + ? * [R + ? * max Q(s',a') - Q(s,a)]
        //
        //  This propagates the value of good rewards backwards through
        //  the chain of states that led to them. Over many episodes,
        //  the Q-values converge to represent the true expected reward
        //  of each state-action pair.
        // ============================================================
        float oldQ = qTable.GetQ(currentState, action);
        float maxFutureQ = qTable.GetMaxQ(newState);
        float newQ = oldQ + LearningRate * (reward + DiscountFactor * maxFutureQ - oldQ);
        qTable.SetQ(currentState, action, newQ);

        // Move to the new state (unless we hit a wall)
        if (newState != currentState)
        {
            Vector2Int newPos = qTable.StateToPosition(newState);
            currentX = newPos.x;
            currentY = newPos.y;
        }

        return reachedGoal;
    }

    /// <summary>
    /// Runs a complete training episode: agent starts at a random position
    /// and tries to reach the goal within maxSteps.
    /// Returns the number of steps taken (or maxSteps if it didn't reach the goal).
    /// </summary>
    public int RunEpisode(int maxSteps = 200)
    {
        SetRandomPosition();

        for (int step = 0; step < maxSteps; step++)
        {
            // If already at goal, done instantly
            if (currentX == GoalPosition.x && currentY == GoalPosition.y)
                return step;

            int action = ChooseAction();
            bool done = Step(action);

            if (done)
                return step + 1;
        }

        return maxSteps;
    }

    /// <summary>
    /// Updates the grid snapshot (call this if the map changes at runtime).
    /// </summary>
    public void UpdateGrid(CellType[,] newGrid)
    {
        gridSnapshot = newGrid;
    }
}
