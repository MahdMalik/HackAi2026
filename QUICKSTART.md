# Quick Start Guide - ModelMystery RL Agents

## 📋 What's Been Implemented

Your Unity game now has a complete RL agent system with:

✅ **Killer Agents** - Hunt for rewards, learn to identify prey  
✅ **Survivor Agents** - Evade threats, learn defensive strategies  
✅ **PPO Learning** - Proximal Policy Optimization training  
✅ **Reward System** - Sophisticated credit assignment for actions  
✅ **Alignment System** - Agents perceive each other dynamically  
✅ **Screen Bounds** - All agents kept visible and in bounds  
✅ **Visual Representation** - Colored agents (Red killers, Cyan survivors)  
✅ **Match Timer** - Countdown display with live stats  
✅ **Auto-Restart** - Matches automatically restart after time ends  

## 🚀 Getting Started (5 Steps)

### Step 1: Setup Your Scene (5 minutes)

**Option A: Quick Automatic Setup**
1. Open your scene in Unity
2. Create an empty GameObject
3. Add the `SceneInitializer` script
4. In Inspector, set:
   - Killer Agent Count: 3
   - Survivor Agent Count: 3
   - Spawn Radius: 15
5. Click "Initialize Scene" in Inspector

**Option B: Manual Setup**
Follow [UNITY_SETUP.md](UNITY_SETUP.md) for detailed hierarchy instructions

### Step 2: Verify Components

After setup, your scene should have:
```
✅ GameManager (with all robots registered)
✅ EvilFellas (parent with 3 KillerAgent children)
✅ NormalFellas (parent with 3 SurvivorAgent children)
```

Each agent should have:
- ✅ Action Script (EvilActionScript or NormalActionScript)
- ✅ Agent Script (EvilScript or NormalScript)  
- ✅ Behavior Parameters component
- ✅ Rigidbody 2D (optional but recommended)

### Step 3: Build the Project

```
Unity Menu:
1. File → Build Settings
2. Configure build for your platform
3. Click "Build"
4. Save as "ModelMystery.exe" (or note the path)
```

### Step 4: Install ML-Agents Training

```bash
# Open terminal/command prompt
pip install mlagents
pip install torch  # If not already installed
```

### Step 5: Start Training!

**Quick Start:**
```bash
cd c:\Users\mahd\Documents\ModelMystery
python PPO.py
```

**That's it!** The training will:
- Launch your built game
- Run agents through scenarios
- Collect experience
- Optimize policies using PPO
- Save models to `results/ModelMystery_Run/`

Training typically takes **30-120 minutes** for convergence.

## 📊 Monitoring Training

### Live Dashboard (Launch in New Terminal)
```bash
tensorboard --logdir=results
```
Then open: http://localhost:6006

### What to Look For:
- **Environment/Cumulative Reward**: Should increase over time
- **Policy Loss**: Should decrease (agent learning)
- **Episode Length**: Should stabilize

## 🎮 After Training

### Option 1: Continue in Inspector
1. Training saves models to `results/ModelMystery_Run/Agent(.onnx)`
2. Drag model to Behavior Parameters in Inspector
3. Set behavior to "Inference Only"
4. Play to see trained agents interact!

### Option 2: Advanced - Modify and Retrain
- Tweak rewards in EvilAgentScript.cs / NormalAgentScript.cs
- Adjust parameters in config.yaml
- Retrain: `python PPO.py --force`

### Match Flow & Rewards
When a match ends (30 seconds by default):
1. All surviving agents get **survival bonus** reward
2. **Killer agents**: +3 per action taken (distributed across all actions)
3. **Survivor agents**: +5 per action taken (distributed across all actions)
4. All agents get EndEpisode() called
5. New match automatically starts with:
   - Fresh positions within screen bounds
   - Reset alignments (killers see all as "Prey", survivors see all as "Neutral")
   - Clear action history
6. Match counter increments (visible in HUD)

## 🔧 Troubleshooting

### Agents not moving?
```
1. Check Console for errors
2. Verify manager reference: agentScript.manager != null
3. Ensure RobotScript.DoUpdate() is called from Update()
```

### Training instantly crashes?
```
1. Verify executable exists at the specified path
2. Test build runs standalone without errors
3. Check firewall isn't blocking ports
```

### Observations mismatch?
```
Killer Agents: Must submit exactly 13 observations
Survivor Agents: Must submit exactly 16 observations

Count AddObservation() calls in CollectObservations()
```

### Agents disappearing or not visible?
```
1. Check that Camera.main is set in your scene
2. Verify the camera has orthographic projection enabled
3. Agents are automatically clamped to screen bounds
4. If agents appear to be stuck at edges, check camera orthographic size
5. Red cubes = Killer agents, Cyan cubes = Survivor agents
```

### Match timer not showing?
```
1. MatchTimerUI is automatically created on GameManager
2. Check that TextMeshPro is installed (required for UI)
3. UI shows: Countdown, Match #, Alive counts
4. Timer color: Green (plenty of time) → Yellow → Red (almost over)
5. A "Match complete!" banner flashes when round ends
```

