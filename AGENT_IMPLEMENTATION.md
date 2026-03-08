# ModelMystery RL Agent Implementation

## Overview

This is a Unity-based multi-agent reinforcement learning environment using PPO (Proximal Policy Optimization) where agents learn through gameplay interactions. Currently implemented: **Killer Agents** and **Survivor Agents**.

## Architecture

### Agent Types

#### **Killer Agents (EvilScript + EvilActionScript)**
- **Initial Alignment**: All others seen as "Prey"
- **Objective**: Hunt and eliminate other agents for rewards
- **Special Alignments**: Can perceive agents as Neutral, Friendly, Prey, or Predator
- **Attack Capability**: Can kill other agents in close range

**Reward Structure:**
- +5: Kill a Prey agent
- +5: Kill a Predator agent  
- -10: Agent dies
- +3: Survive the round (distributed across all actions)
- -0.001: Step penalty (encourages action efficiency)

#### **Survivor Agents (NormalScript + NormalActionScript)**
- **Initial Alignment**: All others seen as "Neutral"
- **Objective**: Survive as long as possible
- **Movement Options**: Run away, Follow, Follow 2nd nearest, Move random (no attacking)
- **Alignments**: Only "Neutral" and "Hostile" states

**Reward Structure:**
- -15: Agent dies (severe penalty)
- +5: Survive the round (distributed across all actions)
- -0.002: Step penalty (slightly higher than killers)

### Observation Space

Both agent types observe:

1. **Nearest Bot Info** (3 values):
   - Bot ID
   - Last action (encoded as 0=Random, 1=Away, 2=Towards, 3=Attack for killers)
   - Existence flag (1 if exists, 0 otherwise)

2. **2nd Nearest Bot Info** (3 values):
   - Same as nearest bot

3. **Game State** (4 values):
   - Time remaining (normalized 0-1)
   - Dead bot count (normalized)
   - Neutral alignment count (normalized)
   - Hostile/Predator alignment count (normalized)

**Survivors additionally observe:**
- Agent type of nearest bots (helps identify threats)

**Total observation size:**
- Killers: 13 continuous values
- Survivors: 16 continuous values

### Action Space

Both agent types use 3 discrete action branches:

#### **Action 0: Movement (Killer: 5 options, Survivor: 4 options)**

*Killers:*
- 0: Run away from nearest
- 1: Follow nearest
- 2: Attack nearest
- 3: Follow 2nd nearest
- 4: Move randomly

*Survivors:*
- 0: Run away from nearest (defensive)
- 1: Follow nearest (cooperative)
- 2: Follow 2nd nearest
- 3: Move randomly

#### **Action 1-2: Alignment Changes (for 2 closest bots)**

*Killers:*
- 0: Change to Neutral
- 1: Change to Friendly
- 2: Change to Prey
- 3: Change to Predator
- 4: No change

*Survivors:*
- 0: Change to Neutral
- 1: Change to Hostile
- 2: No change

## Component Structure

### RobotScript (Base Class)
```
- Handles basic movement and alignment tracking
- Maintains action history (~10 last actions)
- Provides nearest bot calculations
- Tracks death/survival state
```

### EvilScript (Killer Agent)
```
- Inherits from Unity ML-Agents Agent
- Implements PPO learning
- Collects observations for killer behavior
- Processes discrete movement + alignment actions
- Generates rewards based on kills and survival
```

### NormalScript (Survivor Agent)
```
- Inherits from Unity ML-Agents Agent
- Implements PPO learning with defensive strategy
- Collects observations optimized for survival
- Limited action set (no offensive moves)
- Generates rewards for avoidance and survival
```

### GameManager
```
- Manages all robot instances (dictionary by ID)
- Tracks episode timing and termination
- Distributes rewards to agents' last 2 or all actions
- Handles end-of-match reward distribution
- Triggers death/survival callbacks
```

## Training Configuration

The `config.yaml` file specifies:

- **Trainer**: PPO (Proximal Policy Optimization)
- **Batch Size**: 64 experiences per update
- **Learning Rate**: 3.0e-4 (with linear decay)
- **Epsilon**: 0.2 (PPO clipping)
- **Lambda**: 0.99 (GAE λ)
- **Epochs**: 3 passes over batch
- **Max Steps**: 2,000,000 total training steps
- **Network**: 2 layers × 128 hidden units

Both agent types use identical PPO hyperparameters but learn different policies due to different reward structures.

## Training Instructions

### Setup

```bash
# Install ML-Agents
pip install mlagents

# Install PyTorch (if not already installed)
pip install torch
```

### Running Training

**Option 1: Command Line**
```bash
mlagents-learn config.yaml --run-id=ModelMystery_Run --env=ModelMystery.exe
```

**Option 2: Python Script**
```bash
python PPO.py
python PPO.py --force  # Force retraining
python PPO.py --run-id=CustomRunName
```

### In Unity Editor

1. Build the project (File > Build)
2. Point ML-Agents to the built executable
3. Training will control the game and agents learn from experience

### Model Integration

After training completes:
1. Models saved to `results/ModelMystery_Run/`
2. Convert PyTorch models to ONNX format
3. Import into Unity via ML-Agents model importer
4. Attach to agents' Behavior Parameters

## Key Implementation Details

### Reward Assignment

**Last 2 Actions System:**
- When an agent dies or gets a significant kill, reward is distributed:
  - 60% to the last action taken
  - 40% to the second-to-last action

This credits recent decisions that led to the outcome.

**Survival Bonus:**
- Distributed equally across ALL actions taken during the episode
- Encourages long-term strategic play

### Alignment Learning

Agents learn to:
- **Killers**: Update alignment perceptions based on observed behavior to predict threats vs safe prey
- **Survivors**: Identify which agents are "Hostile" (killers) vs "Neutral" (other survivors)

### Safety Features

- Agents reset position randomly at episode start
- Dead agents are removed from active play
- Episode terminates if match time expires
- Action history limited to 10 recent actions

## Performance Metrics to Monitor

1. **Killer Metrics:**
   - Average kills per episode
   - Survival rate
   - Episode length

2. **Survivor Metrics:**
   - Survival duration
   - Deaths per episode
   - Distance from killers (potentially)

3. **Training Health:**
   - Policy loss
   - Value loss
   - Entropy
   - Reward moving average

## Future Enhancement (Hero Agents)

The framework is designed to easily add Hero agents with:
- No "Prey" alignment (won't hunt survivors)
- Ability to distinguish killers vs survivors
- Complex reward: high reward for killing killers, severe penalty for killing survivors
- Neutral start with all agents

## Debug Commands

In GameManager console:
```csharp
// Check robot alignments
Debug.Log(robots[id].alignments);

// Force end match
gameManager.currentTime = gameManager.matchTime;

// Check agent status
robots[id].isAlive // true/false
robots[id].killCount // number of kills
```

## Troubleshooting

**Agents not learning?**
- Check observation/action space matches config
- Verify reward signals are being applied
- Increase training steps
- Lower learning rate for stability

**Training too slow?**
- Increase batch size for faster updates
- Reduce visual encoding complexity
- Use --num-envs to run multiple instances

**Models not loading in Unity?**
- Ensure ONNX conversion completed
- Check model file names match Behavior Parameters
- Verify model dimensions match observation shape
