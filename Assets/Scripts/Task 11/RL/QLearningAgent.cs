using UnityEngine;

public class QLearningAgent
{
    private QTable qTable;
    private CellType[,] gridSnapshot;
    private int gridWidth;
    private int gridHeight;

    private int currentX;
    private int currentY;

    public float LearningRate { get; set; }
    public float DiscountFactor { get; set; }
    public float ExplorationRate { get; set; }

    public float RewardGoal { get; set; }
    public float PenaltyWall { get; set; }
    public float PenaltyStep { get; set; }

    public Vector2Int GoalPosition { get; private set; }
    public Vector2Int CurrentPosition => new Vector2Int(currentX, currentY);
    public QTable Table => qTable;

    public QLearningAgent(CellType[,] grid, int width, int height,
                          float learningRate = 0.1f,
                          float discountFactor = 0.99f,
                          float explorationRate = 1.0f,
                          float rewardGoal = 1000f,
                          float penaltyWall = 0f,
                          float penaltyStep = 0f)
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

    public void SetGoal(int x, int y)
    {
        GoalPosition = new Vector2Int(x, y);
    }

    public void SetGoal(Vector2Int pos)
    {
        GoalPosition = pos;
    }

    public void SetPosition(int x, int y)
    {
        currentX = x;
        currentY = y;
    }

    public void SetRandomPosition()
    {
        int attempts = 0;
        while (attempts < 1000)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            
            if (gridSnapshot[x, y] == CellType.Floor && (x != GoalPosition.x || y != GoalPosition.y))
            {
                currentX = x;
                currentY = y;
                return;
            }
            attempts++;
        }
        currentX = GoalPosition.x;
        currentY = GoalPosition.y;
    }

    public int GetCurrentStateInfo(int x, int y)
    {
        return qTable.PositionToState(x, y);
    }

    public int ChooseAction()
    {
        if (Random.value < ExplorationRate)
        {
            return Random.Range(0, QTable.ActionCount);
        }
        else
        {
            int state = GetCurrentStateInfo(currentX, currentY);
            return qTable.GetBestAction(state);
        }
    }

    public bool Step(int action)
    {
        int currentState = GetCurrentStateInfo(currentX, currentY);

        Vector2Int dir = QTable.ActionToDirection(action);
        int newX = currentX + dir.x;
        int newY = currentY + dir.y;

        float reward;
        int newState;
        bool reachedGoal = false;

        if (newX < 0 || newX >= gridWidth || newY < 0 || newY >= gridHeight ||
            gridSnapshot[newX, newY] == CellType.Wall)
        {
            reward = PenaltyWall;
            newState = currentState; 
        }
        else if (newX == GoalPosition.x && newY == GoalPosition.y)
        {
            reward = RewardGoal;
            newState = GetCurrentStateInfo(newX, newY);
            reachedGoal = true;
        }
        else
        {
            reward = PenaltyStep;
            newState = GetCurrentStateInfo(newX, newY);
        }

        float oldQ = qTable.GetQ(currentState, action);
        
        float maxFutureQ = reachedGoal ? 0f : qTable.GetMaxQ(newState);
        
        float newQ = oldQ + LearningRate * (reward + DiscountFactor * maxFutureQ - oldQ);
        qTable.SetQ(currentState, action, newQ);

        if (newState != currentState)
        {
            currentX = newX;
            currentY = newY;
        }

        return reachedGoal;
    }

    public int RunEpisode(int maxSteps = 1000)
    {
        SetRandomPosition();

        for (int step = 0; step < maxSteps; step++)
        {
            if (currentX == GoalPosition.x && currentY == GoalPosition.y)
                return step;

            int action = ChooseAction();
            bool done = Step(action);

            if (done) return step + 1;
        }

        return maxSteps;
    }

    public void UpdateGrid(CellType[,] newGrid)
    {
        gridSnapshot = newGrid;
    }
}