### Agents running outside bounds?
```
Agents are now automatically clamped to the visible screen bounds during movement
- ClampPositionToScreenBounds() is called each frame in DoUpdate()
- Screen bounds calculated from camera orthographic size and aspect ratio
- Padding of 0.5 units from screen edges to ensure visibility
```

### Match doesn't restart?
```
1. Check if GameManager has matchEnded flag properly set
2. Verify all agents get EndEpisode() called
3. OnEpisodeBegin() must be called on all agents for proper reset
4. matchNumber increments for each new match (see in logs)
5. Default match length is 300 seconds (5 minutes) to provide adequate training time
```

### Match finishes too fast?
```
If matches are finishing instantly, check:
1. matchTime is set to 300 (5 minutes, not 30 seconds)
2. Time.deltaTime is being accumulated normally
3. ML-Agents timescale might be affecting delta time
4. Check Unity project Time scale settings (should be 1.0 in editor)
```

### Training too slow?
```bash
# Run 4 parallel environments
python PPO.py --env=ModelMystery.exe --num-envs=4
```

## 📚 Deep Dive Docs

- **[AGENT_IMPLEMENTATION.md](AGENT_IMPLEMENTATION.md)** - Technical architecture
- **[UNITY_SETUP.md](UNITY_SETUP.md)** - Detailed scene setup
- **[IMPLEMENTATION_CHECKLIST.md](IMPLEMENTATION_CHECKLIST.md)** - Complete checklist

## ⚙️ Key Configuration Files

### config.yaml
```yaml
behaviors:
  KillerAgent: # Training config for hunters
  SurvivorAgent: # Training config for survivors
```

Modify for:
- Faster/slower learning: `learning_rate`
- Larger/smaller networks: `hidden_units`
- More/less training: `max_steps`

### PPO.py
```python
train_agents(
    config_path="config.yaml",
    run_id="ModelMystery_Run",  # Change for new runs
    force=False  # Set True to overwrite existing models
)
```

## 💡 Understanding the Agents

### Killer Agents Think Like This:
> "I see prey! Kill it for +5 reward. Other killers are also prey. I'll learn to recognize who's weak. Death costs me -10, but surviving gives +3 bonus."

**Action Space:**
- Movement: Run, Follow, Attack, Follow Other, Random (5 options)
- Perception: Change opinion about 2 nearest agents (4 alignments + no-change)

### Survivor Agents Think Like This:
> "I need to survive! Moving away from threats gives me +5 at the end. Death is really bad (-15). I can identify killers and other survivors."

**Action Space:**
- Movement: Run, Follow, Follow Other, Random (4 options, NO attacking)
- Perception: Mark agents as Neutral or Hostile (3 options per agent)

## 🎯 Expected Results

After 1 million steps:
- **Killers**: ~60-80% kill rate, strategic hunting patterns
- **Survivors**: ~40-60% round survival, evasion behaviors well-developed

After 2 million steps:
- **Killers**: Sophisticated prey identification, coordinated hunting
- **Survivors**: Predictable safe zones, group formation

## 🧬 Next: Implementing Hero Agents (Future)

When ready to add Hero agents:
1. Copy EvilScript → HeroScript structure
2. Modify alignment system (no "Prey" alignment)
3. Implement complex reward function
4. Add to scene: `CreateHeroAgent()` in SceneInitializer
5. Train with: `config.yaml HeroAgent behavior`

[See AGENT_IMPLEMENTATION.md for Hero specifications]

## 📞 Common Commands Reference

```bash
# Start training
python PPO.py

# Force retraining
python PPO.py --force

# Custom run ID
python PPO.py --run-id=MyExperiment

# Multiple parallel environments (faster)
mlagents-learn config.yaml --num-envs=4 --run-id=FastRun

# Monitor tensorboard real-time
tensorboard --logdir=results

# Export for inference (after training)
mlagents-export-to-onnx results/ModelMystery_Run --output=Models/
```

## ✨ Fine-tuning Tips

**If agents learn too slowly:**
- Increase `learning_rate` in config.yaml (3.0e-4 → 5.0e-4)
- Increase `batch_size` (64 → 128)
- Decrease `time_horizon` (64 → 32)

**If training is unstable:**
- Decrease `learning_rate` (3.0e-4 → 1.0e-4)
- Increase `buffer_size` (12800 → 25600)
- Increase `epsilon` (0.2 → 0.3)

**If agents converge too early (exploiting single strategy):**
- Increase exploration: `beta` (1.0e-2 → 5.0e-2)
- Randomize rewards slightly
- Run 2nd phase with modified rewards

---

**Ready to Train? Run:**
```bash
cd c:\Users\mahd\Documents\ModelMystery
python PPO.py
```

**Questions?** Check [AGENT_IMPLEMENTATION.md](AGENT_IMPLEMENTATION.md) or the troubleshooting section above.

Good luck! 🚀
