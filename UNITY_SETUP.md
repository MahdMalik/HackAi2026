# Unity Setup Guide for ModelMystery Agents

## Scene Configuration

### Hierarchy Structure Required

```
Scene
├── GameManager (Empty GameObject)
│   └── Script: GameManager.cs
│
├── EvilFellas (Parent for Killer Agents)
│   ├── KillerAgent_1
│   │   ├── Script: EvilActionScript.cs
│   │   ├── Script: EvilScript.cs (Agent)
│   │   ├── Component: Behavior Parameters
│   │   │   └── Behavior Name: "KillerAgent"
│   │   │   └── Model: (Assign trained model here)
│   │   └── Component: Rigidbody2D (optional, for physics)
│   │
│   ├── KillerAgent_2
│   └── ... (more killers)
│
└── NormalFellas (Parent for Survivor Agents)
    ├── SurvivorAgent_1
    │   ├── Script: NormalActionScript.cs
    │   ├── Script: NormalScript.cs (Agent)
    │   ├── Component: Behavior Parameters
    │   │   └── Behavior Name: "SurvivorAgent"
    │   │   └── Model: (Assign trained model here)
    │   └── Component: Rigidbody2D (optional)
    │
    ├── SurvivorAgent_2
    └── ... (more survivors)
```

## Step-by-Step Setup

### 1. GameManager Setup
- Create an empty GameObject and name it "GameManager"
- Add the GameManager.cs script
- Set matchTime (default 30 seconds)
- Do NOT assign manager reference (auto-finds via GetComponent)

### 2. Killer Agents Setup
- Create parent GameObject named "EvilFellas"
- For each killer agent:
  - Create child GameObject (e.g., "KillerAgent_1")
  - Add **EvilActionScript.cs**
  - Add **EvilScript.cs** (the Agent component)
  - Drag GameManager object to Manager field in EvilActionScript
  - Add **Behavior Parameters** component:
    - Behavior Name: `KillerAgent`
    - Discrete Action Size: `[5, 5, 5]` (Movement: 5, Alignment1: 5, Alignment2: 5)
    - Observation Size: **13**
    - Add SensingComponent type sensor
  - Position randomly in the scene

### 3. Survivor Agents Setup
- Create parent GameObject named "NormalFellas"
- For each survivor agent:
  - Create child GameObject (e.g., "SurvivorAgent_1")
  - Add **NormalActionScript.cs**
  - Add **NormalScript.cs** (the Agent component)
  - Drag GameManager object to Manager field in NormalActionScript
  - Add **Behavior Parameters** component:
    - Behavior Name: `SurvivorAgent`
    - Discrete Action Size: `[4, 3, 3]` (Movement: 4, Alignment1: 3, Alignment2: 3)
    - Observation Size: **16**
    - Add SensingComponent type sensor
  - Position randomly in the scene

### 4. Behavior Parameters Configuration

For both agent types:
```
Behavior Parameters:
├── Behavior Name: (KillerAgent or SurvivorAgent)
├── Model: (Leave empty during training, assign model after)
├── Inference Device: GPU (recommended for faster inference)
├── Observations:
│   ├── Observation Size: (13 for Killers, 16 for Survivors)
│   └── Observation Type: Default
├── Actions:
│   ├── Action Space Type: Discrete
│   └── Discrete Actions: [5, 5, 5] for Killers, [4, 3, 3] for Survivors
```

## Communication with GameManager

Each agent script automatically:
- Registers with GameManager in Awake()
- Sets robotId sequentially
- Initializes alignment dictionary

Verify in GameManager that robots dictionary is populated:
```csharp
void OnGUI() {
    GUILayout.Label($"Robots Registered: {robots.Count}");
    foreach(var robot in robots) {
        GUILayout.Label($"Robot {robot.Key}: {robot.Value.agentType}");
    }
}
```

## Training Mode vs Inference Mode

### Training Mode
1. Open scene in Unity Editor
2. Run: `python PPO.py` or `mlagents-learn config.yaml --run-id=...`
3. Press Play in Unity
4. ML-Agents will control agent actions and collect rewards
5. Stop training with Ctrl+C

### Inference Mode (After Training)
1. Import trained model:
   - Export from PyTorch to ONNX
   - Use ML-Agents importer in Unity
2. Assign model to Behavior Parameters
3. Set Behavior Type to "Inference Only" in Inspector
4. Play scene - agents will use trained policy

## Common Issues & Solutions

### "Manager reference not set"
- Solution: Ensure manager field points to GameObject with GameManager component

### Agents not moving
- Check if OnActionReceived is being called
- Verify Movement action execution in ExecuteMovementAction
- Check robot.isAlive status

### Rewards not applying
- Verify GameManager.AssignRewardToLastActions is called
- Check that Agent component's AddReward is functional
- Monitor console for errors

### Observation size mismatch errors
- Killer: Must be 13 (not 12 or 14)
- Survivor: Must be 16 (not 15)
- Count all AddObservation calls in CollectObservations

### Discrete action errors
- Killer: [5, 5, 5] - Movement 5 options, Alignment 5 options each
- Survivor: [4, 3, 3] - Movement 4 options, Alignment 3 options each

## Testing Individual Agents

Test a single agent behavior:
```csharp
// In a test method
public void TestKillerMovement() {
    EvilScript killer = GetComponent<EvilScript>();
    
    // Simulate action
    ActionBuffers test = new ActionBuffers();
    test.DiscreteActions.Array[0] = 1; // Follow nearest
    killer.OnActionReceived(test);
    
    // Verify movement
    Assert.AreEqual("Move Towards", killer.currentAction);
}
```

## Physics Setup (Optional)

If using Rigidbody2D for collision detection:
```
Rigidbody 2D:
├── Body Type: Dynamic
├── Gravity Scale: 0
├── Collision:
│   └── Enable collision detection for kill zones
```

Add colliders:
- Trigger collider for kill detection
- OnTriggerStay method in RobotScript calls CheckAndExecuteKill

## Performance Optimization

For faster training with multiple instances:
```bash
mlagents-learn config.yaml --run-id=ModelMystery_Run \
    --env=ModelMystery.exe \
    --num-envs=4  # Run 4 parallel environments
```

Build multiple instances:
1. Create builds in separate folders
2. ML-Agents will automatically manage parallel training
3. 4x faster learning with 4 environments

## Next Steps

1. **Setup the scene** following the hierarchy above
2. **Configure agents** with correct behavior parameters
3. **Build the project** (File > Build Settings > Build)
4. **Start training** with `python PPO.py`
5. **Monitor training** in TensorBoard:
   ```bash
   tensorboard --logdir=results
   ```
6. **After 1-2M steps**, integrate trained models back into Unity

Typical training time: 30 minutes to 2 hours depending on scene size and agent count.
